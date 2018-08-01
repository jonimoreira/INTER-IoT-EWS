using EDXLSharp.EDXLDELib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContextManager.DataObjects.EDXL
{
    /// <summary>
    /// 1.4     Applications of the EDXL Distribution Element
    /// The primary use of the EDXL Distribution Element is to identify and provide information to enable the routing of encapsulated 
    /// payloads, called Content Objects.It is used to provide a common mechanism to encapsulate content information.
    /// </summary>
    public class DE
    {
        private EDXLSharp.EDXLDELib.EDXLDE msgDE;
        private List<CAP> cAPmessages;

        public EDXLSharp.EDXLDELib.EDXLDE DEelement
        {
            get
            {
                if (msgDE == null)
                    msgDE = new EDXLDE();

                if (msgDE.ContentObjects == null)
                    msgDE.ContentObjects = new List<ContentObject>();

                return msgDE;
            }
        }

        public List<CAP> CAPmessages
        {
            get
            {
                if (cAPmessages == null)
                    cAPmessages = new List<CAP>();
                return cAPmessages;
            }

            set
            {
                cAPmessages = value;
            }
        }
        
        public DE(CAP msgCAP): base()
        {
            DEelement.DistributionID = Guid.NewGuid() + "_" + DateTime.Now.ToFileTimeUtc();
            DEelement.SenderID = "inter.iot.ews@gmail.com";
            DEelement.DateTimeSent = DateTime.Now;

            ContentObject contentObject = new ContentObject();
            contentObject.ContentDescription = "EDXL-CAP message";

            NonXMLContentType nonXml = new NonXMLContentType();
            nonXml.MIMEType = "application/ld+json";
            // TODO: do not deserialize CAP message (JObject to string)
            //nonXml.ContentData = msgCAP.GetJsonLD().ToString(Newtonsoft.Json.Formatting.None);
            contentObject.NonXMLContent = nonXml;

            contentObject.ContentDescription = "CAP message from INTER-IoT-EWS warning a risk of accident detected";
            
            DEelement.ContentObjects.Add(contentObject);
            CAPmessages.Add(msgCAP);
            
        }
        
        public JObject GetJsonLD()
        {
            JObject context = GetContextJSON_EDXL();

            JObject edxlMessage = new JObject();
            edxlMessage.Add("@context", context);
            edxlMessage.Add("@id", "edxl_de_inst:" + DEelement.DistributionID);
            edxlMessage.Add("@type", "edxl_de:EDXLDistribution");
            edxlMessage.Add("edxl_de:distributionID", DEelement.DistributionID);
            edxlMessage.Add("edxl_de:senderID", DEelement.SenderID);
            edxlMessage.Add("rdfs:label", "[INTER-IoT-EWS] Early Warning: situation detected");
            edxlMessage.Add("edxl_de:dateTimeSent", DEelement.DateTimeSent.ToString("o"));
            edxlMessage.Add("edxl_de:dateTimeExpires", DEelement.DateTimeSent.AddDays(2).ToString("o"));
            edxlMessage.Add("edxl_de:hasContent", this.GetContent());
            edxlMessage.Add("edxl_de:hasDistribuitionKind", this.GetDistribuitionKind());
            edxlMessage.Add("edxl_de:hasDistribuitionStatus", this.GetDistribuitionStatus());

            return edxlMessage;
        }

        public JObject GetContent()
        {
            JObject result = new JObject();
            
            result.Add("@id", "edxl_de_inst:Content_" + DEelement.DistributionID);
            result.Add("@type", "edxl_de:Content");
            result.Add("edxl_de:hasContentObject", GetContentObject());

            return result;
        }

        public JArray GetContentObject()
        {
            JArray resultArray = new JArray();
            //foreach (CAP capMessage in cAPmessages)
            foreach (ContentObject contentObj in DEelement.ContentObjects)
            {
                JObject result = new JObject();

                result.Add("@id", "edxl_de_inst:OtherContent" + DEelement.DistributionID + "_" + Guid.NewGuid());
                result.Add("@type", "edxl_de:OtherContent");
                result.Add("edxl_de:mimeType", contentObj.NonXMLContent.MIMEType);
                result.Add("edxl_de:hasContentDescriptor", GetContentDescriptor(contentObj));
                //JObject edxlContent = JObject.Parse(contentObj.NonXMLContent.ContentData);
                JObject edxlContent = CAPmessages[0].GetJsonLD();
                result.Add("edxl_de:contentData", edxlContent);

                resultArray.Add(result);
            }

            return resultArray;
        }

        public JObject GetContentDescriptor(ContentObject contentObj)
        {
            JObject result = new JObject();

            result.Add("@id", "edxl_de_inst:ContentDescriptor_" + DEelement.DistributionID + "_" + Guid.NewGuid());
            result.Add("@type", "edxl_de:ContentDescriptor");
            result.Add("edxl_de:contentDescription", contentObj.ContentDescription);

            return result;
        }

        public JObject GetDistribuitionKind()
        {
            JObject result = new JObject();

            result.Add("@id", "edxl_de_inst:DistributionKind_SensorDetection_" + DEelement.DistributionID + "_" + Guid.NewGuid());
            result.Add("@type", "edxl_de:SensorDetection");

            return result;
        }

        public JToken GetDistribuitionStatus()
        {
            return cAPmessages[0].ConvertToInstanceInJsonLD(cAPmessages[0].CAPelement.MessageStatus.ToString(), "['edxl_de:DistributionStatus', 'edxl_cap:Exercise', 'edxl_cap:Status']");
        }

        public JObject GetContextJSON_EDXL()
        {
            // <http://fpc.ufba.br/ontologies/edxl_cap>

            string context = @"
{
    'label' : {
      '@id' : 'http://www.w3.org/2000/01/rdf-schema#label'
    },
    'distributionID' : {
      '@id' : 'http://fpc.ufba.br/ontologies/edxl_de#distributionID'
    },
    'senderID' : {
      '@id' : 'http://fpc.ufba.br/ontologies/edxl_de#senderID'
    },
    'dateTimeSent' : {
      '@id' : 'http://fpc.ufba.br/ontologies/edxl_de#dateTimeSent',
      '@type' : 'http://www.w3.org/2001/XMLSchema#dateTime'
    },
    'dateTimeExpires' : {
      '@id' : 'http://fpc.ufba.br/ontologies/edxl_de#dateTimeExpires',
      '@type' : 'http://www.w3.org/2001/XMLSchema#dateTime'
    },
    'hasDistribuitionStatus' : {
      '@id' : 'http://fpc.ufba.br/ontologies/edxl_de#hasDistribuitionStatus',
      '@type' : '@id'
    },
    'hasDistribuitionKind' : {
      '@id' : 'http://fpc.ufba.br/ontologies/edxl_de#hasDistribuitionKind',
      '@type' : '@id'
    },
    'hasContent' : {
      '@id' : 'http://fpc.ufba.br/ontologies/edxl_de#hasContent',
      '@type' : '@id'
    },
    'hasContentObject' : {
      '@id' : 'http://fpc.ufba.br/ontologies/edxl_de#hasContentObject',
      '@type' : '@id'
    },
    'hasContentDescriptor' : {
      '@id' : 'http://fpc.ufba.br/ontologies/edxl_de#hasContentDescriptor',
      '@type' : '@id'
    },
    'contentData' : {
      '@id' : 'http://fpc.ufba.br/ontologies/edxl_de#contentData',
      '@type' : '@id'
    },
    'rdf' : 'http://www.w3.org/1999/02/22-rdf-syntax-ns#',
    'owl' : 'http://www.w3.org/2002/07/owl#',
    'xsd' : 'http://www.w3.org/2001/XMLSchema#',
    'rdfs' : 'http://www.w3.org/2000/01/rdf-schema#',
    'edxl_cap' : 'http://fpc.ufba.br/ontologies/edxl_cap#',
    'edxl_cap_inst' : 'http://fpc.ufba.br/ontologies/edxl_cap/instances#',
    'edxl_de' : 'http://fpc.ufba.br/ontologies/edxl_de#',
    'edxl_de_inst' : 'http://fpc.ufba.br/ontologies/edxl_de/instances#'
}
            ";

            JObject contextObj = JObject.Parse(context);

            return contextObj;
        }

    }
}

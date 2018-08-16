using ContextManager.DataObjects.EDXL.InferenceHandler;
using EDXLSharp;
using EDXLSharp.CAPLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContextManager.DataObjects.EDXL
{
    /// <summary>
    /// 1.1    Purpose
    ///     The Common Alerting Protocol(CAP) provides an open, non-proprietary digital message format for all types of alerts and notifications.
    ///     It does not address any particular application or telecommunications method.The CAP format is compatible with emerging techniques, 
    ///     such as Web services, as well as existing formats including the Specific Area Message Encoding (SAME) used for the United States’ 
    ///     National Oceanic and Atmospheric Administration(NOAA) Weather Radio and the Emergency Alert System(EAS), while offering enhanced 
    ///     capabilities that include:
    /// ·         Flexible geographic targeting using latitude/longitude shapes and other geospatial representations in three dimensions;
    /// ·         Multilingual and multi-audience messaging;
    /// ·         Phased and delayed effective times and expirations;
    /// ·         Enhanced message update and cancellation features;
    /// ·         Template support for framing complete and effective warning messages;
    /// ·         Compatible with digital signature capability; and,
    /// ·         Facility for digital images and audio.
    ///     Key benefits of CAP will include reduction of costs and operational complexity by eliminating the need for multiple custom software 
    ///     interfaces to the many warning sources and dissemination systems involved in all-hazard warning. The CAP message format can be converted 
    ///     to and from the “native” formats of all kinds of sensor and alerting technologies, forming a basis for a technology-independent national 
    ///     and international “warning internet.”
    ///     
    /// 1.4     Applications of the CAP Alert Message
    ///     The primary use of the CAP Alert Message is to provide a single input to activate all kinds of alerting and public warning systems.
    ///     This reduces the workload associated with using multiple warning systems while enhancing technical reliability and target-audience 
    ///     effectiveness.It also helps ensure consistency in the information transmitted over multiple delivery systems, another key to warning 
    ///     effectiveness.
    ///     A secondary application of CAP is to normalize warnings from various sources so they can be aggregated and compared in tabular 
    ///     or graphic form as an aid to situational awareness and pattern detection.
    ///     Although primarily designed as an interoperability standard for use among warning systems and other emergency information systems, 
    ///     the CAP Alert Message can be delivered directly to alert recipients over various networks, including data broadcasts.Location-aware 
    ///     receiving devices could use the information in a CAP Alert Message to determine, based on their current location, whether that particular 
    ///     message was relevant to their users.
    ///     The CAP Alert Message can also be used by sensor systems as a format for reporting significant events to collection and analysis systems and centers.
    /// 
    /// </summary>
    public class CAP
    {
        private EDXLSharp.CAPLib.CAP msgCAP;

        public EDXLSharp.CAPLib.CAP CAPelement
        {
            get
            {
                return msgCAP;
            }
        }

        private Dictionary<String, object> attributesDataFromSituationIdentified;
        private string situationTypeIdentified;
        private SituationInference situationInference;


        public CAP(string situationType, Dictionary<String, object> attributesFromEPL, SituationInference situationInference)
        {
            msgCAP = new EDXLSharp.CAPLib.CAP();
            attributesDataFromSituationIdentified = attributesFromEPL;
            this.situationTypeIdentified = situationType;
            this.situationInference = situationInference;

            string messageID = "edxl_cap_inst:AlertMessage_" + situationTypeIdentified + "_" + attributesDataFromSituationIdentified["TriggerEventBegin"] + "_" + Guid.NewGuid().ToString();
            string senderID = "INTER-IoT-EWS (semiotics-iot.eu): Port of Valencia";
                        
            SetMandatoryFields(messageID, senderID);
        }

        private void SetMandatoryFields(string messageID, string senderID)
        {
            msgCAP.MessageID = messageID;
            msgCAP.SenderID = senderID;

            msgCAP.MessageStatus = StatusType.Exercise;
            msgCAP.MessageType = MsgType.Alert;
            msgCAP.Scope = ScopeType.Restricted;

            // Restriction: "The text describing the rule for limiting distribution of the restricted alert message" 
            msgCAP.Restriction = "Only for INTER-IoT-EWS tests"; 

        }

        public void AddAllTripData(string targetName)
        {
            SetAllFields(targetName);
        }

        private void SetAllFields(string targetName)
        {
            // Addresses: "The group listing of intended recipients of the alert message" 
            msgCAP.Addresses = targetName;

            // Optional fields:

            // Source: "The text identifying the source of the alert message"
            msgCAP.Source = msgCAP.SenderID;

            // Note: "The text describing the purpose or significance of the alert message"
            msgCAP.Note = "Exercise";

            // TODO: check the use of these fields
            //msgCAP.HandlingCode = "Code handling?"; 
            //msgCAP.ReferenceIDs = "MessageID from other CAP messages"; ==> check format
            //msgCAP.IncidentIDs = "IncidentID (can generate...)"; ==> check format
            
            InfoType capInfoType = new InfoType();
            capInfoType.Category = GetCategoryTypes();

            capInfoType.Event = "Activation (trigger) event of " + situationTypeIdentified;

            EmergencyInformation emergencySituationInfo = situationInference.GetSeverityAndUrgency();
            capInfoType.Severity = emergencySituationInfo.Severity;
            capInfoType.Urgency = emergencySituationInfo.Urgency;
            capInfoType.Headline = emergencySituationInfo.Description;
            capInfoType.Parameter = GetParameters();

            capInfoType.Certainty = GetCertaintyType();

            capInfoType.ResponseType = GetResponseTypes();

            capInfoType.Audience = "Target XX";

            capInfoType.EventCode = GetPortValenciaEventCodes();

            // The effective time of the information of the alert message
            capInfoType.Effective = GetDateTimeSituationDetected();

            // The expected time of the beginning of the subject event of the alert message
            capInfoType.OnSet = GetDateTimeTriggerEventBegin();

            capInfoType.Expires = GetDateTimeExpires();

            capInfoType.SenderName = "INTER-IoT-EWS (semiotics-iot.eu)";
            
            capInfoType.Description = "Generated from situation identified: " + situationTypeIdentified;

            // The text describing the recommended action to be taken by recipients of the alert message
            capInfoType.Instruction = "Reaction processes";

            capInfoType.Web = new Uri("http://semiotics-iot.eu/"); // TODO: add to web.config

            capInfoType.Contact = "Joao Moreira (j.luizrebelomoreira@utwente.nl)"; // TODO: add to web.config (is it the contact about the accident?)

            capInfoType.Area = GetLocations();

            capInfoType.Resource = GetResources();
            
            msgCAP.Info.Add(capInfoType);

        }

        private List<NameValueType> GetParameters()
        {
            List<NameValueType> result = new List<NameValueType>();

            foreach (string key in attributesDataFromSituationIdentified.Keys)
            {
                NameValueType nameValueType = new NameValueType();
                nameValueType.Name = key;
                nameValueType.Value = attributesDataFromSituationIdentified[key].ToString();
                result.Add(nameValueType);
            }

            return result;
        }

        private List<CategoryType> GetCategoryTypes()
        {
            List<CategoryType> result = new List<CategoryType>();
            result.Add(CategoryType.Transport);
            result.Add(CategoryType.Safety);

            if (situationTypeIdentified.StartsWith("UC02") 
                || situationTypeIdentified.StartsWith("UC03"))
                result.Add(CategoryType.Health);

            // Dangerous goods (UC05)
            if (situationTypeIdentified.StartsWith("UC05"))
                result.Add(CategoryType.CBRNE);

            // UC05?
            //result.Add(CategoryType.Fire);

            // Vehicle collision depending on the severity/urgency??
            //result.Add(CategoryType.Rescue);

            return result;
        }


        private List<AreaType> GetLocations()
        {
            List<AreaType> result = new List<AreaType>();

            AreaType areaType = new AreaType();
            string lat = attributesDataFromSituationIdentified["latitude"].ToString();
            string lon = attributesDataFromSituationIdentified["longitude"].ToString();

            areaType.AreaDesc = "Point(" + lat + " " + lon + ")";

            areaType.GeoCode = new List<NameValueType>();

            //List<string> circle = new List<string>();
            //circle.Add("52.2390,6.8573 0");
            //circle.Add("0");
            //areaType.Circle = circle;

            result.Add(areaType);

            return result;
        }

        private CertaintyType? GetCertaintyType()
        {
            return CertaintyType.Possible;
        }


        private List<ResponseType> GetResponseTypes()
        {
            // TODO: depends on the situation response process (reaction)
            List<ResponseType> result = new List<ResponseType>();
            result.Add(ResponseType.Monitor);
            //result.Add(ResponseType.Evacuate); // Depending on UCXX x thresholds (table)? 

            return result;
        }

        private List<ResourceType> GetResources()
        {
            List<ResourceType> result = new List<ResourceType>();

            ResourceType healthData = new ResourceType();
            healthData.CapNamespace = "ResourceType_01";
            healthData.ResourceDesc = "Data collected during the trip";
            healthData.MimeType = "application/ld+json";

            // Link to get from Context Manager REST
            healthData.Uri_var = new Uri("https://inter-iot-ews-contextmanagerrest-v0.azurewebsites.net/api/deviceobservations/deviceId/tripId");

            // Or content in the alert: should be dependent of the size of the message
            //healthData.DerefUri = "json-ld with all health data: collection of documents from MongoDB (filter:deviceId, tripId)";

            result.Add(healthData);

            return result;
        }
        
        private DateTime GetDateTimeExpires()
        {
            // TODO: Set a default (configurable: web.config)
            return DateTime.Now.AddDays(2);
        }

        private DateTime GetDateTimeSituationDetected()
        {
            // TODO: Best option would be the datetime got in the EventProcessor.defaultUpdateEventHandler (add to the Situation object)
            DateTime result = (DateTime)situationInference.AttributesSituationIdentified["SituationBegin"];
            return result;
        }

        private DateTime GetDateTimeTriggerEventBegin()
        {
            // TODO: Get from o.resultTime (event that triggered the situation identification)
            DateTime result = (DateTime)situationInference.AttributesSituationIdentified["TriggerEventBegin"];
            return result;
        }

        private List<NameValueType> GetPortValenciaEventCodes()
        {
            // SAME classification: https://en.wikipedia.org/wiki/Specific_Area_Message_Encoding

            List<NameValueType> result = new List<NameValueType>();

            NameValueType code01 = new NameValueType();
            code01.Name = "LAE";
            code01.Value = "Local Area Emergency";
            result.Add(code01);

            NameValueType code02 = new NameValueType();
            code02.Name = "HMW";
            code02.Value = "Hazardous Materials Warning";
            result.Add(code02);

            return result;
        }

        public JObject GetJsonLD()
        {
            JObject context = GetContextJSON_EDXL();

            JObject edxlMessage = new JObject();            
            edxlMessage.Add("@context", context);
            edxlMessage.Add("@id", msgCAP.MessageID);
            edxlMessage.Add("@type", "edxl_cap:AlertMessage");
            edxlMessage.Add("rdfs:label", "[INTER-IoT-EWS] Early Warning: " + situationTypeIdentified);

            AddInfoTypeElements(edxlMessage);

            AddAlertElements(edxlMessage);
            
            return edxlMessage;
        }

        private void AddAlertElements(JObject edxlMessage)
        {
            edxlMessage.Add("edxl_cap:identifier", msgCAP.MessageID);
            edxlMessage.Add("edxl_cap:hasSender", ConvertToInstanceInJsonLD(msgCAP.SenderID, "edxl_cap:Sender"));
            edxlMessage.Add("edxl_cap:sentTime", DateTime.Now);
            edxlMessage.Add("edxl_cap:hasStatus", ConvertToInstanceInJsonLD(msgCAP.MessageStatus.ToString(), "['edxl_cap:Exercise', 'edxl_cap:Status']"));
            edxlMessage.Add("edxl_cap:hasMsgType", ConvertToInstanceInJsonLD(msgCAP.MessageType.ToString(), "['edxl_cap:AlertType', 'edxl_cap:MsgType']"));
            edxlMessage.Add("edxl_cap:hasScope", ConvertToInstanceInJsonLD(msgCAP.Scope.ToString(), "['edxl_cap:Restricted', 'edxl_cap:Scope']"));
            edxlMessage.Add("edxl_cap:addresses", msgCAP.Addresses);
            edxlMessage.Add("edxl_cap:source", msgCAP.Source);
            edxlMessage.Add("edxl_cap:note", msgCAP.Note);

        }

        private void AddInfoTypeElements(JObject edxlMessage)
        {
            JArray arryOfInfoElements = new JArray();
            int i = 0;
            foreach (InfoType infoTypeElement in msgCAP.Info)
            {
                JObject infoTypeElementJsonLD = new JObject();

                infoTypeElementJsonLD.Add("@id", "edxl_cap_inst:Info_" + msgCAP.MessageID.Replace("edxl_cap_inst:", string.Empty) + "_" + ++i);
                infoTypeElementJsonLD.Add("@type", "edxl_cap:Info");
                infoTypeElementJsonLD.Add("edxl_cap:hasCategory", GetCategoryElementsAsJsonLD(infoTypeElement.Category));
                infoTypeElementJsonLD.Add("edxl_cap:event", infoTypeElement.Event);
                infoTypeElementJsonLD.Add("edxl_cap:hasUrgency", ConvertToInstanceInJsonLD(infoTypeElement.Urgency.ToString() + "_" + (int)infoTypeElement.Urgency, "['edxl_cap:Urgency','edxl_cap:" + infoTypeElement.Urgency.ToString() + "']"));
                infoTypeElementJsonLD.Add("edxl_cap:hasSeverity", ConvertToInstanceInJsonLD(infoTypeElement.Severity.ToString() + "_" + (int)infoTypeElement.Severity, "['edxl_cap:Severity','edxl_cap:" + infoTypeElement.Severity.ToString() + "']"));
                infoTypeElementJsonLD.Add("edxl_cap:hasCertainty", ConvertToInstanceInJsonLD(infoTypeElement.Certainty.ToString() + "_" + (int)infoTypeElement.Certainty, "['edxl_cap:Certainty','edxl_cap:" + infoTypeElement.Certainty.ToString() + "']"));
                infoTypeElementJsonLD.Add("edxl_cap:hasResponseType", GetResponseElementsAsJsonLD(infoTypeElement.ResponseType));
                infoTypeElementJsonLD.Add("edxl_cap:audience", infoTypeElement.Audience);
                infoTypeElementJsonLD.Add("edxl_cap:eventCode", GetEventCodeElementsAsJsonLD(infoTypeElement.EventCode));
                infoTypeElementJsonLD.Add("edxl_cap:effective", infoTypeElement.Effective); // TODO: transform to xsd date/time
                infoTypeElementJsonLD.Add("edxl_cap:onset", infoTypeElement.OnSet); // TODO: transform to xsd date/time
                infoTypeElementJsonLD.Add("edxl_cap:expires", infoTypeElement.Expires); // TODO: transform to xsd date/time
                infoTypeElementJsonLD.Add("edxl_cap:senderName", infoTypeElement.SenderName); 
                infoTypeElementJsonLD.Add("edxl_cap:headline", infoTypeElement.Headline);
                infoTypeElementJsonLD.Add("edxl_cap:description", infoTypeElement.Description);
                infoTypeElementJsonLD.Add("edxl_cap:instruction", infoTypeElement.Instruction);
                infoTypeElementJsonLD.Add("edxl_cap:webInfo", infoTypeElement.Web);
                infoTypeElementJsonLD.Add("edxl_cap:contact", infoTypeElement.Contact);
                infoTypeElementJsonLD.Add("edxl_cap:hasArea", GetAreaElementsAsJsonLD(infoTypeElement.Area));
                infoTypeElementJsonLD.Add("edxl_cap:hasResource", GetResourceElementsAsJsonLD(infoTypeElement.Resource));

                JArray parameters = new JArray();
                foreach (NameValueType nameValueType in infoTypeElement.Parameter)
                {
                    JObject param = new JObject();
                    param.Add("@type", "owl:Thing");
                    param.Add("@id", Guid.NewGuid());
                    param.Add("xsd:Name", nameValueType.Name);
                    param.Add("rdf:value", nameValueType.Value);
                    parameters.Add(param);

                }
                infoTypeElementJsonLD.Add("edxl_cap:parameter", parameters);


                arryOfInfoElements.Add(infoTypeElementJsonLD);
            }

            edxlMessage.Add("edxl_cap:hasInfo", arryOfInfoElements);
        }

        private JToken GetAreaElementsAsJsonLD(List<AreaType> areas)
        {
            JArray result = new JArray();

            foreach (AreaType areaType in areas)
            {
                string id = "edxl_cap_inst:" + areaType.AreaDesc.Trim().Replace("(", "_").Replace(")", "_").Replace(" ", "_");

                JObject areaObj = new JObject();
                areaObj.Add("@id", id); 
                areaObj.Add("@type", "edxl_cap:Area");
                areaObj.Add("edxl_cap:areaDesc", areaType.AreaDesc);
                string geoCode = string.Empty;
                foreach (NameValueType nameValueType in areaType.GeoCode)
                {
                    geoCode += nameValueType.Name + ":" + nameValueType.Value + ",";
                }
                areaObj.Add("edxl_cap:areaGeoCode", geoCode); 

                result.Add(areaObj);
            }

            return result;
        }

        private JToken GetCategoryElementsAsJsonLD(List<CategoryType> categories)
        {
            JArray result = new JArray();

            foreach (CategoryType categType in categories)
            {
                JObject jsonObj = new JObject();
                jsonObj.Add("@id", "edxl_cap_inst:" + categType.ToString() + "_" + (int)categType);

                string categSubKind = "edxl_cap:" + categType.ToString();
                jsonObj.Add("@type", JArray.Parse("['edxl_cap:Category', '" + categSubKind + "']"));

                result.Add(jsonObj);
            }

            return result;
        }

        private JToken GetResponseElementsAsJsonLD(List<ResponseType> responses)
        {
            JArray result = new JArray();

            foreach (ResponseType respType in responses)
            {
                JObject jsonObj = new JObject();
                jsonObj.Add("@id", "edxl_cap_inst:" + respType.ToString() + "_" + (int)respType);
                string subKind = "edxl_cap:" + respType.ToString();
                jsonObj.Add("@type", JArray.Parse("['edxl_cap:ResponseType', '" + subKind + "']"));

                result.Add(jsonObj);
            }

            return result;
        }

        private JToken GetEventCodeElementsAsJsonLD(List<NameValueType> codes)
        {
            JArray result = new JArray();

            foreach (NameValueType nameValue in codes)
            {
                JObject jsonObj = new JObject();
                jsonObj.Add("@id", "edxl_cap_inst:EventCode_" + nameValue.Name);
                jsonObj.Add("rdfs:label", nameValue.Value);
                
                result.Add(jsonObj);
            }

            return result;
        }

        private JToken GetResourceElementsAsJsonLD(List<ResourceType> resources)
        {
            JArray result = new JArray();

            foreach (ResourceType resType in resources)
            {
                JObject jsonObj = new JObject();
                jsonObj.Add("@id", "edxl_cap_inst:" + resType.CapNamespace); // TODO: check
                jsonObj.Add("@type", "edxl_cap:Resource");
                if (resType.DerefUri != null) jsonObj.Add("edxl_cap:derefUri", resType.DerefUri);
                if (resType.MimeType != null) jsonObj.Add("edxl_cap:mimeType", resType.MimeType);
                if (resType.ResourceDesc != null) jsonObj.Add("edxl_cap:resourceDesc", resType.ResourceDesc);
                if (resType.Digest != null) jsonObj.Add("edxl_cap:digest", resType.Digest);
                if (resType.Size != null) jsonObj.Add("edxl_cap:resourceSize", resType.Size);
                if (resType.Uri_var != null) jsonObj.Add("edxl_cap:resourceURI", resType.Uri_var);

                result.Add(jsonObj);
            }

            return result;
        }

        public JToken ConvertToInstanceInJsonLD(string instanceID, string instanceType)
        {
            JObject result = new JObject();
            result.Add("@id", "edxl_cap_inst:" + instanceID);

            if (instanceType.StartsWith("["))
                result.Add("@type", JArray.Parse(instanceType));
            else
                result.Add("@type", instanceType);

            return result;
        }


        public JObject GetContextJSON_EDXL()
        {
            // <http://fpc.ufba.br/ontologies/edxl_cap>

            string context = @"
{
    'first' : {
      '@id' : 'http://www.w3.org/1999/02/22-rdf-syntax-ns#first',
      '@type' : '@id'
    },
    'rest' : {
      '@id' : 'http://www.w3.org/1999/02/22-rdf-syntax-ns#rest',
      '@type' : '@id'
    },
    'onProperty' : {
      '@id' : 'http://www.w3.org/2002/07/owl#onProperty',
      '@type' : '@id'
    },
    'someValuesFrom' : {
      '@id' : 'http://www.w3.org/2002/07/owl#someValuesFrom',
      '@type' : '@id'
    },
    'onClass' : {
      '@id' : 'http://www.w3.org/2002/07/owl#onClass',
      '@type' : '@id'
    },
    'maxQualifiedCardinality' : {
      '@id' : 'http://www.w3.org/2002/07/owl#maxQualifiedCardinality',
      '@type' : 'http://www.w3.org/2001/XMLSchema#nonNegativeInteger'
    },
    'intersectionOf' : {
      '@id' : 'http://www.w3.org/2002/07/owl#intersectionOf',
      '@type' : '@id'
    },
    'equivalentClass' : {
      '@id' : 'http://www.w3.org/2002/07/owl#equivalentClass',
      '@type' : '@id'
    },
    'comment' : {
      '@id' : 'http://www.w3.org/2000/01/rdf-schema#comment'
    },
    'identifier' : {
      '@id' : 'http://fpc.ufba.br/ontologies/edxl_cap#identifier'
    },
    'hasScope' : {
      '@id' : 'http://fpc.ufba.br/ontologies/edxl_cap#hasScope',
      '@type' : '@id'
    },
    'hasInfo' : {
      '@id' : 'http://fpc.ufba.br/ontologies/edxl_cap#hasInfo',
      '@type' : '@id'
    },
    'hasSender' : {
      '@id' : 'http://fpc.ufba.br/ontologies/edxl_cap#hasSender',
      '@type' : '@id'
    },
    'source' : {
      '@id' : 'http://fpc.ufba.br/ontologies/edxl_cap#source'
    },
    'sentTime' : {
      '@id' : 'http://fpc.ufba.br/ontologies/edxl_cap#sentTime',
      '@type' : 'http://www.w3.org/2001/XMLSchema#dateTime'
    },
    'label' : {
      '@id' : 'http://www.w3.org/2000/01/rdf-schema#label'
    },
    'addresses' : {
      '@id' : 'http://fpc.ufba.br/ontologies/edxl_cap#addresses'
    },
    'hasStatus' : {
      '@id' : 'http://fpc.ufba.br/ontologies/edxl_cap#hasStatus',
      '@type' : '@id'
    },
    'hasMsgType' : {
      '@id' : 'http://fpc.ufba.br/ontologies/edxl_cap#hasMsgType',
      '@type' : '@id'
    },
    'note' : {
      '@id' : 'http://fpc.ufba.br/ontologies/edxl_cap#note'
    },
	'eventCode' : {
      '@id' : 'http://fpc.ufba.br/ontologies/edxl_cap#eventCode'
    },
    'senderName' : {
      '@id' : 'http://fpc.ufba.br/ontologies/edxl_cap#senderName'
    },	
    'rdf' : 'http://www.w3.org/1999/02/22-rdf-syntax-ns#',
    'owl' : 'http://www.w3.org/2002/07/owl#',
    'xsd' : 'http://www.w3.org/2001/XMLSchema#',
    'rdfs' : 'http://www.w3.org/2000/01/rdf-schema#',
    'edxl_cap' : 'http://fpc.ufba.br/ontologies/edxl_cap#',
    'edxl_cap_inst' : 'http://fpc.ufba.br/ontologies/edxl_cap/instances#'
}
            ";

            JObject contextObj = JObject.Parse(context);

            return contextObj;
        }
        
    }
}

using ContextManager.DataObjects.EDXL;
using INTERIoTEWS.SituationIdentificationManager.SituationIdentificationREST.SituationIdentification;
using INTERIoTEWS.SituationIdentificationManager.SituationIdentificationREST.Util;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace INTERIoTEWS.SituationIdentificationManager.SituationIdentificationREST.SituationReaction
{
    public class NotificationActivity
    {
        public string Name { get; set; }
        public List<TargetNotificationRequirement> TargetNotificationRequirements { get; set; }

        public NotificationActivity(string name)
        {
            this.Name = name;
            TargetNotificationRequirements = new List<TargetNotificationRequirement>();

            // Default requirement
            //TargetNotificationRequirement targetNotificationRequirement = new SituationReaction.TargetNotificationRequirement("Default", TargetNotificationRequirement.TargetNotificationRequirementTypeEnum.AllTripData);
            //TargetNotificationRequirements.Add(targetNotificationRequirement);
        }

        public void Execute(Situation situationIdentified)
        {
            CAP edxlCAP = new CAP(situationIdentified.Type.Name, situationIdentified.AttributesFromEPL, situationIdentified.SituationInference);
                        
            // Add target requirements to the message
            foreach (TargetNotificationRequirement requirement in TargetNotificationRequirements)
            {
                switch (requirement.TargetNotificationRequirementType)
                {
                    case TargetNotificationRequirement.TargetNotificationRequirementTypeEnum.AllTripData:
                        edxlCAP.AddAllTripData(requirement.TargetName);
                        break;
                    case TargetNotificationRequirement.TargetNotificationRequirementTypeEnum.AllHealthDataFromTrip:
                        break;
                    default:
                        break;
                }

                if (requirement.TargetNotificationRequirementType == TargetNotificationRequirement.TargetNotificationRequirementTypeEnum.AllTripData)
                    break;
            }

            DE edxlDE = new DE(edxlCAP);

            //JObject jsonLd = edxlCAP.GetJsonLD();
            JObject jsonLd = edxlDE.GetJsonLD();

            AzureIoT azureIoT = new AzureIoT();
            azureIoT.SendToAzureIoTHub(jsonLd);
        }

        public void ExecuteOld()
        {

            string edxlSitRep = @"
{
  '@id' : '1234',
  '@type' : 'edxl:SitRep',
  'label' : 'Test result'
}
            ";

            // http://w3id.org/concorde/ontology/EDXL#Transport


            foreach (TargetNotificationRequirement requirement in TargetNotificationRequirements)
            {
                //edxlSitRep += ", dataElement"...
            }

            JObject edxlSitRepJSON = JObject.Parse(edxlSitRep);

            AzureIoT azureIoT = new AzureIoT();
            //azureIoT.SendToAzureIoTHub(edxlSitRepJSON);
            /*
            CAPmessage edxlCAP = new CAPmessage();
            edxlSitRep = edxlCAP.Test05();

            azureIoT.SendToAzureIoTHub(edxlSitRep);*/
        }
    }
}

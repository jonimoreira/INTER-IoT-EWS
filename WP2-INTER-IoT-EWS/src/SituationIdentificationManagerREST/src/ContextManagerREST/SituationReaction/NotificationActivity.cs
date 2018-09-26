using ContextManager.DataObjects.EDXL;
using INTERIoTEWS.SituationIdentificationManager.SituationIdentificationREST.SituationIdentification;
using INTERIoTEWS.SituationIdentificationManager.SituationIdentificationREST.Util;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
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

            // Example of multiple channels
            SendMessageByEmail(jsonLd, edxlCAP);
            SendMessageToIoTHub(jsonLd);

            
        }

        private async Task SendMessageToIoTHub(JObject jsonLd)
        {
            AzureIoT azureIoT = new AzureIoT();
            azureIoT.SendToAzureIoTHub(jsonLd);
        }

        private async Task SendMessageByEmail(JObject jsonLd, CAP edxlCAP)
        {
            SmtpClient client = new SmtpClient();
            client.Port = 587;
            client.Host = "smtp.gmail.com";
            client.EnableSsl = true;
            client.Timeout = 10000;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Credentials = new System.Net.NetworkCredential("inter.iot.ews@gmail.com", "sdddddddsdsdsdsdsd");

            string emailBody = @"
                ATTENTION: INTER-IoT-EWS just issued an warning about an accident risk or accident detected!
                Severity: " + edxlCAP.Severity + @"
                Urgency: " + edxlCAP.Urgency + @"
                TripId: " + edxlCAP.TripId + @"
                Location: " + edxlCAP.Location + @"
                Response process: " + edxlCAP.Instruction + @"
            
                Data about the situation identified: " + jsonLd.ToString(Newtonsoft.Json.Formatting.Indented) + @"

                All data regarding the trip is available to download through the INTER-IoT-EWS REST: https://inter-iot-ews-contextmanagerrest-v0.azurewebsites.net/api/trip/" + edxlCAP.TripId;

            MailMessage mm = new MailMessage("inter.iot.ews@gmail.com", GetEmailsToNotify(), "[INTER-IoT-EWS] Warning message: " + edxlCAP.Headline, emailBody);
            mm.BodyEncoding = UTF8Encoding.UTF8;
            mm.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;

            client.Send(mm);
        }

        private string GetEmailsToNotify()
        {
            // Here we can specify email addresses for each notification activity with channel 'email': ideally this would be implemented with a relational DB
            return "jonimoreira@gmail.com,inter.iot.ews@gmail.com";
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

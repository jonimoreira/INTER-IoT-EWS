using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ContextManagerREST.CEP.SituationReaction
{
    public class NotificationActivity
    {
        public string Name { get; set; }
        public List<TargetNotificationRequirement> TargetNotificationRequirements { get; set; }

        public NotificationActivity(string name)
        {
            this.Name = name;
            TargetNotificationRequirements = new List<TargetNotificationRequirement>();
        }

        public void Execute()
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

            AzureIoT azureIoT = new ContextManagerREST.AzureIoT();
            azureIoT.SendToAzureIoTHub(edxlSitRepJSON);
        }
    }
}

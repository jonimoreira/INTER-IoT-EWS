using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text;
using Microsoft.Azure.Devices.Client;
using com.espertech.esper.client;
using INTERIoTEWS.SituationIdentificationManager.SituationIdentificationREST.Models;
using INTERIoTEWS.SituationIdentificationManager.SituationIdentificationREST.Util;
using INTERIoTEWS.Context.DataObjects.SOSA;

namespace INTERIoTEWS.SituationIdentificationManager.SituationIdentificationREST.Controllers
{
    [Route("api/[controller]")]
    public class DeviceObservationsController : Controller
    {
        // SQL Server (relational): once serialized in POCO for CEP (NESPER), can store (async) the structured data
        private EWSContext db = new EWSContext(); 

        // CEP server
        private EPServiceProvider epService = EPServiceProviderManager.GetDefaultProvider();

        // GET api/deviceobservations
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "ST1", "ST2" };
        }

        // GET api/deviceobservations/5
        [HttpGet("{deviceId}")]
        public string Get(string deviceId)
        {
            //new Task(() => { AzureIoT.ReceiveMessagesFromIoTHub(); }).Start();
            
            return "DeviceId: " + deviceId;
        }

        // PUT api/deviceobservations/5
        [HttpPut("{deviceId}")]
        public HttpResponseMessage Put(string deviceId, [FromBody]JToken value)
        {
            // When working with INTER-MW + IPSM, change for parsing the message and not execute the semantic translations here
            List<Observation> observations = SimulateInputHandler(value);

            //new Task(() => { ExecuteSituationIdentificationManager(value); }).Start();
            ExecuteSituationIdentificationManager(observations);

            return new HttpResponseMessage(HttpStatusCode.Created);
        }

        private List<Observation> SimulateInputHandler(JToken value)
        {
            SemanticTranslations translations = new SemanticTranslations(value);
            List<Observation> observations = translations.ExecuteMappings();
            return observations;
        }
        
        private void ExecuteSituationIdentificationManager(List<Observation> observations)
        {   
            foreach (Observation observation in observations)
            {
                SendEventToCEP(observation);
            }
        }
        
        /// <summary>
        /// Sends an event to the CEP Server
        /// </summary>
        /// <param name="obj">the event</param>
        public void SendEventToCEP(Observation obj)
        {
            epService.EPRuntime.SendEvent(obj);
        }

        private void SaveFile(string deviceId, JToken data)
        {
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            string filePath = @"D:\Projects\InterIOT\Workplan\WP2-INTER-IoT-EWS\data\ContextManagerREST_Device" + deviceId + "_" + unixTimestamp.ToString() + "_" + Guid.NewGuid() + ".json";
            using (StreamWriter outputFile = new StreamWriter(filePath))
            {
                outputFile.Write(data.ToString());
            }
        }
               

    }
}

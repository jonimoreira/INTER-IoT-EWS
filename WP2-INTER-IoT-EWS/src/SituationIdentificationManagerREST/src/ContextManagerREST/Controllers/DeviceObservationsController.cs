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
using INTERIoTEWS.SituationIdentificationManager.SituationIdentificationREST.SituationIdentification.CEP;

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
            string baseUrl = HttpContext.Request.Host.Value;
            return new string[] { "ST1", "ST2", baseUrl };
        }

        // GET api/deviceobservations/5
        [HttpGet("{deviceId}")]
        public string Get(string deviceId)
        {
            string result = "Id: " + deviceId;

            if (deviceId.StartsWith("UC0"))
            {
                EventProcessor eventProcessor = new EventProcessor();

                if (deviceId.Contains(","))
                {
                    List<string> statements = deviceId.Split(',').ToList();
                    foreach (string statement in statements)
                    {
                        eventProcessor.StopStatement(statement);
                    }
                }
                else
                {
                    eventProcessor.StopStatement(deviceId);
                }
                result = "EP statements stopped: " + deviceId;
                
            }
            else if (deviceId == "reset")
            {
                EventProcessor eventProcessor = new EventProcessor();
                eventProcessor.RestartAllStatements();
                result = "EP statements restarted";
            }

            return result;
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
            List<Observation> ordered = observations.OrderByDescending(x => x.Identifier).ToList();
            foreach (Observation observation in ordered)
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

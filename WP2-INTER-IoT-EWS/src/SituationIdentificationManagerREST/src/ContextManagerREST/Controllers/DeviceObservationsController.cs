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
using ContextManager.DataObjects.EDXL.InferenceHandler;
using System.Diagnostics;

namespace INTERIoTEWS.SituationIdentificationManager.SituationIdentificationREST.Controllers
{
    [Route("api/[controller]")]
    public class DeviceObservationsController : Controller
    {
        // SQL Server (relational): once serialized in POCO for CEP (NESPER), can store (async) the structured data
        private EWSContext db = new EWSContext(); 

        // CEP server
        private EPServiceProvider epService = EPServiceProviderManager.GetDefaultProvider();

        private static SemanticTranslationsMechanism SemanticTranslationsApproach = SemanticTranslationsMechanism.RawSPARQL;

        // GET api/deviceobservations
        [HttpGet]
        public IEnumerable<string> Get()
        {
            string baseUrl = HttpContext.Request.Host.Value;
            return new string[] { "ST1", "ST2", baseUrl };
        }

        // GET api/deviceobservations/5
        [HttpGet("{threshdoldAcceleration}/{thresholdBradycardia}/{thresholdTachycardia}")]
        public string Get(double threshdoldAcceleration, double thresholdBradycardia, double thresholdTachycardia)
        {
            string result = "threshdoldAcceleration:" + threshdoldAcceleration + ", thresholdBradycardia:" + thresholdBradycardia + ", thresholdTachycardia:" + thresholdTachycardia;
            
            SituationInference.thresholdAcceleration = threshdoldAcceleration;
            SituationInference.thresholdBradycardia = thresholdBradycardia;
            SituationInference.thresholdTachycardia = thresholdTachycardia;

            EventProcessor eventProcessor = new EventProcessor();
            eventProcessor.RecreateAllStatements();

            System.Diagnostics.Trace.TraceInformation("[SituationIdentificationManager] {threshdoldAcceleration}/{thresholdBradycardia}/{thresholdTachycardia}: " + result);

            return result;
        }

        // GET api/deviceobservations/5
        [HttpGet("{deviceId}")]
        public string Get(string deviceId)
        {
            string result = "Id: " + deviceId;

            System.Diagnostics.Trace.TraceInformation("[SituationIdentificationManager] api/deviceobservations/id: " + result);

            if (deviceId.StartsWith("UC0"))
            {
                EventProcessor eventProcessor = new EventProcessor();

                if (deviceId.Contains(","))
                {
                    List<string> statements = deviceId.Split(',').ToList();
                    foreach (string statement in statements)
                    {
                        string stopStatement = string.Empty;

                        switch (statement)
                        {
                            case "UC01_ST01":
                                stopStatement = "UC01_VehicleCollisionDetected_ST01";
                                break;
                            case "UC01_ST02":
                                stopStatement = "UC01_VehicleCollisionDetected_ST02";
                                break;
                            case "UC01_ST03":
                                stopStatement = "UC01_VehicleCollisionDetected_ST03";
                                break;
                            case "UC01_ST04":
                                stopStatement = "UC01_VehicleCollisionDetected_ST04";
                                break;
                            case "UC01_ST05":
                                stopStatement = "UC01_VehicleCollisionDetected_ST05";
                                break;
                            case "UC02_ST01":
                                stopStatement = "UC02_HealthEarlyWarningScore_ST01";
                                break;
                            case "UC02_ST02":
                                stopStatement = "UC02_HealthEarlyWarningScore_ST02";
                                break;
                            case "UC02_ST03":
                                stopStatement = "UC02_HealthEarlyWarningScore_ST03";
                                break;
                            case "UC02_ST04":
                                stopStatement = "UC02_HealthEarlyWarningScore_ST04";
                                break;
                            case "UC03_ST01":
                                stopStatement = "UC03_TemporalRelations_ST01";
                                break;
                            case "UC03_ST02":
                                stopStatement = "UC03_TemporalRelations_ST02";
                                break;
                            case "UC04_ST01":
                                stopStatement = "UC04_DangerousGoods_ST01";
                                break;
                            case "UC04_ST02":
                                stopStatement = "UC04_DangerousGoods_ST02";
                                break;
                            case "UC04_ST03":
                                stopStatement = "UC04_DangerousGoods_ST03";
                                break;
                            default:
                                break;
                        }


                        eventProcessor.StopStatement(stopStatement);
                    }
                }
                else
                {
                    eventProcessor.StopStatement(deviceId);
                }
                result = "EP statements stopped: " + deviceId;
                
            }
            else if (deviceId.StartsWith("reset"))
            {
                EventProcessor eventProcessor = new EventProcessor();
                eventProcessor.RestartAllStatements();
                result = "EP statements restarted";

                string[] translationMechanismArr = deviceId.Split('-').ToArray();
                if (translationMechanismArr.Length == 3)
                {
                    string translationMechanism = translationMechanismArr[2].Trim();

                    switch (translationMechanism)
                    {
                        case "1":
                            // default behaviour with semantic translations executed at the Situation Manager with SPARQL queries
                            break;
                        case "2":
                            // call IPSM for semantic translations
                            SemanticTranslationsApproach = SemanticTranslationsMechanism.IPSM;
                            break;
                        default:
                            break;
                    }
                }

            }

            System.Diagnostics.Trace.TraceInformation("[SituationIdentificationManager] End: " + result);
            
            return result;
        }

        // PUT api/deviceobservations/5
        [HttpPut("{deviceId}")]
        public HttpResponseMessage Put(string deviceId, [FromBody]JToken value)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            System.Diagnostics.Trace.TraceInformation("[SituationIdentificationManager] Recived message: " + deviceId + ". Translations approach: " + SemanticTranslationsApproach.ToString());

            // When working with INTER-MW + IPSM, change for parsing the message and not execute the semantic translations here
            List<Observation> observations = new List<Observation>();

            if (SemanticTranslationsApproach == SemanticTranslationsMechanism.RawSPARQL)
                observations = SimulateInputHandler(value);
            else if (SemanticTranslationsApproach == SemanticTranslationsMechanism.IPSM)
                observations = PostTranslationToIPSM(value);
            else if (SemanticTranslationsApproach == SemanticTranslationsMechanism.INTER_MW_and_IPSM)
                AddObservationOfDataTranslatedWithIPSM(string.Empty, observations);

            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            System.Diagnostics.Trace.TraceInformation("[SituationIdentificationManager] After semantic translations executed. observations.Count: " + observations.Count + ". Elapsed time =" + elapsedTime);

            //new Task(() => { ExecuteSituationIdentificationManager(value); }).Start();
            ExecuteSituationIdentificationManager(observations);

            return new HttpResponseMessage(HttpStatusCode.Created);
        }

        private List<Observation> PostTranslationToIPSM(JToken value)
        {
            List<Observation> result = new List<Observation>();

            JObject messageFormattedINTER_IoT_GraphSrtucture = AddINTER_IoT_GraphSrtucture((JObject)value);

            //string url = "http://grieg.ibspan.waw.pl:8888/translation";
            string url = "http://168.63.44.177:8888/translation";

            // Prepare IPSM input message
            JObject align = new JObject();
            align.Add("name", "SAREF_CO");
            align.Add("version", "0.57");

            JArray aligns = new JArray();
            aligns.Add(align);

            JObject message = new JObject();
            message.Add("alignIDs", aligns);
            message.Add("graphStr", messageFormattedINTER_IoT_GraphSrtucture.ToString(Formatting.None));
            
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(message);
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var responseText = streamReader.ReadToEnd();

                // Data formatted according to iiot ontology (INTER-IoT): map to POCO Observations
                AddObservationOfDataTranslatedWithIPSM(responseText, result);

            }
            
            return result;
        }


        private JObject AddINTER_IoT_GraphSrtucture(JObject messageJson)
        {
            string withGraphs = @"
{
	'@graph': [
		{
			'@graph': [
				{
					'@id': 'InterIoTMsg:meta66b05c61-d687-45a3-b5fb-6864bbec3b69',
					'@type': [
						'InterIoTMsg:Platform_register',
						'InterIoTMsg:meta'
					],
					'InterIoTMsg:conversationID': 'conv99528eba-eb2d-47e8-9ee6-9dd40d19f89a',
					'InterIoTMsg:dateTimeStamp': '2017-05-22T22:19:30.281+02:00',
					'InterIoTMsg:messageID': 'msg7e484a2c-f959-486e-8da0-31143f457234'
				}
			],
			'@id': 'InterIoTMsg:metadata'
		},
		{
			'@graph': [
                " + messageJson.ToString() + @"
            ],
			'@id': 'InterIoTMsg:payload'
		}
	],
	'@context': {
		'InterIoTMsg': 'http://inter-iot.eu/message/',
		'InterIoT': 'http://inter-iot.eu/',
		'rdf': 'http://www.w3.org/1999/02/22-rdf-syntax-ns#',
		'rdfs': 'http://www.w3.org/2000/01/rdf-schema#',
		'xsd': 'http://www.w3.org/2001/XMLSchema#'
	}
}
            ";


            JObject result = JObject.Parse(withGraphs);

            return result;
        }
        

        private void AddObservationOfDataTranslatedWithIPSM(string responseText, List<Observation> result)
        {
            JObject response = JObject.Parse(responseText);
            JObject resultTranslation = new JObject();
            if (response["message"].ToString() == "Message translation successful")
            {
                resultTranslation = JObject.Parse(response["graphStr"].ToString());
                
                SaveFile("GOIoTP", resultTranslation);
                
                // TODO: test with UC01
                SemanticTranslations translations = new SemanticTranslations(resultTranslation);
                List<Observation> observations = translations.ExecuteMappings();
                result.AddRange(observations);
            }
            else
            {
                SaveFile("IPSM_error", response);
            }
        }

        private List<Observation> SimulateInputHandler(JToken value)
        {
            SemanticTranslations translations = new SemanticTranslations(value);
            List<Observation> observations = translations.ExecuteMappings();
            return observations;
        }
        
        private async Task ExecuteSituationIdentificationManager(List<Observation> observations)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            System.Diagnostics.Trace.TraceInformation("[SituationIdentificationManager] ExecuteSituationIdentificationManager Before syntax translations");

            List<Observation> ordered = observations.OrderByDescending(x => x.Identifier).ToList();
            foreach (Observation observation in ordered)
            {
                SendEventToCEP(observation);
            }

            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            System.Diagnostics.Trace.TraceInformation("[SituationIdentificationManager] ExecuteSituationIdentificationManager After syntax translations. Elapsed time =" + elapsedTime);

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

            string filePath = @"D:\Projects\InterIOT\Workplan\WP2-INTER-IoT-EWS\data\SituationIdentificationManager\" + deviceId + "_" + unixTimestamp.ToString() + "_" + Guid.NewGuid() + ".json";
            using (StreamWriter outputFile = new StreamWriter(filePath))
            {
                outputFile.Write(data.ToString());
            }
        }
               

    }
}

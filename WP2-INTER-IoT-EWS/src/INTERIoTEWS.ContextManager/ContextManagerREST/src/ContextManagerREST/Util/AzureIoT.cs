using Microsoft.Azure.Devices.Client;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;

namespace INTERIoTEWS.ContextManager.ContextManagerREST.Util
{
    public class AzureIoT
    {

        private string iotHubUri = "MyDrivingIoTHubEWS.azure-devices.net";
        private string deviceKey = "XXXXXXXXXXXXXXXX";
        private string deviceId = "XXXXXXXXXXXXXXXX";


        public async void SendToAzureIoTHub(JToken messageJson)
        {
            DeviceClient deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey), Microsoft.Azure.Devices.Client.TransportType.Http1);

            var messageString = messageJson.ToString(Formatting.None);
            var message = new Microsoft.Azure.Devices.Client.Message(Encoding.ASCII.GetBytes(messageString));

            await deviceClient.SendEventAsync(message);
        }

        public async void SendToAzureIoTHub(JObject messageJson)
        {
            DeviceClient deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey), Microsoft.Azure.Devices.Client.TransportType.Http1);

            var messageString = messageJson.ToString(Formatting.None);
            var message = new Microsoft.Azure.Devices.Client.Message(Encoding.ASCII.GetBytes(messageString));

            await deviceClient.SendEventAsync(message);
        }

        // To listen directly from Azure IoT Hub and call PUT /api/deviceobservations/{deviceId}
        
        static string connectionString = "XXXXXXXXXXXXXXXX";
        
        static string monitoringEndpointName = "nter-iot-ews-hub-02";

        static string iotHubD2cEndpoint = "messages/events";
        public static EventHubClient eventHubClient;


        public static void SimulateINTERIoT_MW(string hostValue)
        {
            System.Diagnostics.Trace.TraceInformation("[ContextManager] SimulateINTERIoT_MW: Start: " + ((hostValue == null) ? "hostValue is NULL" : hostValue));

            Host = hostValue;
            eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, iotHubD2cEndpoint);
            //eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, monitoringEndpointName);

            var d2cPartitions = eventHubClient.GetRuntimeInformation().PartitionIds;

            System.Diagnostics.Trace.TraceInformation("[ContextManager] SimulateINTERIoT_MW partitions: " + ((d2cPartitions == null) ? "d2cPartitions is NULL" : d2cPartitions.Length.ToString()));
            
            //CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            CancellationTokenSource cts = new CancellationTokenSource();

            var tasks = new List<Task>();
            foreach (string partition in d2cPartitions)
            {
                tasks.Add(ReceiveMessagesFromDeviceAsync(partition, cts.Token));
                //ReceiveMessagesFromDeviceAsync(partition, cts.Token);
            }

            Task.WaitAll(tasks.ToArray());

            System.Diagnostics.Trace.TraceInformation("[ContextManager] SimulateINTERIoT_MW created all threads");

        }

        /// <summary>
        /// https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-operations-monitoring
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private static async Task ReceiveMessagesFromDeviceAsync(string partition, CancellationToken ct)
        {
            var eventHubReceiver = eventHubClient.GetDefaultConsumerGroup().CreateReceiver(partition, DateTime.UtcNow);
            while (true)
            {

                if (ct.IsCancellationRequested)
                {
                    await eventHubReceiver.CloseAsync();
                    break;
                }

                EventData eventData = await eventHubReceiver.ReceiveAsync();

                if (eventData == null) continue;

                string data = Encoding.UTF8.GetString(eventData.GetBytes());
                //Console.WriteLine("Message received. Partition: {0} Data: '{1}'", partition, data);
                System.Diagnostics.Trace.TraceInformation("[ContextManager] ReceiveMessagesFromDeviceAsync data received from " + partition);

                JObject messageJson = JObject.Parse(data);

                string domain = GetDomainFromMessage(messageJson);

                ExecuteSemanticTranslationsAndSendToSituationIdentificationManager(domain, data);

                ManageContextData(domain, data, messageJson);

            }
            
        }




        private static string GetDomainFromMessage(JObject messageJson)
        {
            string domain = "unknown";

            if (messageJson["@type"] != null)
            {
                switch (messageJson["@type"].ToString())
                {
                    case "saref:Device":
                        domain = "health";
                        break;
                    case "edxl_cap:AlertMessage":
                    case "edxl_de:EDXLDistribution":
                        domain = "emergency";
                        break;
                    case "LogiTrans:TransportEvent":
                        domain = "logistics";
                        break;
                    default:
                        domain = "jsonld";
                        break;
                }
            }

            return domain;
        }

        private static async Task ManageContextData(string domain, string data, JObject messageJson)
        {
            // Save in MongoDB
            MongoDBContext mongoDB = new MongoDBContext();
            
            string tripId = string.Empty;
            string dataCollection = "tripId_";
            if (messageJson["@type"] != null)
            {
                switch (messageJson["@type"].ToString())
                {
                    case "saref:Device":
                        tripId = GetTripIdFromHealthMessage(data);
                        dataCollection += tripId + "_" + domain + "_DeviceObservations_SAREF4health";
                        break;
                    case "edxl_cap:AlertMessage":
                    case "edxl_de:EDXLDistribution":
                        tripId = GetTripIdFromEmergencyMessage(data);
                        dataCollection += tripId + "_" + domain + "_DeviceObservations_EDXLCAP";
                        break;
                    case "LogiTrans:TransportEvent":
                        tripId = GetTripIdFromLogisticsMessage(data);
                        dataCollection += tripId + "_" + domain + "_DeviceObservations_LogiCO";
                        break;
                    default:
                        dataCollection += tripId + "_" + domain + "_OtherMessages_JSONLD";
                        break;
                }
                
                mongoDB.SaveDocument(data, dataCollection);
            }
            else
            {
                mongoDB.SaveDocument(data, "OtherMessages");
            }
            
        }

        private static async Task ExecuteSemanticTranslationsAndSendToSituationIdentificationManager(string domain, string data)
        {
            if (domain != "unknown")
            {
                JObject messageJson = JObject.Parse(data);
                JObject messageFormattedINTER_IoT_GraphSrtucture = AddINTER_IoT_GraphSrtucture(messageJson);
                SendFormattedDataToSituationIdentifier(messageFormattedINTER_IoT_GraphSrtucture);
            }
        }

        private static string GetTripIdFromLogisticsMessage(string data)
        {
            string result = string.Empty;

            var jsonLdParser = new JsonLdParser();
            TripleStore tStore = new TripleStore();
            using (var reader = new System.IO.StringReader(data))
            {
                jsonLdParser.Load(tStore, reader);
            }

            string sparqlQuery = @"
                PREFIX LogiCO: <http://ontology.tno.nl/logico#>
                PREFIX LogiTrans: <http://ontology.tno.nl/transport#>
                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
                PREFIX dul: <http://www.ontologydesignpatterns.org/ont/dul/DUL.owl#>
                
                SELECT ?tripId
                WHERE  
	                {
					?transportEvent a LogiTrans:TransportEvent.
	                ?transportEvent dul:isComponentOf ?transport.
                    ?transport LogiCO:hasIDValue ?tripId
	                } 

            ";

            System.Diagnostics.Trace.TraceInformation("[ContextManager] GetTripIdFromLogisticsMessage: Before SPARQL");

            Object results = tStore.ExecuteQuery(sparqlQuery);

            System.Diagnostics.Trace.TraceInformation("[ContextManager] GetTripIdFromLogisticsMessage: After SPARQL");

            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;

                foreach (SparqlResult spqlResult in rset)
                {
                    LiteralNode labelValueNode = (LiteralNode)spqlResult.Value("tripId");
                    result = labelValueNode.Value;
                    break;
                }
            }

            System.Diagnostics.Trace.TraceInformation("[ContextManager] GetTripIdFromLogisticsMessage: After getting TripId: " + result);

            return result;
        }

        private static string GetTripIdFromEmergencyMessage(string data)
        {
            string result = string.Empty;

            var jsonLdParser = new JsonLdParser();
            TripleStore tStore = new TripleStore();
            using (var reader = new System.IO.StringReader(data))
            {
                jsonLdParser.Load(tStore, reader);
            }

            string sparqlQuery = @"
                
                PREFIX edxl_cap: <http://fpc.ufba.br/ontologies/edxl_cap#>
                PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>
                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                                
                SELECT ?tripId
                WHERE  
	                {
                    ?Info edxl_cap:parameter ?param.
                    ?param xsd:Name ?paramName.
                    ?param rdf:value ?tripId.
                    FILTER (?paramName='TripId'^^xsd:string)
                }

            ";

            System.Diagnostics.Trace.TraceInformation("[ContextManager] GetTripIdFromEmergencyMessage: Before SPARQL");

            Object results = tStore.ExecuteQuery(sparqlQuery);

            System.Diagnostics.Trace.TraceInformation("[ContextManager] GetTripIdFromEmergencyMessage: After SPARQL");
            
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;

                foreach (SparqlResult spqlResult in rset)
                {
                    LiteralNode labelValueNode = (LiteralNode)spqlResult.Value("tripId");
                    result = labelValueNode.Value;
                    break;
                }
            }

            System.Diagnostics.Trace.TraceInformation("[ContextManager] GetTripIdFromEmergencyMessage: After getting TripId: " + result);

            return result;
        }

        private static string GetTripIdFromHealthMessage(string data)
        {
            string result = string.Empty;

            var jsonLdParser = new JsonLdParser();
            TripleStore tStore = new TripleStore();
            using (var reader = new System.IO.StringReader(data))
            {
                jsonLdParser.Load(tStore, reader);
            }

            string sparqlQuery = @"
                PREFIX saref: <https://w3id.org/saref#>
                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                PREFIX geo: <http://www.w3.org/2003/01/geo/wgs84_pos#>
                PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
                
                SELECT ?device ?service ?label 
                WHERE  
	                {
					?device saref:offers ?service.
	                ?service a saref:Service.
                    ?service rdfs:label ?label
	                } 

            ";

            System.Diagnostics.Trace.TraceInformation("[ContextManager] GetTripIdFromHealthMessage: Before SPARQL");
            
            Object results = tStore.ExecuteQuery(sparqlQuery);

            System.Diagnostics.Trace.TraceInformation("[ContextManager] GetTripIdFromHealthMessage: After SPARQL");


            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;

                foreach (SparqlResult spqlResult in rset)
                {
                    LiteralNode labelValueNode = (LiteralNode)spqlResult.Value("label");
                    result = labelValueNode.Value;
                    break;
                }
            }

            System.Diagnostics.Trace.TraceInformation("[ContextManager] GetTripIdFromHealthMessage: After getting TripId: " + result);
            
            return result;
        }

        private static void SaveFile(string deviceId, JToken data)
        {
            if (!Host.ToLower().Contains("localhost"))
                return;

            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            string filePath = @"D:\Projects\InterIOT\Workplan\WP2-INTER-IoT-EWS\data\" + deviceId + "_" + unixTimestamp.ToString() + "_" + Guid.NewGuid() + ".json";
            using (StreamWriter outputFile = new StreamWriter(filePath))
            {
                outputFile.Write(data.ToString());
            }
        }

        private static void VerifyMessageTypeAndTakeAction(string data, MongoDBContext mongoDB, string tripId)
        {
            JObject messageJson = JObject.Parse(data);

            if (messageJson["@type"] != null)
            {
                switch (messageJson["@type"].ToString())
                {
                    case "saref:Device":
                        // Received from Shimmer3-LD app
                        JObject messageFormattedINTER_IoT_GraphSrtucture = AddINTER_IoT_GraphSrtucture(messageJson);

                        // Save in MongoDB
                        //mongoDB.SaveDocument(messageFormattedINTER_IoT_GraphSrtucture);
                        //string dataCollection = "tripId_" + tripId + "_INTERIoT_DeviceObservations_INTERMW_IPSM";
                        //new Task(() => { mongoDB.SaveDocument(messageFormattedINTER_IoT_GraphSrtucture, dataCollection); }).Start();

                        // Send to Situation Identifier Manager REST
                        //SendFormattedDataToSituationIdentifier(messageFormattedINTER_IoT_GraphSrtucture);
                        new Task(() => { PreProcessFormattedData(messageFormattedINTER_IoT_GraphSrtucture); }).Start();
                        
                        break;
                    case "edxl_cap:AlertMessage":

                        break;
                    default:
                        break;
                }

            }

        }

        private static void PreProcessFormattedData(JObject messageFormattedINTER_IoT_GraphSrtucture)
        {

            JObject translatedMessage = messageFormattedINTER_IoT_GraphSrtucture; // GetTranslatedMessageFromIPSM(messageFormattedINTER_IoT_GraphSrtucture);

            SendFormattedDataToSituationIdentifier(translatedMessage);

        }

        private static JObject GetTranslatedMessageFromIPSM(JObject messageFormattedINTER_IoT_GraphSrtucture)
        {
            try
            {
                string url = "http://grieg.ibspan.waw.pl:3000/translation/";
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "GET";

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(messageFormattedINTER_IoT_GraphSrtucture.ToString());
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var responseText = streamReader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[ContextManager] Error on[GetTranslatedMessageFromIPSM]:" + ex.Message + Environment.NewLine + "InnerException: " + ((ex.InnerException != null) ? ex.InnerException.Message : "NULL"));
            }
            return null;
        }

        public static string Host = "localhost";

        private static void SendFormattedDataToSituationIdentifier(JObject messageFormattedINTER_IoT_GraphSrtucture)
        {
            try
            {
                System.Diagnostics.Trace.TraceInformation("[ContextManager] SendFormattedDataToSituationIdentifier: Before sending");

                string url = "http://localhost:53269/api/deviceobservations/123";

                if (!Host.ToLower().Contains("localhost"))
                    url = "http://inter-iot-ews-situationidentificationmanagerrest-v0.azurewebsites.net/api/deviceobservations/123";

                var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "PUT";

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(messageFormattedINTER_IoT_GraphSrtucture.ToString());
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var responseText = streamReader.ReadToEnd();
                }
                System.Diagnostics.Trace.TraceInformation("[ContextManager] SendFormattedDataToSituationIdentifier: After sending");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[ContextManager] Error on[SendFormattedDataToSituationIdentifier]:" + ex.Message + Environment.NewLine + "InnerException: " + ((ex.InnerException != null) ? ex.InnerException.Message : "NULL"));
            }
        }

        private static JObject AddINTER_IoT_GraphSrtucture(JObject messageJson)
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

    }
}

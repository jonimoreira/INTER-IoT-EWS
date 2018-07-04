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

namespace INTERIoTEWS.ContextManager.ContextManagerREST.Util
{
    public class AzureIoT
    {

        private string iotHubUri = "XXXXXXXXXXXXXXXXXXXXXXXX";
        private string deviceKey = "XXXXXXXXXXXXXXXXXXXXXXXX";
        private string deviceId = "XXXXXXXXXXXXXXXXXXXXXXXX";


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
        //free tier: static string connectionString = "HostName=MyDrivingIoTHubEWS.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=tCI9KK86Dpkd2c22WbsRFzTQX0uMjrxnKzu1bpsM1ZI=";
        static string connectionString = "XXXXXXXXXXXXXXXXXXXXXXXX";
        static string iotHubD2cEndpoint = "messages/events";
        static EventHubClient eventHubClient;


        public static void SimulateINTERIoT_MW()
        {
            eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, iotHubD2cEndpoint);

            var d2cPartitions = eventHubClient.GetRuntimeInformation().PartitionIds;

            CancellationTokenSource cts = new CancellationTokenSource();

            var tasks = new List<Task>();
            foreach (string partition in d2cPartitions)
            {
                tasks.Add(ReceiveMessagesFromIoTHub(partition, cts.Token));
            }
            Task.WaitAll(tasks.ToArray());
        }

        private static async Task ReceiveMessagesFromIoTHub(string partition, CancellationToken ct)
        {
            MongoDBContext mongoDB = new MongoDBContext();
            var eventHubReceiver = eventHubClient.GetDefaultConsumerGroup().CreateReceiver(partition, DateTime.UtcNow);
            while (true)
            {
                if (ct.IsCancellationRequested) break;
                EventData eventData = await eventHubReceiver.ReceiveAsync();
                if (eventData == null) continue;

                string data = Encoding.UTF8.GetString(eventData.GetBytes());
                Console.WriteLine("Message received. Partition: {0} Data: '{1}'", partition, data);

                // Save in MongoDB
                //mongoDB.SaveDocument(data);
                new Task(() => { mongoDB.SaveDocument(data, "DeviceObservations_SAREF"); }).Start();

                // Verify message: add INTER-IoT graphs (to be used by Sit.Identifier)
                //VerifyMessageTypeAndTakeAction(data, mongoDB);
                new Task(() => { VerifyMessageTypeAndTakeAction(data, mongoDB); }).Start();

            }
        }

        private static void VerifyMessageTypeAndTakeAction(string data, MongoDBContext mongoDB)
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
                        new Task(() => { mongoDB.SaveDocument(messageFormattedINTER_IoT_GraphSrtucture, "DeviceObservations_INTERIoT_input"); }).Start();

                        // Send to Situation Identifier Manager REST
                        //SendFormattedDataToSituationIdentifier(messageFormattedINTER_IoT_GraphSrtucture);
                        new Task(() => { PreProcessFormattedData(messageFormattedINTER_IoT_GraphSrtucture); }).Start();


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
            { }
            return null;
        }

        private static void SendFormattedDataToSituationIdentifier(JObject messageFormattedINTER_IoT_GraphSrtucture)
        {
            try
            {
                string url = "http://localhost:53269/api/deviceobservations/123";
                //string url = "http://inter-iot-ews-situationidentificationmanagerrest-v0.azurewebsites.net/api/deviceobservations/123";
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
            }
            catch (Exception ex)
            { }
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

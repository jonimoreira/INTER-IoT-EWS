using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.ServiceBus.Messaging;
using System.Threading;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Net;
using INTERIoTEWS.ContextManager.ContextManagerREST.Util;

namespace ReadDeviceToCloudMessages
{
    class Program
    {
        static string connectionString = "XXXXXXX";
        
        static string iotHubD2cEndpoint = "messages/events";
        static EventHubClient eventHubClient;

        static MongoDBContext mongoDB = new MongoDBContext();

        static void Main(string[] args)
        {
            /*
            JsonLDLoader jsonldLoader = new ReadDeviceToCloudMessages.JsonLDLoader();
            jsonldLoader.LoadSAREF4healthFromFiles();
            return;
            */
            //ConvertTimestampXSDdateTime(1528212347205.0);

            deviceClient = Microsoft.Azure.Devices.Client.DeviceClient.Create(iotHubUri, new Microsoft.Azure.Devices.Client.DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey), Microsoft.Azure.Devices.Client.TransportType.Http1);

            mongoDB = new MongoDBContext();

            Console.WriteLine("Receive messages. Ctrl-C to exit.\n");
            eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, iotHubD2cEndpoint);

            var d2cPartitions = eventHubClient.GetRuntimeInformation().PartitionIds;

            CancellationTokenSource cts = new CancellationTokenSource();

            System.Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Exiting...");
            };

            var tasks = new List<Task>();
            foreach (string partition in d2cPartitions)
            {
                tasks.Add(ReceiveMessagesFromDeviceAsync(partition, cts.Token));
            }
            Task.WaitAll(tasks.ToArray());

        }

        private static async Task ReceiveMessagesFromDeviceAsync(string partition, CancellationToken ct)
        {
            var eventHubReceiver = eventHubClient.GetDefaultConsumerGroup().CreateReceiver(partition, DateTime.UtcNow);
            while (true)
            {
                if (ct.IsCancellationRequested) break;
                EventData eventData = await eventHubReceiver.ReceiveAsync();
                if (eventData == null) continue;

                string data = Encoding.UTF8.GetString(eventData.GetBytes());
                Console.WriteLine("Message received. Partition: {0} Data: '{1}'", partition, data);

                SaveFile(data);

                mongoDB.SaveDocument(data, "DeviceObservations_EDXL");

                //VerifyMessageTypeAndTakeAction(data);

                //JsonLDLoader jsonLDLoader = new JsonLDLoader();
                //jsonLDLoader.LoadSAREF4health(data);

            }
        }

        private static string ConvertTimestampXSDdateTime(double timestamp)
        {
            string result = string.Empty;

            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dateTime = dateTime.AddMilliseconds(timestamp).ToLocalTime();
            result = System.Runtime.Remoting.Metadata.W3cXsd2001.SoapDateTime.ToString(dateTime);

            return result;
        }

        private static void VerifyMessageTypeAndTakeAction(string data)
        {
            JObject messageJson = JObject.Parse(data);

            if (messageJson["@type"] != null)
            {
                switch (messageJson["@type"].ToString())
                {
                    case "saref:Device":
                        // Received from Shimmer3-LD app
                        JObject messageFormattedINTER_IoT_GraphSrtucture = AddINTER_IoT_GraphSrtucture(messageJson);

                        // Send to Situation Identifier Manager REST
                        //SendFormattedDataToSituationIdentifier(messageFormattedINTER_IoT_GraphSrtucture);

                        SaveFile(messageFormattedINTER_IoT_GraphSrtucture.ToString());

                        //SendToAzureIoTHub(messageFormattedINTER_IoT_GraphSrtucture);
                        break;
                    default:
                        break;
                }

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
                " + messageJson.ToString(Newtonsoft.Json.Formatting.None) + @"
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



        private static void SendFormattedDataToSituationIdentifier(JObject messageFormattedINTER_IoT_GraphSrtucture)
        {
            string url = "http://localhost:53268/api/deviceobservations/123";
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



        static Microsoft.Azure.Devices.Client.DeviceClient deviceClient;
        static string iotHubUri = "XXXXX";
        static string deviceKey = "XXXXXXX"
        static string deviceId = "XXXXXXX";

        private static async void SendToAzureIoTHub(JObject eCGDeviceJSON)
        {
            var messageString = eCGDeviceJSON.ToString(Newtonsoft.Json.Formatting.None);//JsonConvert.SerializeObject(eCGDeviceJSON);
            var message = new Microsoft.Azure.Devices.Client.Message(Encoding.ASCII.GetBytes(messageString));
            //message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");

            await deviceClient.SendEventAsync(message);
        }
        

        private static void SaveFile(string data)
        {
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            string filePath = @"D:\Projects\InterIOT\Workplan\WP2-INTER-IoT-EWS\data\EDXL\" + unixTimestamp.ToString() + "_" + Guid.NewGuid() + ".json";
            using (StreamWriter outputFile = new StreamWriter(filePath))
            {
                outputFile.Write(data);
            }
        }
        
        
    }
}

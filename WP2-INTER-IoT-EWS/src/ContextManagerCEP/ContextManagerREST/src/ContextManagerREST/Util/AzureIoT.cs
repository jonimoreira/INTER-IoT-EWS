using Microsoft.Azure.Devices.Client;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ContextManagerREST.Util
{
    public class AzureIoT
    {

        private string iotHubUri = "MyDrivingIoTHubEWS.azure-devices.net";
        private string deviceKey = "hI336rVjlnimW9VdaHYOSqfWq83VAf+Tkdc6VJKrhUA=";
        private string deviceId = "ZY224DC54P";


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
        static string connectionString = "HostName=MyDrivingIoTHubEWS.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=tCI9KK86Dpkd2c22WbsRFzTQX0uMjrxnKzu1bpsM1ZI=";
        static string iotHubD2cEndpoint = "messages/events";
        static EventHubClient eventHubClient;


        private static async Task ReceiveMessagesFromDeviceAsync(string partition, CancellationToken ct)
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

                mongoDB.SaveDocument(data);
            }
        }

        public static void ReceiveMessagesFromIoTHub()
        {
            eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, iotHubD2cEndpoint);

            var d2cPartitions = eventHubClient.GetRuntimeInformation().PartitionIds;

            CancellationTokenSource cts = new CancellationTokenSource();
            
            var tasks = new List<Task>();
            foreach (string partition in d2cPartitions)
            {
                tasks.Add(ReceiveMessagesFromDeviceAsync(partition, cts.Token));
            }
            Task.WaitAll(tasks.ToArray());
        }

    }
}

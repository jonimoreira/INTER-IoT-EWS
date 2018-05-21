using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.ServiceBus.Messaging;
using System.Threading;
using System.IO;

namespace ReadDeviceToCloudMessages
{
    class Program
    {
        static string connectionString = "HostName=MyDrivingIoTHubEWS.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=tCI9KK86Dpkd2c22WbsRFzTQX0uMjrxnKzu1bpsM1ZI=";
        static string iotHubD2cEndpoint = "messages/events";
        static EventHubClient eventHubClient;

        static void Main(string[] args)
        {
            /*
            JsonLDLoader jsonldLoader = new ReadDeviceToCloudMessages.JsonLDLoader();
            jsonldLoader.LoadSAREF4healthFromFiles();
            return;
            */
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
                JsonLDLoader jsonLDLoader = new JsonLDLoader();
                jsonLDLoader.LoadSAREF4health(data);

            }
        }

        private static void SaveFile(string data)
        {
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            string filePath = @"D:\Projects\InterIOT\Workplan\WP2-INTER-IoT-EWS\data\" + unixTimestamp.ToString() + "_" + Guid.NewGuid() + ".json";
            using (StreamWriter outputFile = new StreamWriter(filePath))
            {
                outputFile.Write(data);
            }
        }
        
        
    }
}

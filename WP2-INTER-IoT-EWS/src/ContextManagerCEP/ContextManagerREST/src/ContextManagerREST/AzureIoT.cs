using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContextManagerREST
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

    }
}

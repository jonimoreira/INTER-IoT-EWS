using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace ReadDeviceToCloudMessages
{
    public class JsonLDLoader
    {
        public JsonLDLoader()
        {

        }

        public async void LoadSAREF4health(string message)
        {
            Console.WriteLine(message);

            JObject sarefMakesMeasurementItemJSON = JObject.Parse(message);
            //sarefMakesMeasurementItemJSON.TryGetValue("")

            ECGDevice eCGdevice = new ECGDevice();
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(message));
            DataContractJsonSerializer ser = new DataContractJsonSerializer(eCGdevice.GetType());
            eCGdevice = ser.ReadObject(ms) as ECGDevice;
            ms.Close();



        }

        public async void LoadSAREF4healthFromFiles()
        {
            string filePath = @"D:\Projects\InterIOT\Workplan\WP2-INTER-IoT-EWS\data\tests\1525876445_1fd06e81-2684-44a8-a49a-3d89e7ab792f.json";

            using (StreamReader r = new StreamReader(filePath))
            {
                string json = r.ReadToEnd();
                ECGDevice eCGdevice = JsonConvert.DeserializeObject<ECGDevice>(json);
                Console.WriteLine(eCGdevice.label);

            }
        }
    }
}

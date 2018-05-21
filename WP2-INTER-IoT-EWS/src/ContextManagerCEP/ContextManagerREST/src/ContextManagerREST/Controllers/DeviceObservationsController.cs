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
using ContextManagerREST.Domain;

namespace ContextManagerREST.Controllers
{
    [Route("api/[controller]")]
    public class DeviceObservationsController : Controller
    {
        // CEP server
        EPServiceProvider epService = EPServiceProviderManager.GetDefaultProvider();

        // GET api/deviceobservations
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "Test1", "Test2" };
        }

        // GET api/deviceobservations/5
        [HttpGet("{deviceId}")]
        public string Get(string deviceId)
        {
            return "Device: " + deviceId;
        }

        // POST api/deviceobservations
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/deviceobservations/5
        [HttpPut("{deviceId}")]
        public HttpResponseMessage Put(string deviceId, [FromBody]JToken value)
        {
            //SaveFile(deviceId, value);
            //SendToAzureIoTHub(value);

            // Send data to CEP
            //AccelerometerSensor accelSensor = new AccelerometerSensor(deviceId);
            //accelSensor.Acceleration = 25;
            //SendEventToCEP(accelSensor);

            MapperJSONLDtoDomain mapper = new MapperJSONLDtoDomain(value);
            List<Sensor> sensors = mapper.ExecuteMappings();
            foreach (Sensor sensor in sensors)
            {
                SendEventToCEP(sensor);
            }

            return new HttpResponseMessage(HttpStatusCode.Created);
        }


        /// <summary>
        /// Sends an event to the CEP Server
        /// </summary>
        /// <param name="obj">the event</param>
        public void SendEventToCEP(Sensor obj)
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

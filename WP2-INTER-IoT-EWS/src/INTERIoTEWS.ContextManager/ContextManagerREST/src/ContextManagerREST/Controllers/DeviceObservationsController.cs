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
using INTERIoTEWS.ContextManager.ContextManagerREST.Util;

namespace INTERIoTEWS.ContextManager.ContextManagerREST.Controllers
{
    [Route("api/[controller]")]
    public class DeviceObservationsController : Controller
    {
        // Context Database
        private MongoDBContext mongoDB = new MongoDBContext();
        
        // GET api/deviceobservations
        [HttpGet]
        public JsonResult Get()
        {
            List<JObject> result = mongoDB.GetAllDocuments();

            return Json(result);//, JsonRequestBehavior.AllowGet);
        }

        // GET api/deviceobservations/5
        [HttpGet("{deviceId}")]
        public string Get(string deviceId)
        {
            new Task(() => { AzureIoT.SimulateINTERIoT_MW(); }).Start();
            
            return "ReceiveMessagesFromIoTHub started! DeviceId: " + deviceId;
        }
        
        // PUT api/deviceobservations/5
        [HttpPut("{deviceId}")]
        public HttpResponseMessage Put(string deviceId, [FromBody]JToken value)
        {
            new Task(() => { ExecuteContextManager(value); }).Start();

            return new HttpResponseMessage(HttpStatusCode.Created);
        }
                
        private void ExecuteContextManager(JToken value)
        {
            mongoDB.SaveDocument(value);
            mongoDB.TestQueries();            
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

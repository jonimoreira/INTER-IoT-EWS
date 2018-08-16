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
            /*
            mongoDB.Collection = "DeviceObservations_SAREF";
            //List<JObject> result = mongoDB.GetAllDocuments();
            long num = mongoDB.GetCountDocuments();
            //return Json(result);//, JsonRequestBehavior.AllowGet);
            */
            /*
            long num = 51;
            
            return Json(num);
            */

            string baseUrl = HttpContext.Request.Host.Value;

            return Json(baseUrl);

        }
        
        // GET api/deviceobservations/5
        [HttpGet("{deviceId}")]
        public string Get(string deviceId)
        {
            string result = "start";
            if (AzureIoT.eventHubClient == null || AzureIoT.eventHubClient.IsClosed)
            {
                new Task(() => { AzureIoT.SimulateINTERIoT_MW(HttpContext.Request.Host.Value); }).Start();
                result = "called SimulateINTERIoT_MW";
            }

            if (deviceId == "savelocal")
            {
                mongoDB.Collection = "DeviceObservations_SAREF";
                List<JObject> allDocs = mongoDB.GetAllDocuments();
                JObject jo = SaveJsonFilesFromMongoDb(allDocs);
                result = deviceId;
            }
            
            return "Result: " + result;
        }
        
        // PUT api/deviceobservations/5
        [HttpPut("{deviceId}")]
        public HttpResponseMessage Put(string deviceId, [FromBody]JToken value)
        {
            new Task(() => { ExecuteContextManager(value); }).Start();

            return new HttpResponseMessage(HttpStatusCode.Created);
        }
        
        private JObject SaveJsonFilesFromMongoDb(List<JObject> result)
        {
            JObject resultJo = new JObject();
            string basePath = @"D:\Projects\InterIOT\Workplan\WP2-INTER-IoT-EWS\data\MongoDB";

            foreach (JObject jo in result)
            {
                Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                string filePath = basePath + @"\SAREF4health_" + unixTimestamp.ToString() + "_" + Guid.NewGuid() + ".json";

                jo.Remove("_id");

                using (StreamWriter outputFile = new StreamWriter(filePath))
                {
                    outputFile.Write(jo.ToString(Formatting.Indented));
                }
                resultJo = jo;
            }

            return resultJo;
        }

        private void ExecuteContextManager(JToken value)
        {
            mongoDB.SaveDocument(value);
            mongoDB.TestQueries();            
        }
        
    }
}

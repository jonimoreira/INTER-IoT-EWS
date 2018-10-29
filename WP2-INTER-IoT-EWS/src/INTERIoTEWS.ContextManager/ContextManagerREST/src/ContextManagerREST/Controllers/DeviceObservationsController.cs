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
            System.Diagnostics.Trace.TraceInformation("[ContextManager] api/deviceobservations/" + ((deviceId == null) ? "deviceId is NULL" : deviceId));

            string result = "start";
            if (AzureIoT.eventHubClient == null || AzureIoT.eventHubClient.IsClosed)
            {
                new Task(() => { AzureIoT.SimulateINTERIoT_MW(HttpContext.Request.Host.Value); }).Start();
                result = "called SimulateINTERIoT_MW";
            }

            if (deviceId.StartsWith("DisableIoTHubSubscription"))
            {
                if (AzureIoT.eventHubClient != null)
                {
                    lock (AzureIoT.eventHubClient)
                    {
                        if (!AzureIoT.eventHubClient.IsClosed)
                            AzureIoT.eventHubClient.Close();
                        try
                        {
                            AzureIoT.eventHubClient = null;
                            result += "Success: DisableIoTHubSubscription";
                        }
                        catch (Exception ex) { result += "Exception on DisableIoTHubSubscription: " + ex.Message; }
                    }
                }
                else
                    result += "AzureIoT.eventHubClient == null";
            }
            
            if (deviceId.StartsWith("savelocal"))
            {

                string[] collectionNameArr = deviceId.Split(',').ToArray();
                string collectionName = (collectionNameArr.Length > 1) ? collectionNameArr[1].Trim() : string.Empty;

                mongoDB.Collection = collectionName;
                List<JObject> allDocs = mongoDB.GetAllDocuments();
                JObject jo = SaveJsonFilesFromMongoDb(allDocs, collectionName);
                result = deviceId;
            }
            else if (deviceId.StartsWith("testmongodb"))
            {
                JObject test = new JObject();
                test.Add("@id", "testId");
                test.Add("label", "hello world!");
                try
                {
                    mongoDB.SaveDocument(test, "TestCollName_" + deviceId);
                    result = "saved in MongoDB!";
                }
                catch (Exception ex)
                {
                    result = ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine + ((ex.InnerException != null) ? "InnerException.Message=" + ex.InnerException.Message : string.Empty);
                    System.Diagnostics.Trace.TraceError("[ContextManager] Saving in MongoDB:" + result);
                }
            }
            else if (deviceId.StartsWith("translation"))
            {
                string translationMechanism = deviceId.Split('-').ToArray()[1].Trim();

                switch (translationMechanism)
                {
                    case "1":
                        // default behaviour with semantic translations executed at the Situation Manager with SPARQL queries
                        break;
                    case "2":
                        // call IPSM for semantic translations
                        break;
                    default:
                        break;
                }
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
        
        private JObject SaveJsonFilesFromMongoDb(List<JObject> result, string collectionName)
        {
            JObject resultJo = new JObject();
            string basePath = @"D:\Projects\InterIOT\Workplan\WP2-INTER-IoT-EWS\data\MongoDB\" + collectionName;
            System.IO.Directory.CreateDirectory(basePath);

            foreach (JObject jo in result)
            {
                Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                string filePath = basePath + @"\" + unixTimestamp.ToString() + "_" + Guid.NewGuid() + ".json";

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

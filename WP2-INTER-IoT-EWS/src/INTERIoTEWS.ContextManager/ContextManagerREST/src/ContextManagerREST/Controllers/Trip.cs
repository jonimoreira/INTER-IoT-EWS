using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using INTERIoTEWS.ContextManager.ContextManagerREST.Util;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net;

namespace INTERIoTEWS.ContextManager.ContextManagerREST.Controllers
{
    [Route("api/[controller]")]
    public class Trip : Controller
    {
        // Context Database
        private MongoDBContext mongoDB = new MongoDBContext();

        // GET: /<controller>/
        public IActionResult Index()
        {
            return View();
        }

        // GET api/trip/5
        [HttpGet("{tripId}")]
        public JsonResult Get(string tripId)
        {
            System.Diagnostics.Trace.TraceInformation("[ContextManager] api/trip/" + ((tripId == null) ? "deviceId is NULL" : tripId));

            string colName = "tripId_" + tripId + "_health_DeviceObservations_SAREF4health";
            mongoDB.Collection = colName;
            List<JObject> result = mongoDB.GetAllDocuments();

            return Json(result);
        }

        // POST api/trip
        [HttpPost()]
        public HttpResponseMessage Post([FromBody]JToken value)
        {
            mongoDB.Collection = "Test_SERVER_PUSH_fromINTERMW";

            JObject test = new JObject();
            test.Add("@id", "testId");
            test.Add("label", "hello world: " + DateTime.UtcNow.ToString("o"));

            mongoDB.SaveDocument(test);

            return new HttpResponseMessage(HttpStatusCode.Created);
        }

        private void ExecuteContextManager(string tripId, JToken value)
        {
            mongoDB.SaveDocument(value);
            mongoDB.TestQueries();
        }

    }
}

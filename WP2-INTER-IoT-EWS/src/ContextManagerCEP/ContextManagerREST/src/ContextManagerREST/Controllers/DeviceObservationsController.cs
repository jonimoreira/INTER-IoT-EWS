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
using ContextManagerREST.Models;
using ContextManagerREST.Util;

namespace ContextManagerREST.Controllers
{
    [Route("api/[controller]")]
    public class DeviceObservationsController : Controller
    {
        // Context Database
        private EWSContext db = new EWSContext(); // SQL Server (relational): changed for MongoDB
        private MongoDBContext mongoDB = new MongoDBContext();
        
        // CEP server
        private EPServiceProvider epService = EPServiceProviderManager.GetDefaultProvider();

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
            new Task(() => { AzureIoT.ReceiveMessagesFromIoTHub(); }).Start();
            
            return "ReceiveMessagesFromIoTHub started! DeviceId: " + deviceId;
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
            new Task(() => { ExecuteContextManager(value); }).Start();

            //new Task(() => { ExecuteSituationIdentificationManager(value); }).Start();

            return new HttpResponseMessage(HttpStatusCode.Created);
        }

        
        private void ExecuteContextManager(JToken value)
        {
            mongoDB.SaveDocument(value);
            mongoDB.TestQueries();

            /*
            ContextManager.DataObjects.SAREF.DeviceObservation mobileDevice = new ContextManager.DataObjects.SAREF.DeviceObservation();

            // Get mobile device information
            mobileDevice.DeviceIdURI = value["@id"].ToString();

            db.DeviceObservations.Add(mobileDevice);
            db.SaveChanges();
            

            MemoryStream ms = new MemoryStream();
            using (Newtonsoft.Json.Bson.BsonWriter writer = new Newtonsoft.Json.Bson.BsonWriter(ms))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(writer, value);                
            }
            

            var docBson = BsonDocument.Create(ms);

            
            var collection = database.GetCollection<BsonDocument>("bar");

            /*
            var document = new BsonDocument
{
    { "name", "MongoDB" },
    { "type", "Database" },
    { "count", 1 },
    { "info", new BsonDocument
        {
            { "x", 203 },
            { "y", 102 }
        }}
};

            collection.InsertOne(document);
            // await collection.InsertOneAsync(document);
            */

        }
        


        private void ExecuteSituationIdentificationManager(JToken value)
        {
            MapperJSONLDtoDomain mapper = new MapperJSONLDtoDomain(value);
            List<Sensor> sensors = mapper.ExecuteMappings();
            foreach (Sensor sensor in sensors)
            {
                SendEventToCEP(sensor);
            }
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

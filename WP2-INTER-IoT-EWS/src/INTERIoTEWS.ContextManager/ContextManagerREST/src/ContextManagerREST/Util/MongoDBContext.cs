using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace INTERIoTEWS.ContextManager.ContextManagerREST.Util
{
    public class MongoDBContext
    {
        private MongoClient client;
        private IMongoDatabase database;
        //private const string mongoDbServerConnstr = "mongodb://localhost:27017";
        //private const string mongoDbServerConnstr = "mongodb://18.184.254.139:27017";
        private const string mongoDbServerConnstr = "mongodb://admin:1nter1otews@40.85.72.192:27017"; // Azure
        private const string mongoDbDatabase = "INTER_IoT_EWS_v1";
        private string collection = "DeviceObservations_";

        public string Collection
        {
            get
            {
                return collection;
            }

            set
            {
                collection = value;
            }
        }

        public MongoDBContext()
        {
            client = new MongoClient(mongoDbServerConnstr);
            database = client.GetDatabase(mongoDbDatabase);
            Collection += "EDXL";
        }

        public void SaveDocument(JToken jsonDoc)
        {
            try
            {
                var collDeviceObs = database.GetCollection<BsonDocument>(Collection);
                var docInput = BsonDocument.Parse(jsonDoc.ToString());
                //collDeviceObs.InsertOne(docInput);
                collDeviceObs.InsertOneAsync(docInput); // async insert
            }
            catch (Exception ex)
            {
                string result = ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine + ((ex.InnerException != null) ? "InnerException.Message=" + ex.InnerException.Message : string.Empty);
                JObject test = new JObject();
                test.Add("errorMsg", result);
                var collDeviceObs = database.GetCollection<BsonDocument>("ErrorContextManagerInsert");
                var docInput = BsonDocument.Parse(test.ToString());
                //collDeviceObs.InsertOne(docInput);
                collDeviceObs.InsertOneAsync(docInput);
                System.Diagnostics.Trace.TraceError("[ContextManager] Error on[SaveDocument]:" + ex.Message + Environment.NewLine + "InnerException: " + ((ex.InnerException != null) ? ex.InnerException.Message : "NULL"));

            }
        }

        public void SaveDocument(JToken jsonDoc, string collectionName)
        {
            try
            {
                var collDeviceObs = database.GetCollection<BsonDocument>(collectionName);
                var docInput = BsonDocument.Parse(jsonDoc.ToString());
                //collDeviceObs.InsertOne(docInput);
                collDeviceObs.InsertOneAsync(docInput); // async insert
            }
            catch (Exception ex)
            {
                string result = ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine + ((ex.InnerException != null) ? "InnerException.Message=" + ex.InnerException.Message : string.Empty);
                JObject test = new JObject();
                test.Add("errorMsg", result);
                var collDeviceObs = database.GetCollection<BsonDocument>("ErrorContextManagerInsert");
                var docInput = BsonDocument.Parse(test.ToString());
                //collDeviceObs.InsertOne(docInput);
                collDeviceObs.InsertOneAsync(docInput);
                System.Diagnostics.Trace.TraceError("[ContextManager] Error on[SaveDocument2]:" + ex.Message + Environment.NewLine + "InnerException: " + ((ex.InnerException != null) ? ex.InnerException.Message : "NULL"));

            }

        }

        public void SaveDocument(string jsonDoc)
        {
            var collDeviceObs = database.GetCollection<BsonDocument>(Collection);
            var docInput = BsonDocument.Parse(jsonDoc);
            collDeviceObs.InsertOne(docInput);
        }

        public void TestQueries()
        {
            var collDeviceObs = database.GetCollection<BsonDocument>(Collection);
            
            var document = collDeviceObs.Find(new BsonDocument()).FirstOrDefault();
            
            string field = "label";
            string fieldValue = "Smartphone Motorola G5 Plus used in INTER-IoT-EWS project";
            FindQuery(field, fieldValue);

            field = "@id";
            fieldValue = "sarefInst:MobileDeviceAsSemanticFieldGateway_MotoG5Plus_ZY224DC54P";
            FindQuery(field, fieldValue);

            field = "@id";
            fieldValue = "sarefInst:Shimmer3ECG_unit_T9JRN42_DeviceId";
            FindQuery(field, fieldValue);
            
            field = "saref:consistsOf.comment";
            fieldValue = "Shimmer3 ECG unit (T9J-RN42): INTER-IoT-EWS project";
            FindQuery(field, fieldValue);

            field = "saref:makesMeasurement.relatesToProperty";
            fieldValue = "sarefInst:VehicleCollisionDetectedFromMobileDevice";
            FindQuery(field, fieldValue);

            fieldValue = "{'saref:makesMeasurement': {'relatesToProperty' : 'sarefInst:VehicleCollisionDetectedFromMobileDevice'} }";
            FindQueryWithJson(fieldValue);

            fieldValue = "{'saref:makesMeasurement': {'relatesToProperty' : 'sarefInst:VehicleCollisionDetectedFromMobileDevice', 'hasValue' : '1'} }";
            FindQueryWithJson(fieldValue);

            FindQueryWithBuilder();

            /*
            var filterBuilder = Builders<BsonDocument>.Filter;
            var filter2 = filterBuilder.Gt("x", 50) & filterBuilder.Lte("x", 300);
            var cursor2 = collection.Find(filter).ToCursor();
            foreach (var doc in cursor2.ToEnumerable())
            {
                Console.WriteLine(doc);
            } */
        }

        public void FindQuery(string field, string value)
        {
            var collDeviceObs = database.GetCollection<BsonDocument>(Collection);

            var filter = new BsonDocument(field, value);
            //var filter = "{ " + field + ": '" + value + "'}";
            // var filter = Builders<BsonDocument>.Filter.Gt(field, value);

            var cursor = collDeviceObs.Find(filter).ToCursor();
            foreach (var doc in cursor.ToEnumerable())
            {
                Console.WriteLine(doc);
            }
            
        }

        public void FindQueryWithJson(string jsonDoc)
        {
            var collDeviceObs = database.GetCollection<BsonDocument>(Collection);

            var filter = jsonDoc;
            
            var cursor = collDeviceObs.Find(filter).ToCursor();
            foreach (var doc in cursor.ToEnumerable())
            {
                Console.WriteLine(doc);
            }

        }

        public void FindQueryWithBuilder()
        {
            var collDeviceObs = database.GetCollection<BsonDocument>(Collection);

            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Lt("geo:location.geo:lat", 50) & builder.Eq("saref:makesMeasurement.relatesToProperty", "sarefInst:VehicleCollisionDetectedFromMobileDevice");

            var cursor = collDeviceObs.Find(filter).ToCursor();
            foreach (var doc in cursor.ToEnumerable())
            {
                Console.WriteLine(doc);
            }

        }

        public long GetCountDocuments()
        {
            var collDeviceObs = database.GetCollection<BsonDocument>(Collection);
            long countDocs = collDeviceObs.Find(new BsonDocument()).Count();
            return countDocs;
        }

        public List<JObject> GetAllDocuments()
        {
            var collDeviceObs = database.GetCollection<BsonDocument>(Collection);
            var cursor = collDeviceObs.Find(new BsonDocument()).ToCursor();
            List<JObject> result = new List<JObject>();
            foreach (var doc in cursor.ToEnumerable())
            {
                // Remove "_id" that is added by MongoDB (BSON)
                JObject json = JObject.Parse(doc.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.Strict }));
                json.Remove("_id");
                result.Add(json);
            }
            return result;
        }
    }
}

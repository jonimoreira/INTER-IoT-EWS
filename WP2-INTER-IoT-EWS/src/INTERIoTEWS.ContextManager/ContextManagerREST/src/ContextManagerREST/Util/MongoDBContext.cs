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
        private const string mongoDbServerConnstr = "XXXXXXXXXXXXXXXXXXXXXXXX";
        private const string mongoDbDatabase = "INTER_IoT_EWS_v0";

        public MongoDBContext()
        {
            client = new MongoClient(mongoDbServerConnstr);
            database = client.GetDatabase(mongoDbDatabase);
        }

        public void SaveDocument(JToken jsonDoc)
        {
            var collDeviceObs = database.GetCollection<BsonDocument>("DeviceObservations");
            var docInput = BsonDocument.Parse(jsonDoc.ToString());
            collDeviceObs.InsertOne(docInput);
            // collDeviceObs.InsertOneAsync(docInput); // async insert
        }

        public void SaveDocument(JToken jsonDoc, string collectionName)
        {
            var collDeviceObs = database.GetCollection<BsonDocument>(collectionName);
            var docInput = BsonDocument.Parse(jsonDoc.ToString());
            collDeviceObs.InsertOne(docInput);
            // collDeviceObs.InsertOneAsync(docInput); // async insert
        }

        public void SaveDocument(string jsonDoc)
        {
            var collDeviceObs = database.GetCollection<BsonDocument>("DeviceObservations");
            var docInput = BsonDocument.Parse(jsonDoc);
            collDeviceObs.InsertOne(docInput);
        }

        public void TestQueries()
        {
            var collDeviceObs = database.GetCollection<BsonDocument>("DeviceObservations");
            
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
            var collDeviceObs = database.GetCollection<BsonDocument>("DeviceObservations");

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
            var collDeviceObs = database.GetCollection<BsonDocument>("DeviceObservations");

            var filter = jsonDoc;
            
            var cursor = collDeviceObs.Find(filter).ToCursor();
            foreach (var doc in cursor.ToEnumerable())
            {
                Console.WriteLine(doc);
            }

        }

        public void FindQueryWithBuilder()
        {
            var collDeviceObs = database.GetCollection<BsonDocument>("DeviceObservations");

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
            var collDeviceObs = database.GetCollection<BsonDocument>("DeviceObservations");
            long countDocs = collDeviceObs.Find(new BsonDocument()).Count();
            return countDocs;
        }

        public List<JObject> GetAllDocuments()
        {
            var collDeviceObs = database.GetCollection<BsonDocument>("DeviceObservations");
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

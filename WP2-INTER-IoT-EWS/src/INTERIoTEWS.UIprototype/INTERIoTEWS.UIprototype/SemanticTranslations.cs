using INTERIoTEWS.Context.DataObjects.SEMIoTICS;
using INTERIoTEWS.Context.DataObjects.SOSA;
using INTERIoTEWS.Context.DataObjects.SOSA.GEO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;

namespace INTERIoTEWS.SituationIdentificationManager.SituationIdentificationREST.Util
{
    enum SemanticTranslationsMechanism
    {
        RawSPARQL = 1,
        IPSM = 2
    }

    public class SemanticTranslations
    {
        
        private JToken inputData;
        private JToken inputDataWithINTERIoTGraphsStructure;
        private const string vehicleCollisionProperty = "https://w3id.org/saref/instances#VehicleCollisionDetectedFromECGDeviceAccelerometerComputedByMobile";

        public SemanticTranslations(JToken input)
        {
            inputDataWithINTERIoTGraphsStructure = input;
            inputData = RemoveINTERIoTGraphsStructure(input);
        }

        private JToken RemoveINTERIoTGraphsStructure(JToken input)
        {
            JToken result = input;

            JToken mainGraph = input["@graph"];
            if (mainGraph != null )
            {
                int count = 0;
                foreach (JToken graph in mainGraph.Children())
                {
                    count++;
                    if (count == 2)
                    {
                        foreach (JToken token in graph.Children())
                        {
                            result = token;
                            foreach (JToken token2 in token.Children())
                            {
                                result = token2;
                                foreach (JToken token3 in token2.Children())
                                {
                                    result = token3;
                                }
                            }
                            break;
                        }
                    }                    
                }                
            }

            return result;
        }

        public List<Observation> ExecuteMappings()
        {
            // Check if the message is related to e-Health (from Shimmer3 app: @context contains SAREF4health (FHIR? UFO ECG?)) or logistics (from MyDriving app: @context contains LogiCO)
            JToken context = inputData["@context"];
            if (context == null)
            {
                JToken contextINTERIoTGraphsStructure = inputDataWithINTERIoTGraphsStructure["@context"];

                if (contextINTERIoTGraphsStructure == null)
                    throw new Exception("@context not provided");
                context = contextINTERIoTGraphsStructure;
            }                

            bool isEHealthMessage = false;
            bool isLogisticsMessage = false;
            bool isINTERIoTMessage = false;

            foreach (JToken token in context.Children().Values())
            {
                if (token.ToString() == "https://w3id.org/def/saref4health#")
                    isEHealthMessage = true;
                if (token.ToString() == "http://ontology.tno.nl/logico#")
                    isLogisticsMessage = true;
                if (token.ToString() == "http://inter-iot.eu/")
                    isINTERIoTMessage = true;
            }

            List<Observation> result = new List<Observation>();

            if (isEHealthMessage)
                result.AddRange(ExecuteMappingsFromSAREF());

            if (isLogisticsMessage)
                result.AddRange(ExecuteMappingsLogistics());

            if (isINTERIoTMessage)
                result.AddRange(ExecuteMappingsINTERIoT());

            return result;
        }

        Formatting formattingForJSON = Formatting.Indented;

        private List<Observation> ExecuteMappingsINTERIoT()
        {
            List<Observation> result = new List<Observation>();

            var jsonLdParser = new JsonLdParser();
            TripleStore tStore = new TripleStore();
            using (var reader = new System.IO.StringReader(inputDataWithINTERIoTGraphsStructure.ToString(formattingForJSON)))
            {
                jsonLdParser.Load(tStore, reader);
            }

            Platform platform = null;
            Point geoPoint = null;

            // Query for all geo:location (Device or Sensor) and map to hasLocation (Platform or Sensor)
            string sparqlQuery = @"
                
                PREFIX InterIoTMsg: <http://inter-iot.eu/message/>
                PREFIX InterIoT: <http://inter-iot.eu/>
                PREFIX sosa: <http://www.w3.org/ns/sosa/>

                PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                
                SELECT ?platform ?label ?location ?point
                FROM InterIoTMsg:payload
                WHERE  
	            {
	                ?platform a sosa:Platform.
	                ?platform <http://inter-iot.eu/GOIoTP#hasLocation> ?location.
                    ?platform rdfs:label ?label.
                    ?location <http://www.opengis.net/ont/geosparql#asWKT> ?point.
	            }   


            ";
            Object results = tStore.ExecuteQuery(sparqlQuery);
            
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                
                foreach (SparqlResult spqlResult in rset)
                {
                    string locationId = spqlResult["location"].ToString();

                    LiteralNode pointValueNode = (LiteralNode)spqlResult.Value("point");
                    string point = pointValueNode.AsValuedNode().AsString().Trim();
                    
                    string[] latLon = point.Substring("Point(".Length).Split(' ');
                    double lat = double.Parse(latLon[0].Trim());
                    double lon = double.Parse(latLon[1].Trim().Substring(0, latLon[1].Trim().Length - 1));

                    geoPoint = new Point(locationId, lat, lon);

                    string deviceId = spqlResult["platform"].ToString();

                    LiteralNode labelValueNode = (LiteralNode)spqlResult.Value("label");
                    string label = labelValueNode.Value;
                    platform = new Platform(deviceId, geoPoint, label);

                    break;
                }
            }

            // Gets tripId (Service from SAREF)
            sparqlQuery = @"

                PREFIX InterIoTMsg: <http://inter-iot.eu/message/>
                PREFIX saref: <https://w3id.org/saref#>

                PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
                
                SELECT ?device ?service ?tripid
                FROM InterIoTMsg:payload
                WHERE  
	            {   
				    ?device saref:offers ?service.
					?service rdfs:label ?tripid
	            }   

            ";

            results = tStore.ExecuteQuery(sparqlQuery);
            string tripId = string.Empty;

            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                foreach (SparqlResult spqlResult in rset)
                {
                    LiteralNode tripidNode = (LiteralNode)spqlResult.Value("tripid");
                    string tripid = tripidNode.Value;
                    platform.tripId = tripid;
                    break;
                }
            }

            // Query for all sensor measurements (SAREF) -> observations (SOSA)
            // Order by timestamp (SAREF) -> Observation.resultTime
            //?observation sosa:observedProperty ?measProp.

            sparqlQuery = @"
                
                PREFIX InterIoTMsg: <http://inter-iot.eu/message/>
                PREFIX InterIoT: <http://inter-iot.eu/>
                PREFIX sosa: <http://www.w3.org/ns/sosa/>

                PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                PREFIX time: <http://www.w3.org/2006/time#>

                
                SELECT ?sensor ?observation ?measValue ?measUnit ?measProp ?measTime ?sensorLabel
                FROM InterIoTMsg:payload
                WHERE  
	            {
	                ?observation a sosa:Observation.	
	                ?observation sosa:madeBySensor ?sensor.
	                ?observation sosa:hasResult ?result.
	                ?observation sosa:phenomenonTime ?timeInstant.
	                ?timeInstant time:inTimePosition ?timePosition.
	                ?timePosition time:numericPosition ?measTime.
	                ?sensor rdfs:label ?sensorLabel.
	                ?result <http://inter-iot.eu/GOIoTP#hasValue> ?measValue.
	                ?result <http://inter-iot.eu/GOIoTP#hasUnit> ?measUnit.
	                ?observation sosa:observedProperty ?measProp.
	            }   
                ORDER BY ?measTime

            ";

            results = tStore.ExecuteQuery(sparqlQuery);
            //query = sparqlparser.ParseFromString(sparqlQuery);
            //results = processor.ProcessQuery(query);

            Dictionary<string, Sensor> sensorsDic = new Dictionary<string, Sensor>();
            string messageId = string.Empty;

            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                int i = 0;

                foreach (SparqlResult spqlResult in rset)
                {
                    if (messageId == string.Empty)
                        messageId = platform.Identifier + "_" + DateTime.Now.ToUniversalTime() + "_" + Guid.NewGuid();

                    string sensorId = spqlResult["sensor"].ToString().Trim();
                    Sensor sensor;
                    if (sensorsDic.ContainsKey(sensorId))
                        sensor = sensorsDic[sensorId];
                    else
                    {
                        string sensorLabel = spqlResult["sensorLabel"].ToString().Trim();
                        sensor = new Sensor(sensorId, platform, sensorLabel);
                        sensorsDic.Add(sensorId, sensor);
                    }

                    string measurementId = spqlResult["measurement"].ToString().Trim();

                    LiteralNode measurementValueNode = (LiteralNode)spqlResult.Value("measValue");
                    double measurementValue = measurementValueNode.AsValuedNode().AsDouble();

                    LiteralNode measurementTimeNode = (LiteralNode)spqlResult.Value("measTime");
                    DateTime measDateTime = DateTime.Parse(measurementTimeNode.Value);

                    string measurementUnit = spqlResult["measUnit"].ToString().Trim();
                    string measurementProperty = spqlResult["measProp"].ToString().Trim();

                    Observation observation = new Observation(measurementId, sensor, messageId);

                    // sosa:hasResult predicate
                    observation.hasResult = new Result();
                    observation.hasResult.hasValue = measurementValue;
                    observation.hasResult.hasUnit = measurementUnit;

                    // sosa:obesrvedProperty predicate
                    string observablePropertyId = measurementProperty + "_" + Guid.NewGuid();
                    observation.observedProperty = new ObservableProperty(observablePropertyId, measurementProperty);

                    // sosa:resultTime predicate
                    observation.resultTime = measDateTime;

                    // Treat specific observations (e.g. collision detected, heart rate, etc)
                    switch (measurementProperty)
                    {
                        case vehicleCollisionProperty:
                            AddVehicleCollisionDetected(observation, result);
                            break;
                        default:
                            AddObservation(observation, result);
                            break;
                    }
                }
            }

            return result;
        }

        private List<Observation> ExecuteMappingsFromSAREF()
        {
            List<Observation> result = new List<Observation>();

            var jsonLdParser = new JsonLdParser();
            TripleStore tStore = new TripleStore();
            using (var reader = new System.IO.StringReader(inputData.ToString(formattingForJSON)))
            {
                jsonLdParser.Load(tStore, reader);
            }

            //InMemoryDataset ds = new InMemoryDataset(tStore, new Uri("http://mydefaultgraph.org"));
            //ISparqlQueryProcessor processor = new LeviathanQueryProcessor(ds);
            //Should get a Graph back from a CONSTRUCT query
            //SparqlQueryParser sparqlparser = new SparqlQueryParser();
            
            Platform platform = null;
            Point geoPoint = null;

            // Query for all geo:location (Device or Sensor) and map to hasLocation (Platform or Sensor)
            string sparqlQuery = @"
                PREFIX saref: <https://w3id.org/saref#>
                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                PREFIX geo: <http://www.w3.org/2003/01/geo/wgs84_pos#>
                PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
                
                SELECT ?devicesubclass ?device ?location ?lat ?long ?label 
                WHERE  
	                {
					?device geo:location ?location.
	                ?device rdf:type ?devicesubclass.
                    ?device rdfs:label ?label.
	                ?location geo:lat ?lat.
	                ?location geo:long ?long
	                } 

            ";
            Object results = tStore.ExecuteQuery(sparqlQuery);

            //SparqlQuery query = sparqlparser.ParseFromString(sparqlQuery);
            //Object results = processor.ProcessQuery(query);

            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;

                // (rset.Count > 1) Oportunity to check if the devices/sensors are within the same location (rset.Count > 1)

                foreach (SparqlResult spqlResult in rset)
                {
                    // By now we only get the first location and use it
                    // Get location
                    string locationId = spqlResult["location"].ToString();

                    LiteralNode latValueNode = (LiteralNode)spqlResult.Value("lat");
                    double lat = latValueNode.AsValuedNode().AsDouble();

                    LiteralNode lonValueNode = (LiteralNode)spqlResult.Value("long");
                    double lon = lonValueNode.AsValuedNode().AsDouble();

                    geoPoint = new Point(locationId, lat, lon);

                    string deviceId = spqlResult["device"].ToString();

                    LiteralNode labelValueNode = (LiteralNode)spqlResult.Value("label");
                    string label = labelValueNode.Value;
                    platform = new Platform(deviceId, geoPoint, label);

                    break;
                }
            }

            // Gets tripId (Service from SAREF)
            sparqlQuery = @"
                PREFIX saref: <https://w3id.org/saref#>
                PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
                                
                SELECT ?device ?service ?tripid
                WHERE  
	                {
				    ?device saref:offers ?service.
					?service rdfs:label ?tripid
	                } 
            ";

            results = tStore.ExecuteQuery(sparqlQuery);
            string tripId = string.Empty;

            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                foreach (SparqlResult spqlResult in rset)
                {
                    LiteralNode tripidNode = (LiteralNode)spqlResult.Value("tripid");
                    string tripid = tripidNode.Value;
                    platform.tripId = tripid;
                    break;
                }
            }

            // Query for all sensor measurements (SAREF) -> observations (SOSA)
            // Order by timestamp (SAREF) -> Observation.resultTime
            sparqlQuery = @"
                PREFIX saref: <https://w3id.org/saref#>
                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
                                
                SELECT ?sensor ?measurement ?measValue ?measUnit ?measProp ?measTime ?sensorLabel
                WHERE  
	                {
					?sensor saref:makesMeasurement ?measurement.
	                ?measurement saref:hasValue ?measValue.
	                ?measurement saref:hasTimestamp ?measTime. 
	                ?measurement saref:isMeasuredIn ?measUnit. 
	                ?measurement saref:relatesToProperty ?measProp.
                    ?sensor rdfs:label ?sensorLabel.
	                } 
                ORDER BY ?measTime

            ";

            results = tStore.ExecuteQuery(sparqlQuery);
            //query = sparqlparser.ParseFromString(sparqlQuery);
            //results = processor.ProcessQuery(query);

            Dictionary<string, Sensor> sensorsDic = new Dictionary<string, Sensor>();
            string messageId = string.Empty;

            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                int i = 0;

                foreach (SparqlResult spqlResult in rset)
                {
                    if (messageId == string.Empty)
                        messageId = platform.Identifier + "_" + DateTime.Now.ToUniversalTime() + "_" + Guid.NewGuid();

                    string sensorId = spqlResult["sensor"].ToString().Trim();
                    Sensor sensor;
                    if (sensorsDic.ContainsKey(sensorId))
                        sensor = sensorsDic[sensorId];
                    else
                    {
                        string sensorLabel = spqlResult["sensorLabel"].ToString().Trim();                        
                        sensor = new Sensor(sensorId, platform, sensorLabel);
                        sensorsDic.Add(sensorId, sensor);
                    }
                    
                    string measurementId = spqlResult["measurement"].ToString().Trim();
                    
                    LiteralNode measurementValueNode = (LiteralNode) spqlResult.Value("measValue");
                    double measurementValue = measurementValueNode.AsValuedNode().AsDouble(); 

                    LiteralNode measurementTimeNode = (LiteralNode)spqlResult.Value("measTime");
                    DateTime measDateTime = DateTime.Parse(measurementTimeNode.Value); 

                    string measurementUnit = spqlResult["measUnit"].ToString().Trim();
                    string measurementProperty = spqlResult["measProp"].ToString().Trim();

                    Observation observation = new Observation(measurementId, sensor, messageId);
                    //observation.hasSimpleResult = i++; // Test
                    
                    // sosa:hasResult predicate
                    observation.hasResult = new Result();
                    observation.hasResult.hasValue = measurementValue;
                    observation.hasResult.hasUnit = measurementUnit;
                    
                    // sosa:obesrvedProperty predicate
                    string observablePropertyId = measurementProperty + "_" + Guid.NewGuid();
                    observation.observedProperty = new ObservableProperty(observablePropertyId, measurementProperty);

                    // sosa:resultTime predicate
                    observation.resultTime = measDateTime;

                    // Treat specific observations (e.g. collision detected, heart rate, etc)
                    switch (measurementProperty)
                    {
                        case vehicleCollisionProperty:
                            AddVehicleCollisionDetected(observation, result);
                            break;
                        default:
                            AddObservation(observation, result);
                            break;
                    }                    
                    
                }
            }

            return result;

        }

        private void AddObservation(Observation observation, List<Observation> result)
        {
            result.Add(observation);
        }

        private void AddVehicleCollisionDetected(Observation observation, List<Observation> result)
        {
            VehicleCollisionDetectedObservation vehicleCollision = new VehicleCollisionDetectedObservation(observation);
            result.Add(vehicleCollision);
        }

        private List<Observation> Translate_SAREF_Sensor(string rdf_subject, TripleStore tStore)
        {
            List<Observation> result = new List<Observation>();

            // SPARQL query to get measurements

            return result;            
        }

        private bool IsRDFtype(string rdf_predicate, string rdf_object, string objectToCompare)
        {
            if (rdf_predicate != "http://www.w3.org/1999/02/22-rdf-syntax-ns#type")
                return false;

            return rdf_object == objectToCompare;
        }

        private void SetObservationsLocation(JToken geoLocation, List<Observation> observations)
        {
            foreach (Observation observation in observations)
            {
                //observation.Lat = 25;
            }
        }

        private List<Observation> TranslateSAREF4health_ECGDevice(JToken ecgDeviceJson)
        {
            List<Observation> result = new List<Observation>();
            // Load Accelerometer
            JToken deviceConsistsOf = ecgDeviceJson["saref:consistsOf"];
            if (deviceConsistsOf != null)
            {
                foreach (JToken jsonProp in deviceConsistsOf.Children())
                {
                    if (jsonProp["@type"] != null)
                    {
                        string jsonPropId = jsonProp["@id"].ToString();
                        string jsonPropType = string.Empty;
                        if (jsonProp["@type"] is JArray)
                        {
                            JArray types = (JArray)jsonProp["@type"];
                            foreach (JToken type in types)
                            {
                                if (type.ToString() != "saref:Sensor")
                                    jsonPropType = type.ToString();
                            }
                        }
                        else
                        {
                            jsonPropType = jsonProp["@type"].ToString();
                        }
                        
                        switch (jsonPropType)
                        {
                            case "saref4health:ECGLead":
                            case "saref4health:ECGLeadBipolarLimb":
                            case "saref4health:ECGLeadUnipolar":
                                result.AddRange(TranslateSAREF4health_ECGLead(jsonProp));
                                break;
                            case "saref4health:AccelerometerSensor":
                                result.AddRange(TranslateSAREF4health_AccelerometerSensor(jsonProp));
                                break;
                            case "saref:Sensor":
                                result.AddRange(TranslateSAREF_Sensor(jsonProp));
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            return result;
        }

        private List<Observation> TranslateSAREF_makesMeasurement(JToken makesMeasurement)
        {
            List<Observation> result = new List<Observation>();

            JArray makesMeasurementArr;
            if (makesMeasurement is JArray)
                makesMeasurementArr = (JArray)makesMeasurement;
            else
            {
                makesMeasurementArr = new JArray();
                makesMeasurementArr.Add(makesMeasurement);
            }

            foreach (JToken measurement in makesMeasurementArr)
            {
                if (measurement["@type"] != null)
                {
                    string jsonPropId = measurement["@id"].ToString();
                    string jsonPropType = measurement["@type"].ToString();
                    switch (jsonPropType)
                    {
                        case "saref:Measurement":
                            result.AddRange(TranslateSAREF_Measurement(measurement));
                            break;
                        default:
                            break;
                    }
                }
            }
            return result;
        }
        
        private List<Observation> TranslateSAREF_Measurement(JToken measurement)
        {
            List<Observation> result = new List<Observation>();
            /*
            string measurementId = measurement["@id"].ToString();
            string measurementType = measurement["@type"].ToString(); 
            JToken relatesToProperty = measurement["saref:relatesToProperty"];
            if (relatesToProperty != null)
            {
                string propertyId = relatesToProperty["@id"].ToString();
                string propertyType = relatesToProperty["@type"].ToString();
                switch (propertyId)
                {
                    // sarefInst:VehicleCollisionDetectedFromMobileDevice
                    case "sarefInst:VehicleCollisionDetectedFromECGDeviceAccelerometerComputedByMobile":
                        VehicleCollisionDetectedObservation observationCollision = new VehicleCollisionDetectedObservation(measurementId);
                        result.Add(observationCollision);
                        break;
                    default:
                        break;
                }
            }
            */
            return result;
        }
        
        private List<Observation> TranslateSAREF_Sensor(JToken jsonProp)
        {
            List<Observation> result = new List<Observation>();

            // saref:makesMeasurement => Observation / measurement 
            JToken makesMeasurement = jsonProp["saref:makesMeasurement"];
            if (makesMeasurement != null)
            {
                result.AddRange(TranslateSAREF_makesMeasurement(makesMeasurement));
            }

            return result;
        }

        private List<Observation> TranslateSAREF4health_AccelerometerSensor(JToken jsonProp)
        {
            List<Observation> result = new List<Observation>();

            return result;
        }

        private List<Observation> TranslateSAREF4health_ECGLead(JToken jsonProp)
        {
            List<Observation> result = new List<Observation>();

            return result;
        }

        private List<Observation> ExecuteMappingsLogistics()
        {
            List<Observation> result = new List<Observation>();

            return result;
        }

        /// <summary>
        /// deprecated
        /// </summary>
        /// <returns></returns>
        private List<Observation> ExecuteMappingsEhealth()
        {
            List<Observation> result = new List<Observation>();
            // Load Accelerometer
            JToken ecgDeviceConsistsOf = inputData["saref:consistsOf"];
            if (ecgDeviceConsistsOf != null)
            {
                foreach (JToken sensor in ecgDeviceConsistsOf.Children())
                {
                    if (sensor["@type"] != null)
                    {
                        string sensorId = sensor["@id"].ToString();
                        string sensorType = sensor["@type"].ToString();
                        switch (sensorType)
                        {
                            case "saref4health:AccelerometerSensor":

                                double triAxialAccel = 0;
                                bool collisionDetected = false;
                                foreach (JToken measurement in sensor["makesMeasurement"].Children())
                                {
                                    string isMeasuredIn = measurement["isMeasuredIn"].ToString();
                                    string measurementRelatesToProperty = measurement["relatesToProperty"].ToString();
                                    string measurementId = measurement["@id"].ToString();

                                    switch (measurementRelatesToProperty)
                                    {
                                        case "sarefInst:Acceleration_Vehicle":
                                            triAxialAccel = double.Parse(measurement["hasValue"].ToString(), CultureInfo.InvariantCulture);
                                            break;
                                        case "sarefInst:VehicleCollisionDetectedFromMobileDevice":
                                            collisionDetected = measurement["hasValue"].ToString() != "0";
                                            break;
                                        default:
                                            break;
                                    }
                                }

                                //AccelerometerSensorObservation accelSensor = new AccelerometerSensorObservation(sensorId, 0, 0, 0, triAxialAccel, collisionDetected);
                                //result.Add(accelSensor);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            return result;
        }
    }
}

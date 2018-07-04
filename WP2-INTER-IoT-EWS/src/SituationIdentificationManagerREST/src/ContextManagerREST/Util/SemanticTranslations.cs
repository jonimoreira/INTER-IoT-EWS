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
    public class SemanticTranslations
    {
        private JToken inputData;

        public SemanticTranslations(JToken input)
        {
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
                throw new Exception("@context not provided");

            bool isEHealthMessage = false;
            bool isLogisticsMessage = false;

            foreach (JToken token in context.Children().Values())
            {
                if (token.ToString() == "https://w3id.org/def/saref4health#")
                    isEHealthMessage = true;
                if (token.ToString() == "http://ontology.tno.nl/logico#")
                    isLogisticsMessage = true;
            }

            List<Observation> result = new List<Observation>();

            if (isEHealthMessage)
                //result.AddRange(ExecuteMappingsEhealth());
                result.AddRange(ExecuteMappingsFromSAREF());

            if (isLogisticsMessage)
                result.AddRange(ExecuteMappingsLogistics());

            return result;
        }

        private List<Observation> ExecuteMappingsFromSAREF()
        {
            List<Observation> result = new List<Observation>();

            var jsonLdParser = new JsonLdParser();
            TripleStore tStore = new TripleStore();
            using (var reader = new System.IO.StringReader(inputData.ToString(Formatting.Indented)))
            {
                jsonLdParser.Load(tStore, reader);
            }

            Platform platform = null;
            Point geoPoint = null;

            // Query for all geo:location (Device or Sensor) and map to hasLocation (Platform or Sensor)
            string sparqlQuery = @"
                PREFIX saref: <https://w3id.org/saref#>
                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                PREFIX geo: <http://www.w3.org/2003/01/geo/wgs84_pos#>
                
                SELECT ?devicesubclass ?device ?location ?lat ?long 
                WHERE  
	                {
					?device geo:location ?location.
	                ?device rdf:type ?devicesubclass.
	                ?location geo:lat ?lat.
	                ?location geo:long ?long
	                } 

            ";
            Object results = tStore.ExecuteQuery(sparqlQuery);
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
                    platform = new Platform(deviceId, geoPoint);

                    break;
                }
            }

            // Query for all sensor measurements (SAREF) -> observations (SOSA)
            // Order by timestamp (SAREF) -> Observation.resultTime
            sparqlQuery = @"
                PREFIX saref: <https://w3id.org/saref#>
                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                                
                SELECT ?sensor ?measurement ?measValue ?measUnit ?measProp ?measTime
                WHERE  
	                {
					?sensor saref:makesMeasurement ?measurement.
	                ?measurement saref:hasValue ?measValue.
	                ?measurement saref:hasTimestamp ?measTime. 
	                ?measurement saref:isMeasuredIn ?measUnit. 
	                ?measurement saref:relatesToProperty ?measProp.					
	                } 
                ORDER BY ?measTime

            ";

            results = tStore.ExecuteQuery(sparqlQuery);

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

                    string sensorId = spqlResult["sensor"].ToString();
                    Sensor sensor;
                    if (sensorsDic.ContainsKey(sensorId))
                        sensor = sensorsDic[sensorId];
                    else
                    {
                        sensor = new Sensor(sensorId, platform);
                        sensorsDic.Add(sensorId, sensor);
                    }
                    
                    string measurementId = spqlResult["measurement"].ToString();

                    LiteralNode measurementValueNode = (LiteralNode) spqlResult.Value("measValue");
                    double measurementValue = measurementValueNode.AsValuedNode().AsDouble(); 

                    LiteralNode measurementTimeNode = (LiteralNode)spqlResult.Value("measTime");
                    DateTime measDateTime = DateTime.Parse(measurementTimeNode.Value); 

                    string measurementUnit = spqlResult["measUnit"].ToString();
                    string measurementProperty = spqlResult["measProp"].ToString();

                    Observation observation = new Observation(measurementId, sensor, messageId);
                    observation.hasSimpleResult = i++; // Test
                    
                    // sosa:hasResult predicate
                    observation.hasResult = new Result();
                    observation.hasResult.hasValue = measurementValue;
                    observation.hasResult.hasUnit = measurementUnit;

                    // sosa:obesrvedProperty predicate
                    string observablePropertyId = measurementProperty + "_" + platform.Identifier + "_" + Guid.NewGuid();
                    observation.observedProperty = new ObservableProperty(observablePropertyId, measurementProperty);

                    // sosa:resultTime predicate
                    observation.resultTime = measDateTime;

                    if (measurementProperty == "https://w3id.org/saref/instances#VehicleCollisionDetectedFromECGDeviceAccelerometerComputedByMobile")
                        AddVehicleCollisionDetected(observation, result);
                    //else if (measurementProperty == "https://w3id.org/saref/instances#VehicleCollisionDetectedFromECGDeviceAccelerometerComputedByMobile")

                    else
                        AddObservation(observation, result);
                    
                }
            }


                    /*
                    foreach (Triple triple in tStore.Triples)
                    {
                        string rdf_subject = triple.Subject.ToString();
                        string rdf_predicate = triple.Predicate.ToString();
                        string rdf_object = triple.Object.ToString();

                        if (IsRDFtype(rdf_predicate, rdf_object, "https://w3id.org/saref#Device"))
                        {
                            //result.AddRange(Translate_SAREF_Device(rdf_subject, tStore));
                        }
                        else if (IsRDFtype(rdf_predicate, rdf_object, "https://w3id.org/saref#Sensor"))
                        {
                            result.AddRange(Translate_SAREF_Sensor(rdf_subject, tStore));
                        }
                    }



                    // saref:consistsOf => Mereology (composition) relationship 
                    JToken deviceConsistsOf = inputData["saref:consistsOf"];
                    if (deviceConsistsOf != null)
                    {
                        foreach (JToken jsonProp in (JArray)deviceConsistsOf) //deviceConsistsOf.Children())
                        {
                            if (jsonProp["@type"] != null)
                            {
                                string jsonPropId = jsonProp["@id"].ToString();
                                string jsonPropType = jsonProp["@type"].ToString();
                                switch (jsonPropType)
                                {
                                    case "saref4health:ECGDevice":
                                        result.AddRange(TranslateSAREF4health_ECGDevice(jsonProp));
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

                    // saref:makesMeasurement => Observation / measurement 
                    JToken makesMeasurement = inputData["saref:makesMeasurement"];
                    if (makesMeasurement != null)
                    {
                        result.AddRange(TranslateSAREF_makesMeasurement(makesMeasurement));
                    }

                    // Sets the location of the observations
                    // geo:location => Location/position (geo-reference)
                    JToken geoLocation = inputData["geo:location"];
                    if (geoLocation != null)
                    {
                        SetObservationsLocation(geoLocation, result);
                    }

                    */

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

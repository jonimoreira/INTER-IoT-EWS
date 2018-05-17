using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using ShimmerAPI;
using Newtonsoft.Json.Linq;
using JsonLD.Core;
using JsonLD.Util;

namespace ShimmerCaptureXamarin.SAREF4health
{
    public class JsonLD
    {
        public string hasFrequencyMeasurement = "sarefInst:FrequencyOf256Hertz"; // "sarefInst:FrequencyOf512Hertz";
        public string measuresPropertyECGdata = "sarefInst:HeartElectricalActivity_Person";
        public string isMeasuredInECGdata = "sarefInst:ElectricPotential_MilliVolts";
        
        public JsonLD()
        { }

        public string GeneratePID(Measurement msg)
        {
            string result = msg.RelatesToProperty + "_Test.1.1_" + msg.HasTimestamp; // string.Empty;

            //result = msg.Type + "_Test.1.1_" + msg.HasTimestamp;

            return result;
        }

        public string GeneratePID_SAREF4health_ECGSampleSequence(string type, string timestamp)
        {
            string result = string.Empty;

            result = type + "_Test.1.1_" + timestamp;

            return result;
        }

        public string GeneratePID(JsonObjectAttribute msg)
        {
            string result = "sarefInst:Message_Test.1.1_1511466006.9682777";

            //result = msg.Type + "_Test.1.1_" + msg.HasTimestamp;

            return result;
        }

        public string TranslateIsMeasuredIn(SensorData sensorData)
        {
            string result = "saref:SpeedUnit_MeterPerSecond";

            switch (sensorData.Unit)
            {
                case "m/(sec^2)":
                    result = "sarefInst:AccelerationUnit_MetrePerSecondSquare";
                    break;
                case "mVolts":
                    result = "sarefInst:ElectricPotential_MilliVolts";
                    break;
                case "cross-axial-energy":
                    result = "sarefInst:TriAxialAccelerationEnergy_MetrePerSecondSquare";
                    break;
                case "boolean":
                    result = "sarefInst:Boolean";
                    break;
                default:
                    // missing: kPa||Celcius* (pressure and temperature)
                    break;
            }

            //Console.WriteLine("sensorData.ToString()=" + sensorData.ToString());

            return result;
        }

        public string TranslateRelatesToProperty(string signalName)
        {
            string result = "sarefInst:Acceleration_Vehicle";

            if (signalName == Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_X
                        || signalName == "accelerationCrossAxial")
                result = "sarefInst:Acceleration_Vehicle";
            else if (signalName == Shimmer3Configuration.SignalNames.ECG_LL_RA
                        || signalName == Shimmer3Configuration.SignalNames.ECG_LA_RA
                        || signalName == Shimmer3Configuration.SignalNames.ECG_VX_RL)
                result = "sarefInst:HeartElectricalActivity_Person";
            else if (signalName == "collisionDetected")
                result = "sarefInst:VehicleCollisionDetectedFromMobileDevice";
            else if (signalName == "batteryLevel")
                result = "sarefInst:BatteryLevel";

            return result;
        }


        public string TranslateMeasurementType(string signalName)
        {
            string result = "saref:Measurement";

            if (signalName == Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_X)
                result = "saref:Measurement";
            else if (signalName == Shimmer3Configuration.SignalNames.ECG_LL_RA
                        || signalName == Shimmer3Configuration.SignalNames.ECG_LA_RA
                        || signalName == Shimmer3Configuration.SignalNames.ECG_VX_RL)
                result = "saref4health:ECGMeasurement"; // TODO: remove, does not make sense anymore

            return result;
        }


        public JObject GetContextJSON_SAREF4health()
        {
            JObject contextJSON_SAREF4health = new JObject();

            string context = @"
{  
    'comment' : {
      '@id' : 'http://www.w3.org/2000/01/rdf-schema#comment'
    },
    'label' : {
      '@id' : 'http://www.w3.org/2000/01/rdf-schema#label'
    },
    'seeAlso' : {
      '@id' : 'http://www.w3.org/2000/01/rdf-schema#seeAlso'
    },
    'consistsOf' : {
      '@id' : 'https://w3id.org/saref#consistsOf',
      '@type' : '@id'
    },
    'hasManufacturer' : {
      '@id' : 'https://w3id.org/saref#hasManufacturer'
    },
    'hasFrequencyMeasurement' : {
      '@id' : 'https://w3id.org/def/saref4envi#hasFrequencyMeasurement',
      '@type' : '@id'
    },
    'hasTypicalConsumption' : {
      '@id' : 'https://w3id.org/saref#hasTypicalConsumption',
      '@type' : '@id'
    },
    'accomplishes' : {
      '@id' : 'https://w3id.org/saref#accomplishes',
      '@type' : '@id'
    },
    'makesMeasurement' : {
      '@id' : 'https://w3id.org/saref#makesMeasurement',
      '@type' : '@id'
    },
    'measuresProperty' : {
      '@id' : 'https://w3id.org/saref#measuresProperty',
      '@type' : '@id'
    },
    'hasValues' : {
      '@id' : 'https://w3id.org/def/saref4health#hasValues',
      '@type' : 'http://www.w3.org/2001/XMLSchema#float'
    },
    'isMeasuredIn' : {
      '@id' : 'https://w3id.org/saref#isMeasuredIn',
      '@type' : '@id'
    },
    'hasTimestamp' : {
      '@id' : 'https://w3id.org/saref#hasTimestamp',
      '@type' : 'http://www.w3.org/2001/XMLSchema#dateTime'
    },
    'relatesToProperty' : {
      '@id' : 'https://w3id.org/saref#relatesToProperty',
      '@type' : '@id'
    },
    'schema' : 'http://schema.org/',
    'xsd' : 'http://www.w3.org/2001/XMLSchema#',
    'xml' : 'http://www.w3.org/XML/1998/namespace',
    'owl' : 'http://www.w3.org/2002/07/owl#',
    'rdf' : 'http://www.w3.org/1999/02/22-rdf-syntax-ns#',
    'rdfs' : 'http://www.w3.org/2000/01/rdf-schema#',
    'saref' : 'https://w3id.org/saref#',
    'saref4envi' : 'https://w3id.org/def/saref4envi#',
    'saref4health' : 'https://w3id.org/def/saref4health#',
    'sarefInst' : 'https://w3id.org/saref/instances#',
    'm3' : 'http://purl.org/iot/vocab/m3-lite#',
    'quantity' : 'http://purl.oclc.org/NET/ssnx/qu/quantity#',
    'qu' : 'http://purl.oclc.org/NET/ssnx/qu/qu#',
    'unit' : 'http://purl.oclc.org/NET/ssnx/qu/unit#',
    'qu-rec20' : 'http://purl.oclc.org/NET/ssnx/qu/qu-rec20#',
    'dim' : 'http://purl.oclc.org/NET/ssnx/qu/dim#',
    'prefix' : 'http://purl.oclc.org/NET/ssnx/qu/prefix#',
    'skos' : 'http://www.w3.org/2004/02/skos/core#',
    'geo' : 'http://www.w3.org/2003/01/geo/wgs84_pos#',
    'dct' : 'http://purl.org/dc/terms/',
    'dcterms' : 'http://purl.org/dc/terms/',
    'vann' : 'http://purl.org/vocab/vann/',
    'foaf' : 'http://xmlns.com/foaf/0.1/',
    'om' : 'http://www.wurvoc.org/vocabularies/om-1.8/',
    'cc' : 'http://creativecommons.org/ns#',
    'time' : 'http://www.w3.org/2006/time#',
    'dc' : 'http://purl.org/dc/elements/1.1/'
  
}
            ";

            contextJSON_SAREF4health = JObject.Parse(context);

            return contextJSON_SAREF4health;
        }




        /* 
         * ECG Vx-RL||ECG Vx-RL||EXG2 CH1||EXG2 CH1||EXG2 Sta||ECG LA-RA||ECG LA-RA||ECG LL-RA||ECG LL-RA||EXG1 Status|
         * |Temperature||Temperature||Pressure||Pressure|
         * |Low Noise Accelerometer Z||Low Noise Accelerometer Z||Low Noise Accelerometer Y||Low Noise Accelerometer Y||Low Noise Accelerometer X||Low Noise Accelerometer X|
         * |System Timestamp||Timestamp||Timestamp||
         * 
         * -40.5198783302162||-561827||-2.19516601504874||-30437||128||9.28118816389897||128688||-12.0892437802844||-167623||128|
         * |27.97772501633||25917||100.821616660031||42217|
         * |10.0120481927711||1216||-0.542168674698795||2023||0.289156626506024||2092|
         * |1515623966158.47||21402.34375||1988534||
        */

        public Measurement TranslateMeasurement(string signalName, SensorData sensorData, double timestamp)
        {
            Measurement msg = new Measurement();
            msg.HasTimestamp = timestamp;
            msg.IsMeasuredIn = TranslateIsMeasuredIn(sensorData); //"saref:SpeedUnit_MeterPerSecond";
            msg.RelatesToProperty = TranslateRelatesToProperty(signalName);
            msg.Type = TranslateMeasurementType(signalName); //"saref:SpeedMeasurement";

            msg.Label = "Measurement of Shimmer 3 ECG [" + signalName + "]_" + timestamp;
            msg.HasValue = sensorData.Data;

            msg.Id = GeneratePID(msg);

            string sarefMakesMeasurementItemJSONstr = @"
{
  '@id' : '" + msg.Id + @"',
  '@type' : '" + msg.Type + @"',
  'label' : '" + msg.Label + @"',
  'hasTimestamp' : '" + msg.HasTimestamp + @"',
  'hasValue' : '" + msg.HasValue + @"',
  'isMeasuredIn' : '" + msg.IsMeasuredIn + @"',
  'relatesToProperty' : '" + msg.RelatesToProperty + @"',
}
            ";

            JObject sarefMakesMeasurementItemJSON = JObject.Parse(sarefMakesMeasurementItemJSONstr);

            msg.JSONLDobject = sarefMakesMeasurementItemJSON;

            return msg;
        }



        public JObject TranslateECGSampleSequence_SAREF4health(List<KeyValuePair<double, double>> values)
        {
            //System.Console.WriteLine("[TranslateECGSampleSequence] values.Count=" + values.Count);
            if (values.Count == 0)
                return null;

            double timestamp = values[0].Key;
            List<double> seriesValues = new List<double>();
            string seriesValuesStr = string.Empty;
            foreach (KeyValuePair<double, double> measurement in values)
            {
                //Needs improvement: copy array / matrix functions
                int currVal = FormatECGMeasurementValue(measurement.Value);
                seriesValues.Add(currVal);
                seriesValuesStr += "," + currVal;
            }

            seriesValuesStr = seriesValuesStr.Substring(1);

            string sarefMakesMeasurementItemJSONstr = @"
{
  '@id' : '" + GeneratePID_SAREF4health_ECGSampleSequence("saref4health:ECGSampleSequence", timestamp.ToString()) + @"',
  '@type' : 'saref4health:ECGSampleSequence',
  'label' : 'ECG measurements series from lead at " + timestamp + @"',
  'hasFrequencyMeasurement' : '" + hasFrequencyMeasurement + @"',
  'hasValues' : [ " + seriesValuesStr + @" ],
  'hasTimestamp' : '" + timestamp + @"',
  'isMeasuredIn' : '" + isMeasuredInECGdata + @"',
  'relatesToProperty' : '" + measuresPropertyECGdata + @"'
}
            ";

            JObject sarefMakesMeasurementItemJSON = JObject.Parse(sarefMakesMeasurementItemJSONstr);

            return sarefMakesMeasurementItemJSON;
        }


        public JObject GetECGDeviceJSON_SAREF4health(JObject contextJSON, List<JObject> listSensorsOfDevice)
        {
            return GetECGDeviceJSON_SAREF4health(contextJSON, listSensorsOfDevice, "sarefInst:RecordingECGSession_01");
        }

        public JObject GetECGDeviceJSON_SAREF4health(JObject contextJSON, List<JObject> listSensorsOfDevice, string recordingECGSession)
        {
            string deviceId = "sarefInst:Shimmer3ECG_unit_T9JRN42_" + Android.OS.Build.Serial;
            JObject eCGDeviceJSON = JObject.FromObject(new
            {
                @context = contextJSON,
                @id = deviceId,
                @type = "saref4health:ECGDevice",
                comment = "Shimmer3 ECG unit (T9J-RN42): INTER-IoT-EWS project",
                label = "Shimmer3 ECG unit T9J-RN42",
                seeAlso = "http://www.shimmersensing.com/products/ecg-development-kit#specifications-tab",
                hasFrequencyMeasurement = hasFrequencyMeasurement,
                accomplishes = recordingECGSession,
                hasManufacturer = "Shimmer",
                hasTypicalConsumption = "sarefInst:Shimmer3ECGBattery",
                consistsOf = listSensorsOfDevice
            });

            return eCGDeviceJSON;
        }


        public JObject GetSensorJSON_SAREF4health(JObject measurementsSeries, string type, string id, string label, string measuresProperty)
        {
            JObject eCGLeadJSON = JObject.FromObject(new
            {
                @id = id,
                @type = type,
                label = label,
                measuresProperty = measuresProperty,
                makesMeasurement = measurementsSeries
            });
            return eCGLeadJSON;
        }

        public JObject GetShimmerAccelerometerSensorJSON_SAREF4health(Measurement measurement, Measurement collisionDetectedMeasurement)
        {
            List<JObject> measurements = new List<JObject>();
            measurements.Add(measurement.JSONLDobject);
            measurements.Add(collisionDetectedMeasurement.JSONLDobject);

            JObject accelJSON = JObject.FromObject(new
            {
                @id = "sarefInst:Shimmer3AccelerometerSensor_T9JRN42",
                @type = "saref4health:AccelerometerSensor",
                comment = "Shimmer3 Accelerometer sensor",
                label = "Shimmer3 Accelerometer T9J-RN42",
                measuresProperty = "sarefInst:Acceleration_Vehicle",
                makesMeasurement = measurements
            });
            return accelJSON;
        }


        public JObject GetGraph1JSON()
        {
            JObject graph1JSON = JObject.FromObject(new
            {
                @id = "InterIoTMsg:meta66b05c61-d687-45a3-b5fb-6864bbec3b69",
                @type = new[]
                {
                    "InterIoTMsg:Thing_update",
                    "InterIoTMsg:meta"
                },
                InterIoTMsg_conversationID = "conv99528eba-eb2d-47e8-9ee6-9dd40d19f89a",
                InterIoTMsg_dateTimeStamp = "2017-05-22T22:19:30.281+02:00",
                InterIoTMsg_messageID = "msg7e484a2c-f959-486e-8da0-31143f457234"

            });
            return graph1JSON;
        }

        public JObject GetGeoLocationJSON()
        {
            JObject geoLocationJSON = JObject.FromObject(new
            {

                @id = "sarefInst:test.1.1.LocationSmartPhone_39.431478658043424_-0.35860926434736484",
                @type = new[]
                    {
                        "owl:NamedIndividual",
                        "geo:SpatialThing"
                    },
                @label = new
                {
                    @language = "en",
                    @value = "Location of the smartphone, should be the same location of the truck (?)"
                },
                geo_latitude = 39.431478658043424,
                geo_longitude = -0.35860926434736484

            });
            return geoLocationJSON;
        }


        public JObject GetGraph2JSON(JObject geoLocationJSON, List<JObject> sarefMakesMeasurementItemList)
        {
            // Location only to be used in MyDriving? Here I can also include the mobile's position...
            JObject graph2JSON = JObject.FromObject(new
            {

                @id = "sarefInst:exampleSmartPhoneSendingInfoTruck",
                @type = "saref:Device",
                @label = new
                {
                    @language = "en",
                    @value = "Motorola Moto G5 Plus"
                },
                geo_location = geoLocationJSON,
                saref_makesMeasurement = sarefMakesMeasurementItemList

            });
            return graph2JSON;
        }

        public JObject GetGraph2JSON(List<JObject> sarefMakesMeasurementItemList)
        {
            JObject graph2JSON = JObject.FromObject(new
            {

                @id = "sarefInst:exampleSmartPhoneSendingInfoTruck",
                @type = "saref:Device",
                @label = new
                {
                    @language = "en",
                    @value = "Motorola Moto G5 Plus"
                },
                saref_makesMeasurement = sarefMakesMeasurementItemList

            });
            return graph2JSON;
        }



        public string FormatJSONLD01(ObjectCluster objectCluster)
        {
            // https://github.com/NuGet/json-ld.net
            // https://github.com/jsonld-java/jsonld-java

            /*
            JObject jsonObject = new JObject();
            
            //JToken context = new JToken();
            // Customise options...
            // Call whichever JSONLD function you want! (e.g. compact)
            Object compact = JsonLdProcessor.Compact(jsonObject, context, options);
            // Print out the result (or don't, it's your call!)
            */

            /*
            JsonLdOptions options = new JsonLdOptions();
            JObject context = new JObject();
            //context.Add()
            //options.SetExpandContext()
            // ... the contents of "contexts/example.jsonld"
            String jsonContext = "{ \"@contxt\": { ... } }";
            //dl..LoadDocument(.addInjectedDoc("http://www.example.com/context", jsonContext);
            //options.documentLoader = dl;

            InputStream inputStream = new FileInputStream("input.json");
            Object jsonObject = JsonUtils.fromInputStream(inputStream);
            HashMap context = new HashMap();
            Object compact = JsonLdProcessor.compact(jsonObject, context, options);
            System.out.println(JsonUtils.toPrettyString(compact));
            */

            //List<Double> data = objectCluster.GetData();
            //List<String> dataNames = objectCluster.GetNames();

            // Create an instance of JsonLdOptions with the standard JSON-LD options
            JsonLdOptions options = new JsonLdOptions();



            JObject message = JObject.FromObject(new
            {
                channel = new
                {
                    title = "James Newton-King",
                    link = "http://james.newtonking.com",
                    description = "James Newton-King's blog." /*8,
                    item =
                        from p in posts
                        orderby p.Title
                        select new
                        {
                            title = p.Title,
                            description = p.Description,
                            link = p.Link,
                            category = p.Categories
                        }*/
                }
            });

            JToken context = JToken.FromObject(new
            {
                test = new
                {
                    a = "b"
                }
            });

            message.Add("item", context);

            JObject jsonLDmessage = JsonLdProcessor.Compact(message, GenerateContextForJSONLD(), options);

            //return message.ToString();
            return JSONUtils.ToPrettyString(jsonLDmessage);

        }

        public JToken GenerateContextForJSONLD()
        {
            Dictionary<string, string> dicContexts = new Dictionary<string, string>();
            dicContexts.Add("schema", "http://schema.org/");
            dicContexts.Add("owl", "http://www.w3.org/2002/07/owl#");

            JToken result = JsonConvert.SerializeObject(dicContexts);
            return result;
        }


        public JObject CreateMessageAndDeleteList_SAREF4health(List<KeyValuePair<double, double>> valuesLead)
        {
            JObject result;

            lock (valuesLead)
            {
                result = TranslateECGSampleSequence_SAREF4health(valuesLead);
                // Delete values from memory (empty list)
                valuesLead.Clear();
            }
            return result;
        }

        public string HardCodeReplaceJSONLD(string input)
        {
            string resuult = input.Replace("\"id\"", "\"@id\"").Replace("\"label\"", "\"@label\"").Replace("\"graph\"", "\"@graph\"").Replace("\"context\"", "\"@context\"").Replace("\"language\"", "\"@language\"").Replace("\"type\"", "\"@type\"").Replace("\"value\"", "\"@value\"").Replace("\"geo_", "\"geo:").Replace("\"InterIoTMsg_", "\"InterIoTMsg:").Replace("\"saref_", "\"saref:").Replace("rdfs_", "rdfs:");
            return resuult;
        }

        public string HardCodeReplaceJSONLDforECGDeviceLevel(string input)
        {
            string resuult = input.Replace("\"id\":", "\"@id\":").Replace("\"type\":", "\"@type\":").Replace("\"context\":", "\"@context\":");
            return resuult;
        }



        public int FormatECGMeasurementValue(double originalValue)
        {
            int result = (int)(originalValue * 100.0);
            return result;
        }

    }
}
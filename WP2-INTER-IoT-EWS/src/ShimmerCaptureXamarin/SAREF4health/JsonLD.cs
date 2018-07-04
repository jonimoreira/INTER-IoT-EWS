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
using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace ShimmerCaptureXamarin.SAREF4health
{
    public class JsonLD
    {
        public JObject hasFrequencyMeasurement; 
        public JObject measuresPropertyECGdata;
        public JObject sarefInst_BatteryLevel;
        public JObject isMeasuredInECGdata;
        public JObject hasTypicalConsumption;
        public JObject sarefInst_ProcessedAccelerometer;

        public JsonLD()
        {
            hasFrequencyMeasurement = new JObject();
            hasFrequencyMeasurement.Add("@id", "sarefInst:FrequencyOf256Hertz"); // "sarefInst:FrequencyOf512Hertz";
            hasFrequencyMeasurement.Add("@type", "saref4envi:FrequencyMeasurement");

            measuresPropertyECGdata = new JObject();
            measuresPropertyECGdata.Add("@id", "sarefInst:HeartElectricalActivity_Person");
            measuresPropertyECGdata.Add("@type", "saref4health:HeartElectricalActivity");

            isMeasuredInECGdata = new JObject();
            isMeasuredInECGdata.Add("@id", "sarefInst:ElectricPotential_MilliVolts");
            isMeasuredInECGdata.Add("@type", "saref4health:ElectricPotential");

            hasTypicalConsumption = new JObject();
            hasTypicalConsumption.Add("@id", "sarefInst:Shimmer3ECGTypicalConsumption");
            hasTypicalConsumption.Add("@type", "saref:Power");

            sarefInst_BatteryLevel = new JObject();
            sarefInst_BatteryLevel.Add("@id", "sarefInst:BatteryLevel");
            sarefInst_BatteryLevel.Add("@type", "saref:Property");

            sarefInst_ProcessedAccelerometer = new JObject();
            sarefInst_ProcessedAccelerometer.Add("@id", "sarefInst:ProcessedAccelerometer");
            sarefInst_ProcessedAccelerometer.Add("@type", "saref:Property");

        }

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
            string result = "sarefInst:Acceleration_Average_Axis";

            if (signalName == Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_X)
                result += "X";
            else if (signalName == Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_Y)
                result += "Y";
            else if (signalName == Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_Z)
                result += "Z";
            else if (signalName == "accelerationCrossAxial")
                result = "sarefInst:Acceleration_CrossAxialFunction";
            else if (signalName == Shimmer3Configuration.SignalNames.ECG_LL_RA
                        || signalName == Shimmer3Configuration.SignalNames.ECG_LA_RA
                        || signalName == Shimmer3Configuration.SignalNames.ECG_VX_RL)
                result = "sarefInst:HeartElectricalActivity_Person";
            else if (signalName == "collisionDetected")
                result = "sarefInst:VehicleCollisionDetectedFromECGDeviceAccelerometerComputedByMobile";
            else if (signalName == "cross-axial-energy_mean")
                result = "sarefInst:CrossAxialFunctionMean";
            else if (signalName == "cross-axial-energy_std-dev")
                result = "sarefInst:CrossAxialFunctionStdDev";
            else if (signalName == "cross-axial-energy_max")
                result = "sarefInst:CrossAxialFunction";
            else if (signalName == "ThresholdGforce")
                result = "sarefInst:ThresholdGforce";
            else if (signalName == "batteryLevel")
                result = "sarefInst:BatteryLevel";

            return result;
        }


        public string TranslateMeasurementType(string signalName)
        {
            string result = "saref:Measurement";
            /*
            if (signalName == Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_X)
                result = "saref:Measurement";
            else if (signalName == Shimmer3Configuration.SignalNames.ECG_LL_RA
                        || signalName == Shimmer3Configuration.SignalNames.ECG_LA_RA
                        || signalName == Shimmer3Configuration.SignalNames.ECG_VX_RL)
                result = "saref4health:ECGMeasurement"; // TODO: remove, does not make sense anymore
            */

            return result;
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

            msg.Label = "Measurement Shimmer3 ECG [" + signalName + "]_" + timestamp;
            msg.HasValue = sensorData.Data;

            msg.Id = GeneratePID(msg);

            string sarefMakesMeasurementItemJSONstr = @"
{
  '@id' : '" + msg.Id + @"',
  '@type' : '" + msg.Type + @"',
  'label' : '" + msg.Label + @"',
  'saref:hasTimestamp' : '" + ConvertTimestampXSDdateTime(msg.HasTimestamp) + @"',
  'saref:hasValue' : " + msg.HasValue + @"
}
            ";

            JObject sarefMakesMeasurementItemJSON = JObject.Parse(sarefMakesMeasurementItemJSONstr);

            sarefMakesMeasurementItemJSON.Add("saref:isMeasuredIn", msg.IsMeasuredIn_JsonLD); // 'saref:isMeasuredIn' : '" + msg.IsMeasuredIn + @"',
            sarefMakesMeasurementItemJSON.Add("saref:relatesToProperty", msg.RelatesToProperty_JsonLD); // 'saref:relatesToProperty' : '" + msg.RelatesToProperty + @"',

            msg.JSONLDobject = sarefMakesMeasurementItemJSON;

            return msg;
        }

        public string ConvertTimestampXSDdateTime(double timestamp)
        {
            string result = string.Empty;

            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dateTime = dateTime.AddMilliseconds(timestamp).ToLocalTime();
            result = SoapDateTime.ToString(dateTime);            

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
    'location' : {
      '@id' : 'http://www.w3.org/2003/01/geo/wgs84_pos#location',
      '@type' : '@id'
    },
    'lat' : {
      '@id' : 'http://www.w3.org/2003/01/geo/wgs84_pos#lat',
      '@type' : 'http://www.w3.org/2001/XMLSchema#decimal'
    },
    'long' : {
      '@id' : 'http://www.w3.org/2003/01/geo/wgs84_pos#long',
      '@type' : 'http://www.w3.org/2001/XMLSchema#decimal'
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
        
        public JObject GetFieldGatewayMobileDeviceJSON_SAREF4health(JObject contextJSON, List<JObject> listDevicesOfDevice, GeolocatorManager geolocatorManager)
        {
            string mobileId = "sarefInst:MobileDeviceAsSemanticFieldGateway_MotoG5Plus_" + Android.OS.Build.Serial;
            string label = "Smartphone Motorola G5 Plus used in INTER-IoT-EWS project";

            JObject mobileDeviceJSON = JObject.FromObject(new
            {
                label = label
            });
            mobileDeviceJSON.Add("@context", contextJSON);
            mobileDeviceJSON.Add("@id", mobileId);
            mobileDeviceJSON.Add("@type", "saref:Device");
            mobileDeviceJSON.Add("saref:consistsOf", JToken.FromObject(listDevicesOfDevice));
            
            // If current position not provided, don't include location in the message
            if (geolocatorManager.CurrentPosition != null)
            {
                JObject location = GetLocation(Android.OS.Build.Serial, geolocatorManager.CurrentPosition.Latitude, geolocatorManager.CurrentPosition.Longitude);
                mobileDeviceJSON.Add("geo:location", location);
            }

            return mobileDeviceJSON;
        }
        
        public JObject GetLocation(string deviceId, double lat, double lon)
        {
            var timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            string id = "sarefInst:DeviceCurrentLocation_" + deviceId + "_" + timestamp;

            string locationStr = @"
{
  '@id' : '" + id + @"',
  '@type' : 'geo:Point',
  'geo:lat' : {
        '@type' : 'http://www.w3.org/2001/XMLSchema#float',
        '@value' : '" + lat + @"'
   },
  'geo:long' : {
        '@type' : 'http://www.w3.org/2001/XMLSchema#float',
        '@value' : '" + lon + @"'
   }
}
            ";

            JObject location = JObject.Parse(locationStr);
            return location;
        }


        public JObject TranslateECGSampleSequence_SAREF4health(List<KeyValuePair<double, double>> values, string lead)
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
  '@id' : '" + GeneratePID_SAREF4health_ECGSampleSequence("saref4health:ECGSampleSequence_" + lead, timestamp.ToString()) + @"',
  '@type' : 'saref4health:ECGSampleSequence',
  'label' : 'ECG measurements series from lead at " + timestamp + @"',
  'saref4health:hasValues' : [ " + seriesValuesStr + @" ],
  'saref:hasTimestamp' : '" + ConvertTimestampXSDdateTime(timestamp) + @"'
}
            ";

            JObject sarefMakesMeasurementItemJSON = JObject.Parse(sarefMakesMeasurementItemJSONstr);

            sarefMakesMeasurementItemJSON.Add("saref4envi:hasFrequencyMeasurement", hasFrequencyMeasurement); //'saref4envi:hasFrequencyMeasurement' : '" + hasFrequencyMeasurement + @"',
            sarefMakesMeasurementItemJSON.Add("saref:isMeasuredIn", isMeasuredInECGdata); //   'saref:isMeasuredIn' : '" + isMeasuredInECGdata + @"',
            sarefMakesMeasurementItemJSON.Add("saref:relatesToProperty", measuresPropertyECGdata); //'saref:relatesToProperty' : '" + measuresPropertyECGdata + @"'

            return sarefMakesMeasurementItemJSON;
        }


        public JObject GetECGDeviceJSON_SAREF4health(List<JObject> listSensorsOfDevice)
        {
            JObject recordingECGSession = new JObject();
            recordingECGSession.Add("@id", "sarefInst:RecordingECGSession_01");
            recordingECGSession.Add("@type", "saref4health:ECGRecordingSession");
            /*
              "author" : "#LivingPerson_TruckDriver_01",
              "comment" : "An ECG recording session taken during a trip (truck driver).",
              "label" : "Recording ECG session",
              "hasEnd" : "2018-04-22T22:15:30",
              "hasStart" : "2018-04-22T18:00:00",
             */

            return GetECGDeviceJSON_SAREF4health(listSensorsOfDevice, recordingECGSession);
        }

        public JObject GetECGDeviceJSON_SAREF4health(List<JObject> listDevicesOfDevice, JObject recordingECGSession)
        {
            // TODO: automatically get the Shimmer3 ECG device Id
            string deviceId = "sarefInst:Shimmer3ECG_unit_T9JRN42_DeviceId";
            JObject eCGDeviceJSON = JObject.FromObject(new
            {   
                comment = "Shimmer3 ECG unit (T9J-RN42): INTER-IoT-EWS project",
                label = "Shimmer3 ECG unit T9J-RN42",
                seeAlso = "http://www.shimmersensing.com/products/ecg-development-kit#specifications-tab"
            });
            eCGDeviceJSON.Add("@id", deviceId);
            eCGDeviceJSON.Add("@type", JArray.Parse("[ 'saref4health:ECGDevice', 'saref:Device']"));
            eCGDeviceJSON.Add("saref4envi:hasFrequencyMeasurement", hasFrequencyMeasurement);
            eCGDeviceJSON.Add("saref:accomplishes", recordingECGSession);
            eCGDeviceJSON.Add("saref:hasManufacturer", "Shimmer");
            eCGDeviceJSON.Add("saref:hasTypicalConsumption", hasTypicalConsumption);
            eCGDeviceJSON.Add("saref:consistsOf", JToken.FromObject(listDevicesOfDevice));

            return eCGDeviceJSON;
        }

        public JObject GetSensorJSON_SAREF4health(JObject measurements, string type, string id, string label, JObject measuresProperty)
        {
            JObject eCGLeadJSON = JObject.FromObject(new
            {
                label = label
            });
            eCGLeadJSON.Add("@id", id);

            string types = string.Empty;
            if (type != "saref:Sensor")
                types = "[ '" + type + "', 'saref:Sensor' ]";

            if (types == string.Empty)
                eCGLeadJSON.Add("@type", type);
            else
                eCGLeadJSON.Add("@type", JArray.Parse(types));

            eCGLeadJSON.Add("saref:measuresProperty", measuresProperty);
            eCGLeadJSON.Add("saref:makesMeasurement", measurements);

            return eCGLeadJSON;
        }

        public JObject GetSensorJSON_SAREF4health(List<JObject> measurements, string type, string id, string label, JObject measuresProperty)
        {
            JObject eCGLeadJSON = JObject.FromObject(new
            {
                label = label
            });
            eCGLeadJSON.Add("@id", id);

            string types = string.Empty;
            if (type != "saref:Sensor")
                types = "[ '" + type + "', 'saref:Sensor' ]";

            if (types == string.Empty)
                eCGLeadJSON.Add("@type", type);
            else
                eCGLeadJSON.Add("@type", JArray.Parse(types));

            eCGLeadJSON.Add("saref:measuresProperty", measuresProperty);
            eCGLeadJSON.Add("saref:makesMeasurement", JToken.FromObject(measurements));

            return eCGLeadJSON;
        }

        public JObject GetShimmerAccelerometerSensorJSON_SAREF4health(Measurement measurementX, Measurement measurementY, Measurement measurementZ)
        {
            string types = "[ 'saref4health:AccelerometerSensor', 'saref:Sensor' ]";
            JArray arrayTypes = JArray.Parse(types);
            
            JObject property = new JObject();
            property.Add("@id", "sarefInst:Acceleration_TriAxial");
            string typesProp = @"
                        [ 'dim:Acceleration', 'saref:Property' ]
                    ";
            JArray arrayProp = JArray.Parse(typesProp);
            property.Add("@type", arrayProp);

            List<JObject> measurements = new List<JObject>();
            measurements.Add(measurementX.JSONLDobject);
            measurements.Add(measurementY.JSONLDobject);
            measurements.Add(measurementZ.JSONLDobject);
            
            JObject accelJSON = JObject.FromObject(new
            {
                //comment = "Shimmer3 Accelerometer sensor",
                label = "Shimmer3 Accelerometer T9J-RN42: average acceleration of each axis within ECG device frequency"
            });

            accelJSON.Add("@id", "sarefInst:Shimmer3AccelerometerSensor_Axis_T9JRN42");
            accelJSON.Add("@type", arrayTypes);
            accelJSON.Add("saref:measuresProperty", property);
            accelJSON.Add("saref:makesMeasurement", JToken.FromObject(measurements));

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


        public JObject CreateMessageAndDeleteList_SAREF4health(List<KeyValuePair<double, double>> valuesLead, string lead)
        {
            JObject result;

            lock (valuesLead)
            {
                result = TranslateECGSampleSequence_SAREF4health(valuesLead, lead);
                // Delete values from memory (empty list)
                valuesLead.Clear();
            }
            return result;
        }
        /*
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
        */


        public int FormatECGMeasurementValue(double originalValue)
        {
            int result = (int)(originalValue * 100.0);
            return result;
        }

    }
}
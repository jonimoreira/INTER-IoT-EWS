using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Newtonsoft.Json;
using ShimmerAPI;
using Newtonsoft.Json.Linq;
using MyDriving.EWS.Logistics.LogiServ;
using MyDriving.DataObjects;
//using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace MyDriving.EWS.SAREF4health
{
    public class JsonLD
    {
        public JObject hasFrequencyMeasurement; 
        public JObject measuresPropertyECGdata;
        public JObject sarefInst_BatteryLevel;
        public JObject isMeasuredInECGdata;
        public JObject hasTypicalConsumption;
        public JObject sarefInst_ProcessedAccelerometer;
        public JObject sarefInst_ProcessedHeartRate;

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

            sarefInst_ProcessedHeartRate = new JObject();
            sarefInst_ProcessedHeartRate.Add("@id", "sarefInst:ProcessedHeartRate");
            sarefInst_ProcessedHeartRate.Add("@type", "saref:Property");
            

        }

        public string GeneratePID(Measurement msg)
        {
            string result = msg.RelatesToProperty + "_Test.X.X_" + msg.HasTimestamp; // string.Empty;

            //result = msg.Type + "_Test.1.1_" + msg.HasTimestamp;

            return result;
        }

        public string GeneratePID_SAREF4health_ECGSampleSequence(string type, string timestamp)
        {
            string result = string.Empty;

            result = type + "_Test.X.X_" + timestamp;

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
                case "beats/min":
                    result = "sarefInst:HeartRateUnit_BeatsPerMinute";
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

            if (signalName == Shimmer3Configuration.SignalNames.WIDE_RANGE_ACCELEROMETER_X)
                result += "X";
            else if (signalName == Shimmer3Configuration.SignalNames.WIDE_RANGE_ACCELEROMETER_Y)
                result += "Y";
            else if (signalName == Shimmer3Configuration.SignalNames.WIDE_RANGE_ACCELEROMETER_Z)
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
            else if (signalName == "heartrate")
                result = "sarefInst:HeartRate";

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
            string _label = "Measurement Shimmer3 ECG [" + signalName + "]_" + timestamp;
            Measurement msg = TranslateMeasurement(signalName, sensorData, timestamp, _label);

            return msg;
        }
            

        public Measurement TranslateMeasurement(string signalName, SensorData sensorData, double timestamp, string _label)
        {
            Measurement msg = new Measurement();
            msg.HasTimestamp = timestamp;
            msg.IsMeasuredIn = TranslateIsMeasuredIn(sensorData); //"saref:SpeedUnit_MeterPerSecond";
            msg.RelatesToProperty = TranslateRelatesToProperty(signalName);
            msg.Type = TranslateMeasurementType(signalName); //"saref:SpeedMeasurement";

            msg.Label = _label;
            msg.HasValue = sensorData.Data;

            msg.Id = GeneratePID(msg);
            
            return msg;
        }

        public string ConvertTimestampXSDdateTime(double timestamp)
        {
            string result = string.Empty;

            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dateTime = dateTime.AddMilliseconds(timestamp).ToUniversalTime(); //.ToLocalTime();
            result = dateTime.ToString("o"); // SoapDateTime.ToString(dateTime);            

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
    'offers' : {
      '@id' : 'https://w3id.org/saref#offers',
      '@type' : '@id'
    },
    'intervalOverlaps' : {
      '@id' : 'http://www.w3.org/2006/time#intervalOverlaps',
      '@type' : '@id'
    },
    'hasBeginning' : {
      '@id' : 'http://www.w3.org/2006/time#hasBeginning',
      '@type' : '@id'
    },
    'inXSDDateTime' : {
      '@id' : 'http://www.w3.org/2006/time#inXSDDateTime',
      '@type' : 'http://www.w3.org/2001/XMLSchema#dateTime'
    },
    'hasIDValue' : {
      '@id' : 'http://ontology.tno.nl/logico#hasIDValue'
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
    'dc' : 'http://purl.org/dc/elements/1.1/',
    'LogiServ' : 'http://ontology.tno.nl/logiserv#',
    'LogiCO' : 'http://ontology.tno.nl/logico#'

}
            ";

            contextJSON_SAREF4health = JObject.Parse(context);

            return contextJSON_SAREF4health;
        }
        
        public JObject GetFieldGatewayMobileDeviceJSON_SAREF4health(string mobileDeviceId, JObject contextJSON, List<JObject> listDevicesOfDevice, double smartphoneLocationLatitude, double smartphoneLocationLongitude, string tripId)
        {
            string mobileId = "sarefInst:MobileDeviceAsSemanticFieldGateway_MotoG5Plus_" + mobileDeviceId;
            string label = "Smartphone";
            string comment = "Smartphone Motorola G5 Plus used in INTER-IoT-EWS project";

            JObject mobileDeviceJSON = new JObject();
            mobileDeviceJSON.Add("@context", contextJSON);
            mobileDeviceJSON.Add("@id", mobileId);
            mobileDeviceJSON.Add("@type", "saref:Device");
            mobileDeviceJSON.Add("rdfs:label", label);
            mobileDeviceJSON.Add("rdfs:comment", comment);
            mobileDeviceJSON.Add("saref:measuresProperty", GetFieldGatewayMeasuresProperty());
            if (listDevicesOfDevice.Count > 0)
                mobileDeviceJSON.Add("saref:consistsOf", JToken.FromObject(listDevicesOfDevice));

            if (smartphoneLocationLatitude != double.MinValue && smartphoneLocationLongitude != double.MinValue)
            {
                JObject location = GetLocation(mobileDeviceId, smartphoneLocationLatitude, smartphoneLocationLongitude);
                mobileDeviceJSON.Add("geo:location", location);
            }

            mobileDeviceJSON.Add("saref:offers", GetTrackTransportationService(tripId));
            
            return mobileDeviceJSON;
        }

        private JObject GetFieldGatewayMeasuresProperty()
        {
            JObject result = new JObject();

            result.Add("@id", "sarefInst:PersonTransportingGoods");
            result.Add("@type", "saref:Property");

            return result;
        }

        private JObject GetECGdeviceMeasuresProperty()
        {
            JObject result = new JObject();

            result.Add("@id", "sarefInst:CardiacBehavior");
            result.Add("@type", "saref:Property");

            return result;
        }

        private JObject GetTrackTransportationService(string tripId)
        {
            JObject result = new JObject();

            result.Add("@id", "sarefInst:ServiceTrackTransportation_" + tripId);
            result.Add("@type", "saref:Service");
            result.Add("rdfs:label", tripId);

            return result;
        }

        public JObject GetLocation(string deviceId, double lat, double lon)
        {
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            var timestamp = unixTimestamp; //new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
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
  'saref4health:hasValues' : { '@list' : [ " + seriesValuesStr + @" ] },
  'saref:hasTimestamp' : '" + ConvertTimestampXSDdateTime(timestamp) + @"'
}
            ";

            JObject sarefMakesMeasurementItemJSON = JObject.Parse(sarefMakesMeasurementItemJSONstr);

            sarefMakesMeasurementItemJSON.Add("saref4envi:hasFrequencyMeasurement", hasFrequencyMeasurement); //'saref4envi:hasFrequencyMeasurement' : '" + hasFrequencyMeasurement + @"',
            sarefMakesMeasurementItemJSON.Add("saref:isMeasuredIn", isMeasuredInECGdata); //   'saref:isMeasuredIn' : '" + isMeasuredInECGdata + @"',
            sarefMakesMeasurementItemJSON.Add("saref:relatesToProperty", measuresPropertyECGdata); //'saref:relatesToProperty' : '" + measuresPropertyECGdata + @"'

            return sarefMakesMeasurementItemJSON;
        }


        public JObject GetECGDeviceJSON_SAREF4health(List<JObject> listSensorsOfDevice, string tripId, DateTime tripBeginTime)
        {
            JObject recordingECGSession = new JObject();
            recordingECGSession.Add("@id", "sarefInst:RecordingECGSession_" + tripId);
            recordingECGSession.Add("@type", JArray.Parse("[ 'saref4health:ECGRecordingSession', 'time:ProperInterval']"));
            recordingECGSession.Add("time:intervalOverlaps", GetTripInformation(tripId, tripBeginTime));
            //recordingECGSession.Add("rdf:value", tripId);
            /*
              "author" : "#LivingPerson_TruckDriver_01",
              "comment" : "An ECG recording session taken during a trip (truck driver).",
              "label" : "Recording ECG session",
              "hasEnd" : "2018-04-22T22:15:30",
              "hasStart" : "2018-04-22T18:00:00",
             */

            return GetECGDeviceJSON_SAREF4health(listSensorsOfDevice, recordingECGSession);
        }

        private JObject GetTripInformation(string tripId, DateTime tripBeginTime)
        {
            Transport transport = new Logistics.LogiServ.Transport();
            transport.Identifier = tripId;
            transport.hasBeginning = tripBeginTime;
            return GetLogicoTransport(transport);
        }

        public JObject GetECGDeviceJSON_SAREF4health(List<JObject> listDevicesOfDevice, JObject recordingECGSession)
        {
            // TODO: automatically get the Shimmer3 ECG device Id
            string deviceId = "sarefInst:Shimmer3ECG_unit_T9JRN42_DeviceId";
            JObject eCGDeviceJSON = JObject.FromObject(new
            {   
                comment = "Shimmer3 ECG unit (T9J-RN42): INTER-IoT-EWS project",
                label = "Shimmer3ECG",
                seeAlso = "http://www.shimmersensing.com/products/ecg-development-kit#specifications-tab"
            });
            eCGDeviceJSON.Add("@id", deviceId);
            eCGDeviceJSON.Add("@type", JArray.Parse("[ 'saref4health:ECGDevice', 'saref:Device']"));
            eCGDeviceJSON.Add("saref4envi:hasFrequencyMeasurement", hasFrequencyMeasurement);
            eCGDeviceJSON.Add("saref:accomplishes", recordingECGSession);
            eCGDeviceJSON.Add("saref:hasManufacturer", "Shimmer");
            eCGDeviceJSON.Add("saref:hasTypicalConsumption", hasTypicalConsumption);
            eCGDeviceJSON.Add("saref:consistsOf", JToken.FromObject(listDevicesOfDevice));
            eCGDeviceJSON.Add("saref:measuresProperty", GetECGdeviceMeasuresProperty());

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

        public JObject GetAccelerometerSensorJSON_SAREF4health(string _id, string _label, Measurement measurementX, Measurement measurementY, Measurement measurementZ)
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

            JObject accelJSON = new JObject();
            accelJSON.Add("@id", _id);
            accelJSON.Add("@type", arrayTypes);
            accelJSON.Add("label", _label);
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
        
        public int FormatECGMeasurementValue(double originalValue)
        {
            int result = (int)(originalValue * 100.0);
            return result;
        }
        

        #region Logistics: LogiCO/LogiServ/LogiTran


        public JObject GetLogicoTransport(Transport transport)
        {
            JObject result = new JObject();
            JObject time_Instant_Begin = GetTimeInstant(transport.hasBeginning);
            JObject time_Instant_End = GetTimeInstant(transport.hasEnd);

            result.Add("@id", "LogiServInst:Transport_" + transport.Identifier);
            result.Add("@type", "LogiServ:Transport");
            result.Add("time:hasBeginning", time_Instant_Begin);
            result.Add("LogiCO:hasIDValue", transport.Identifier);
            //result.Add("time:hasEnd", time_Instant_End);

            return result;
        }

        private JObject GetTimeInstant(DateTime hasBeginning)
        {
            JObject result = new JObject();

            result.Add("@id", "timeInst:Instant_" + hasBeginning.ToString("o"));
            result.Add("@type", "time:Instant");
            result.Add("time:inXSDDateTime", hasBeginning.ToString("o"));

            return result;
        }

        public JObject FormatMessageLogistics(Trip currentTrip, TripPoint currentPoint)
        {
            Transport transport = TranslateTransport(currentTrip, currentPoint);

            string transportEventId = transport.Identifier + "_" + Guid.NewGuid();
            JObject location = GetLogicoLocation(currentPoint);
            JObject time_now = GetTimeInstant(DateTime.UtcNow);
            JObject truck = GetLogicoTruck(transport);
            JObject transport_jo = GetLogicoTransport(transport);

            JObject result = new JObject();

            JObject contextJSON = GetContextLogistics();
            result.Add("@context", contextJSON);
            result.Add("@id", "LogiTrans:TransportEvent_" + transportEventId);
            result.Add("@type", "LogiTrans:TransportEvent");
            result.Add("LogiCO:hasLocation", location);
            result.Add("LogiServ:hasTime", time_now);
            result.Add("LogiTrans:hasTransportHandlingUnit", truck);
            result.Add("dul:isComponentOf", transport_jo);

            return result;
        }

        private JObject GetLogicoTruck(Transport transport)
        {
            JObject result = new JObject();

            string truck_plate = "XPT01298";
            JObject cargo = GetLogicoCargo(transport);
            JObject plate = GetLogicoTruckId(truck_plate);

            result.Add("@id", "LogiCO:Truck_" + truck_plate);
            result.Add("@type", "LogiCO:Truck");
            result.Add("LogiCO:contains", cargo);
            result.Add("LogiCO:hasID", plate);

            return result;
        }

        private JObject GetLogicoTruckId(string truck_plate)
        {
            JObject result = new JObject();

            result.Add("@id", "LogiCO:Identifier_Truck_" + truck_plate);
            result.Add("@type", "LogiCO:Identifier");
            result.Add("rdfs:label", truck_plate);

            return result;
        }

        private JObject GetLogicoCargo(Transport transport)
        {
            JObject result = new JObject();

            string cargoId = transport.Identifier + "_CargoId_TransportingGoodsAt_Location_" + DateTime.UtcNow.ToString("o");
            bool isDangerous_GoodsBeingTransported = true;
            JArray goodsItem = GetLogicoGoodsItem(transport);

            result.Add("@id", "LogiTrans:Cargo_" + cargoId);
            result.Add("@type", "LogiServ:Cargo");
            result.Add("rdfs:label", "Cargo being transported by a truck, contains goods");
            result.Add("LogiCO:isDangerous", isDangerous_GoodsBeingTransported);
            result.Add("LogiCO:contains", goodsItem);

            return result;
        }

        private JArray GetLogicoGoodsItem(Transport transport)
        {
            JArray array = new JArray();
            for (int i = 0; i < 5; i++)
            {
                JObject result = new JObject();

                string itemId = transport.Identifier + "_GoodsItemId";

                result.Add("@id", "LogiTrans:GoodsItem_" + itemId);
                result.Add("@type", "LogiTrans:GoodsItem");
                result.Add("rdfs:label", "Goods item " + i);

                array.Add(result);
            }
            return array;
        }

        private JObject GetLogicoLocation(TripPoint currentPoint)
        {
            JObject result = new JObject();

            JObject locationId = GetLogicoLocationId(currentPoint);

            result.Add("@id", "LogiTrans:TransportLocation_" + currentPoint.Latitude + "_" + currentPoint.Longitude);
            result.Add("@type", "LogiTrans:TransportLocation");
            result.Add("LogiCO:hasID", locationId);

            return result;
        }

        private JObject GetLogicoLocationId(TripPoint currentPoint)
        {
            JObject result = new JObject();

            JObject lat = new JObject();
            lat.Add("@type", "http://www.w3.org/2001/XMLSchema#float");
            lat.Add("@value", currentPoint.Latitude);

            JObject lon = new JObject();
            lon.Add("@type", "http://www.w3.org/2001/XMLSchema#float");
            lon.Add("@value", currentPoint.Longitude);

            result.Add("@id", "LogiCO:Identifier_Location_" + currentPoint.Latitude + "_" + currentPoint.Longitude);
            result.Add("@type", JArray.Parse("['LogiCO:Identifier','geo:Point']"));
            result.Add("geo:lat", lat);
            result.Add("geo:long", lon);

            return result;
        }


        public JObject GetContextLogistics()
        {
            JObject contextJO = new JObject();

            string context = @"
{
    'hasConstituent' : {
      '@id' : 'http://www.ontologydesignpatterns.org/ont/dul/DUL.owl#hasConstituent',
      '@type' : '@id'
    },
    'hasConsumer' : {
      '@id' : 'http://ontology.tno.nl/logiserv#hasConsumer',
      '@type' : '@id'
    },
    'isConstituentOf' : {
      '@id' : 'http://www.ontologydesignpatterns.org/ont/dul/DUL.owl#isConstituentOf',
      '@type' : '@id'
    },
    'label' : {
      '@id' : 'http://www.w3.org/2000/01/rdf-schema#label'
    },
    'hasDestination' : {
      '@id' : 'http://ontology.tno.nl/logiserv#hasDestination',
      '@type' : '@id'
    },
    'hasEnd' : {
      '@id' : 'http://www.w3.org/2006/time#hasEnd',
      '@type' : '@id'
    },
    'hasBeginning' : {
      '@id' : 'http://www.w3.org/2006/time#hasBeginning',
      '@type' : '@id'
    },
    'hasProvider' : {
      '@id' : 'http://ontology.tno.nl/logiserv#hasProvider',
      '@type' : '@id'
    },
    'hasOrigin' : {
      '@id' : 'http://ontology.tno.nl/logiserv#hasOrigin',
      '@type' : '@id'
    },
    'hasStatus' : {
      '@id' : 'http://ontology.tno.nl/logiserv#hasStatus'
    },
    'inXSDDateTime' : {
      '@id' : 'http://www.w3.org/2006/time#inXSDDateTime',
      '@type' : 'http://www.w3.org/2001/XMLSchema#dateTime'
    },   
    'hasLocation' : {
      '@id' : 'http://ontology.tno.nl/logico#hasLocation',
      '@type' : '@id'
    },
    'hasTime' : {
      '@id' : 'http://ontology.tno.nl/logiserv#hasTime',
      '@type' : '@id'
    },
    'hasTransportHandlingUnit' : {
      '@id' : 'http://ontology.tno.nl/transport#hasTransportHandlingUnit',
      '@type' : '@id'
    },
    'isComponentOf' : {
      '@id' : 'http://www.ontologydesignpatterns.org/ont/dul/DUL.owl#isComponentOf',
      '@type' : '@id'
    },
    'contains' : {
      '@id' : 'http://ontology.tno.nl/logico#contains',
      '@type' : '@id'
    },
    'hasID' : {
      '@id' : 'http://ontology.tno.nl/logico#hasID',
      '@type' : '@id'
    },
    'isDangerous' : {
      '@id' : 'http://ontology.tno.nl/logico#isDangerous',
      '@type' : 'http://www.w3.org/2001/XMLSchema#boolean'
    },
    'lat' : {
      '@id' : 'http://www.w3.org/2003/01/geo/wgs84_pos#lat',
      '@type' : 'http://www.w3.org/2001/XMLSchema#decimal'
    },
    'long' : {
      '@id' : 'http://www.w3.org/2003/01/geo/wgs84_pos#long',
      '@type' : 'http://www.w3.org/2001/XMLSchema#decimal'
    },
    'hasIDValue' : {
      '@id' : 'http://ontology.tno.nl/logico#hasIDValue'
    },
    'LogiServ' : 'http://ontology.tno.nl/logiserv#',
    'LogiCO' : 'http://ontology.tno.nl/logico#',
    'LogiTrans' : 'http://ontology.tno.nl/transport#',
    'LogisticsInst' : 'http://ontology.tno.nl/transport/instances/#',
    'owl' : 'http://www.w3.org/2002/07/owl#',
    'xsd' : 'http://www.w3.org/2001/XMLSchema#',
    'skos' : 'http://www.w3.org/2004/02/skos/core#',
    'rdfs' : 'http://www.w3.org/2000/01/rdf-schema#',
    'geo' : 'http://www.w3.org/2003/01/geo/wgs84_pos#',
    'dct' : 'http://purl.org/dc/terms/',
    'rdf' : 'http://www.w3.org/1999/02/22-rdf-syntax-ns#',
    'xml' : 'http://www.w3.org/XML/1998/namespace',
    'dcterms' : 'http://purl.org/dc/terms/',
    'dul' : 'http://www.ontologydesignpatterns.org/ont/dul/DUL.owl#',
    'time' : 'http://www.w3.org/2006/time#',
    'dc' : 'http://purl.org/dc/elements/1.1/'
  }
            ";

            contextJO = JObject.Parse(context);

            return contextJO;
        }

        private Transport TranslateTransport(Trip currentTrip, TripPoint currentPoint)
        {
            Transport result = new EWS.Logistics.LogiServ.Transport(currentTrip);

            return result;
        }

        #endregion

    }
}
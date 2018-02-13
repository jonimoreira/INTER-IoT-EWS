//NOTE: This is only provided as an example and has not been tested extensively
using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Bluetooth;
using Java.Util;
using ShimmerAPI;
using System.Collections.Generic;
using JsonLD.Core;
using Newtonsoft.Json.Linq;
using JsonLD.Util;
using Newtonsoft.Json;
using ShimmerCaptureXamarin.SAREF4health;
using PerpetualEngine.Storage;
using System.IO;

namespace ShimmerCaptureXamarin
{
    
    [Activity(Label = "ShimmerCaptureXamarin", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        ShimmerLogAndStreamXamarin shimmer;
        TextView tvShimmerState;
        TextView tvAccelX;
        TextView tvTimestamp;
        TextView tvECG;
        protected override void OnCreate(Bundle bundle)
        {
            //string test = FormatJSONLD(null);
            // Formatar a mensagem na mao mesmo... 
            //return;

            base.OnCreate(bundle);

            //SimpleStorage.SetContext(ApplicationContext);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);


            tvShimmerState = FindViewById<TextView>(Resource.Id.textViewShimmerState);
            tvAccelX = FindViewById<TextView>(Resource.Id.textViewAccelX);
            //textViewTimestamp
            tvTimestamp = FindViewById<TextView>(Resource.Id.textViewTimestamp);
            tvECG = FindViewById<TextView>(Resource.Id.textViewECG);

            // Get our button from the layout resource,
            // and attach an event to it
            Button buttonStart = FindViewById<Button>(Resource.Id.buttonStart);

            buttonStart.Click += delegate { shimmer.StartStreaming(); };

            // Get our button from the layout resource,
            // and attach an event to it
            Button buttonStop = FindViewById<Button>(Resource.Id.buttonStop);

            buttonStop.Click += delegate { shimmer.StopStreaming(); };

            // Get our button from the layout resource,
            // and attach an event to it
            Button buttonConnect = FindViewById<Button>(Resource.Id.buttonConnect);

            buttonConnect.Click += delegate {
                //int enabledSensors = ((int)ShimmerBluetooth.SensorBitmapShimmer3.SENSOR_A_ACCEL); // this is to enable Analog Accel also known as low noise accelerometer
                int enabledSensors = ((int)ShimmerBluetooth.SensorBitmapShimmer3.SENSOR_A_ACCEL | (int)ShimmerBluetooth.SensorBitmapShimmer3.SENSOR_EXG1_24BIT 
                    | (int)ShimmerBluetooth.SensorBitmapShimmer3.SENSOR_EXG2_24BIT | (int)ShimmerBluetooth.ChannelContentsShimmer3.Temperature
                    | (int)ShimmerBluetooth.SensorBitmapShimmer3.SENSOR_EXG1_16BIT | (int)ShimmerBluetooth.SensorBitmapShimmer3.SENSOR_EXG2_16BIT); 
                int accelRange = 0; // ((int)ShimmerBluetooth.SENSITIVITY_MATRIX_WIDE_RANGE_ACCEL_8G_SHIMMER3);
                int gsrRange = ((int)ShimmerBluetooth.SensorBitmapShimmer3.SENSOR_GSR);
                double samplingRate = 256.0; // 40.0; // 512.0;
                //byte[] defaultECGReg1 = new byte[10] { 0x00, 0xA0, 0x10, 0x40, 0x40, 0x2D, 0x00, 0x00, 0x02, 0x03 }; //see ShimmerBluetooth.SHIMMER3_DEFAULT_ECG_REG1
                //byte[] defaultECGReg2 = new byte[10] { 0x00, 0xA0, 0x10, 0x40, 0x47, 0x00, 0x00, 0x00, 0x02, 0x01 }; //see ShimmerBluetooth.SHIMMER3_DEFAULT_ECG_REG2
                byte[] defaultECGReg1 = ShimmerBluetooth.SHIMMER3_DEFAULT_TEST_REG1; //also see ShimmerBluetooth.SHIMMER3_DEFAULT_ECG_REG1
                byte[] defaultECGReg2 = ShimmerBluetooth.SHIMMER3_DEFAULT_TEST_REG2; //also see ShimmerBluetooth.SHIMMER3_DEFAULT_ECG_REG2

                //shimmer.GetEnabledSensors()
                string bluetoothAddress = "00:06:66:88:db:ca".ToUpper();
                shimmer = new ShimmerLogAndStreamXamarin("ShimmerXamarin", bluetoothAddress);
                //shimmer = new ShimmerLogAndStreamXamarin("ShimmerXamarin", bluetoothAddress, samplingRate, accelRange, gsrRange, enabledSensors, false, false, false, 0, 0, defaultECGReg1, defaultECGReg2, false);
                //shimmer = new ShimmerLogAndStreamXamarin("ShimmerXamarin", "00:06:66:79:E4:54", 1, 0, 4, enabledSensors, false, false, false, 0, 0, defaultECGReg1, defaultECGReg2, false);
                shimmer.UICallback += this.HandleEvent;
                shimmer.StartConnectThread();
            };

            // Get our button from the layout resource,
            // and attach an event to it
            Button buttonDisconnect = FindViewById<Button>(Resource.Id.buttonDisconnect);

            buttonDisconnect.Click += delegate {
                shimmer.Disconnect();
            };

            StartTimer();
        }
        
        public void HandleEvent(object sender, EventArgs args)
        {
            CustomEventArgs eventArgs = (CustomEventArgs)args;
            int indicator = eventArgs.getIndicator();

            switch (indicator)
            {
                case (int)ShimmerBluetooth.ShimmerIdentifier.MSG_IDENTIFIER_STATE_CHANGE:
                    System.Diagnostics.Debug.Write(((ShimmerBluetooth)sender).GetDeviceName() + " State = " + ((ShimmerBluetooth)sender).GetStateString() + System.Environment.NewLine);
                    int state = (int)eventArgs.getObject();
                    if (state == (int)ShimmerBluetooth.SHIMMER_STATE_CONNECTED)
                    {
                        System.Diagnostics.Debug.Write("Connected");
                        RunOnUiThread(() => tvShimmerState.Text = "Shimmer State: Connected");
                    }
                    else if (state == (int)ShimmerBluetooth.SHIMMER_STATE_CONNECTING)
                    {
                        System.Diagnostics.Debug.Write("Connecting");
                        RunOnUiThread(() => tvShimmerState.Text = "Shimmer State: Connecting");
                    }
                    else if (state == (int)ShimmerBluetooth.SHIMMER_STATE_NONE)
                    {
                        System.Diagnostics.Debug.Write("Disconnected");
                        RunOnUiThread(() => tvShimmerState.Text = "Shimmer State: Disconnected");
                    }
                    else if (state == (int)ShimmerBluetooth.SHIMMER_STATE_STREAMING)
                    {
                        System.Diagnostics.Debug.Write("Streaming");
                        RunOnUiThread(() => tvShimmerState.Text = "Shimmer State: Streaming");
                    }
                    break;
                case (int)ShimmerBluetooth.ShimmerIdentifier.MSG_IDENTIFIER_DATA_PACKET:
                    ObjectCluster objectCluster = new ObjectCluster((ObjectCluster)eventArgs.getObject());
                    List<Double> data = objectCluster.GetData();
                    List<String> dataNames = objectCluster.GetNames();
                    List<String> dataUnits = objectCluster.GetUnits();
                    String result="";
                    String resultNames = "";
                    String resultUnits = "";
                    foreach (Double d in data)
                    {
                        result += d.ToString() + "||";
                    }
                    foreach (String u in dataUnits)
                    {
                        resultUnits += u + "||";
                    }
                    foreach (String s in dataNames)
                    {
                        resultNames = s + "||" + resultNames;
                    }
                    //System.Console.WriteLine(resultNames);
                    //System.Console.WriteLine(resultUnits);
                    //System.Console.WriteLine(result);

                    SaveData(objectCluster);

                    SensorData dataAccelX = objectCluster.GetData(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_X, "CAL");
                    SensorData dataTimestamp = objectCluster.GetData(dataNames.IndexOf("System Timestamp"));
                    SensorData dataECG_LL_RA = objectCluster.GetData(Shimmer3Configuration.SignalNames.ECG_LL_RA, "CAL");
                    SensorData dataECG_LA_RA = objectCluster.GetData(Shimmer3Configuration.SignalNames.ECG_LA_RA, "CAL");
                    SensorData dataECG_VX_RL = objectCluster.GetData(Shimmer3Configuration.SignalNames.ECG_VX_RL, "CAL");

                    //data[dataNames.IndexOf("System Timestamp")]
                    //SensorData dataAccelY = objectCluster.GetData(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_Y, "CAL");
                    //SensorData dataAccelZ = objectCluster.GetData(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_Z, "CAL");
                    //SensorData dataECG
                    RunOnUiThread(() => tvAccelX.Text = "AccelX: " + dataAccelX.Data); 
                    RunOnUiThread(() => tvTimestamp.Text = "Timestamp: " + dataTimestamp.Data); 
                    RunOnUiThread(() => tvECG.Text = "ECG LL_RA: " + dataECG_LL_RA.Data + " | LA_RA: " + dataECG_LA_RA.Data + " | VX_RL: " + dataECG_VX_RL.Data);
                    break;
                    /*
                case (int)ShimmerBluetooth.ShimmerIdentifier.MSG_IDENTIFIER_PACKET_RECEPTION_RATE:
                    ObjectCluster objectCluster1 = new ObjectCluster((ObjectCluster)eventArgs.getObject());
                    List<Double> data1 = objectCluster1.GetData();
                    List<String> dataNames1 = objectCluster1.GetNames();
                    String result1 = "";
                    String resultNames1 = "";
                    foreach (Double d in data1)
                    {
                        result1 = d.ToString() + " " + result1;
                    }
                    foreach (String s in dataNames1)
                    {
                        resultNames1 = s + " " + resultNames1;
                    }
                    System.Console.WriteLine(resultNames1);
                    System.Console.WriteLine(result1);
                    break;*/
            }

        }

        System.Timers.Timer timer = new System.Timers.Timer();
        
        public void StartTimer()
        {
            timer.Interval = 5000;
            timer.Elapsed += OnTimedEvent;
            timer.Enabled = true;
            
        }

        //private string storageGroupNameECGleads = "listTimeseriesECG";

        private void OnTimedEvent(object sender, System.Timers.ElapsedEventArgs e)
        {
            System.Console.WriteLine("OnTimedEvent: " + DateTime.Now.ToLongTimeString());
            //string idDevice = Android.OS.Build.Serial;

            // Create JSON-LD message of device measurements
            // Generate Context and Graph1
            JObject contextJSON = GetContextJSON();
            JObject graph1JSON = GetGraph1JSON();

            // Generate ECGMeasurementsSeries
            ECGMeasurementsSeries measurementSeriesLA_RA = CreateMessageAndDeleteListLead1_ECG_LA_RA();
            ECGMeasurementsSeries measurementSeriesLL_RA = CreateMessageAndDeleteListLead2_ECG_LL_RA();
            ECGMeasurementsSeries measurementSeriesLL_LA = CreateMessageAndDeleteListLead3_ECG_LL_LA();
            ECGMeasurementsSeries measurementSeriesVx_RL = CreateMessageAndDeleteListUnipolarLeadVx_RL();

            // Generate ECGLeadBipolarLimb (a ECGSensor)
            JObject sensor_Lead1_ECG_LA_RA = GetECGSensorJSON(measurementSeriesLA_RA, "saref4health:ECGLeadBipolarLimb", "sarefInst:ECGLead_I_code131329", "Lead I (LA-RA)");
            JObject sensor_Lead2_ECG_LL_RA = GetECGSensorJSON(measurementSeriesLL_RA, "saref4health:ECGLeadBipolarLimb", "sarefInst:ECGLead_II_code131330", "Lead II (LL-RA)");
            JObject sensor_Lead3_ECG_LL_LA = GetECGSensorJSON(measurementSeriesLL_LA, "saref4health:ECGLeadBipolarLimb", "sarefInst:ECGLead_III_code131389", "Lead III (LL-LA)");

            // Generate ECGLeadUnipolar (a ECGSensor)
            JObject sensor_LeadVx_RL = GetECGSensorJSON(measurementSeriesVx_RL, "saref4health:ECGLeadUnipolar", "sarefInst:ECGLead_Vx_RL_code131389", "Lead Vx-RL");

            // Generate ECGSensor "container" -> Includes the 4 ECGSensor above (leads) -> ontologically correct would be at least 3 ECGLeadBipolarLimb and 1 ECGLeadUnipolar
            JObject sensor_container = GetECGSensorContainerJSON(sensor_Lead1_ECG_LA_RA, sensor_Lead2_ECG_LL_RA, sensor_Lead3_ECG_LL_LA, sensor_LeadVx_RL);

            // Generate accelerometer measurement (last received?)

            // Generate ECGDevice message (Device composed at least of an ECGSensor ("container"))
            List<JObject> listSensorsOfDevice = new List<JObject>();
            listSensorsOfDevice.Add(sensor_container);
            JObject eCGDeviceJSON = GetECGDeviceJSON(listSensorsOfDevice);

            System.Console.WriteLine(eCGDeviceJSON);

            // Save as file

            try
            {
                string pathToNewFolder = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;// + "/FolderName";
                Console.Write(pathToNewFolder);
                string path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
                Console.Write(path);

                string filename = Path.Combine(path, @"Shimmer3ECG_" + DateTime.Now.ToString("yyyyMMddTHHmmss") + ".json");
                string filenameNewFolder = Path.Combine(pathToNewFolder, @"Shimmer3ECG_" + DateTime.Now.ToString("yyyyMMddTHHmmss") + ".json");

                using (var streamWriter = new StreamWriter(filename, true))
                {
                    //streamWriter.WriteLine(DateTime.UtcNow);
                    streamWriter.Write(eCGDeviceJSON.ToString(Formatting.Indented));
                }

                File.WriteAllText(filename + "a", "Teste escrevendo msg...");
                
                using (var streamWriter = new StreamWriter(filenameNewFolder, true))
                {
                    //streamWriter.WriteLine(DateTime.UtcNow);
                    string result = eCGDeviceJSON.ToString(Formatting.Indented).Replace("\"geo_", "\"geo:").Replace("\"InterIoTMsg_", "\"InterIoTMsg:").Replace("\"saref_", "\"saref:");
                    result = result.Replace("\"id\"", "\"@id\"").Replace("\"label\"", "\"@label\"").Replace("\"graph\"", "\"@graph\"").Replace("\"context\"", "\"@context\"").Replace("\"language\"", "\"@language\"").Replace("\"type\"", "\"@type\"").Replace("\"value\"", "\"@value\"");

                    streamWriter.Write(result);
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("ERROR SAVING FILE!! " + ex.Message);
            }

    /*
    string path = @"D:\Projects\InterIOT\Workplan\WP1-Ontology translations and rules\data\SAREF4health_ShimmerCaptureXamarin\Shimmer3ECG_" + DateTime.Now.ToString("yyyyMMddTHHmmss") + ".json";
    try
    {
        File.WriteAllText(path, eCGDeviceJSON.ToString(Formatting.Indented));
        Console.WriteLine("Saved in file: " + path);
    }
    catch (Exception ex)
    {
        System.Console.WriteLine("ERROR SAVING FILE!! " + ex.Message);
    }

    /*
    using (StreamWriter file = File.CreateText(@"D:\Projects\InterIOT\Workplan\WP1-Ontology translations and rules\src\IPSM\Shimmer3ECG_" + DateTime.Now.ToLongTimeString() + ".json"))
    {
        using (JsonTextWriter writer = new JsonTextWriter(file))
        {
            writer.Formatting = Formatting.Indented;
            eCGDeviceJSON.WriteTo(writer);
            writer.WriteEnd();
            writer.WriteEndObject();
        }
    }


    // serialize JSON directly to a file
    using (StreamWriter file = File.CreateText(@"D:\Projects\InterIOT\Workplan\WP1-Ontology translations and rules\src\IPSM\Shimmer3ECG_" + DateTime.Now.ToLongTimeString() + ".json"))
    {
        JsonSerializer serializer = new JsonSerializer();
        serializer.Serialize(file, eCGDeviceJSON);
        //file.Close();
    }
    */


    /*
    storage = GetStorage();

    lock (storage)
    {
        var values = storage.Get<List<KeyValuePair<double, double>>>(Shimmer3Configuration.SignalNames.ECG_LL_RA);
        System.Console.WriteLine("values.Count=" + values.Count);

        ECGMeasurementsSeries measurementSeriesLL_RA = TranslateECGMeasurementsSeries(values);
        System.Console.WriteLine(measurementSeriesLL_RA.JSONLDobject);

        // Publish in a broker (UniversAAL): create thread

        // Delete values from memory (empty list)
        storage.Delete(Shimmer3Configuration.SignalNames.ECG_LL_RA);
    }
    */

}

        private void SaveData(ObjectCluster objectCluster)
        {
            SaveMeasurementsAsJSONLD(objectCluster);

            /*
            // Format data with JSON-LD -> SAREF:Measurements
            // TODO: GetMeasurementItem only (on save) -> (on timer) create header (graphs), idea - multiple measurements 1 header per message to the cloud (timer)
            string data = string.Empty; // FormatJSONLD(objectCluster);
            
            List<JObject> sarefMakesMeasurementItemList = GetSarefMakesMeasurementItemsJSON(objectCluster);
            foreach (JObject jObject in sarefMakesMeasurementItemList)
            {
                data += jObject.ToString() + ", /n";
            }
            string idDevice = Android.OS.Build.Serial;
            string timeStamp = string.Empty;
            string idMeasurements = idDevice + "_" + timeStamp;

            // Save data in memory (queue? cache? session?)

            var listMeasurements = Application.Context.GetSharedPreferences("listMeasurements", FileCreationMode.Private);
            var elem = listMeasurements.Edit();
            // TODO: Unique ID: InterIoTMsg:messageID (???)
            //   or ID device + "_" + TimeStamp?
            //  
            elem.PutString(idMeasurements, data);
            elem.Commit();

            */
        }

        private JObject GetContextJSON()
        {
            JObject contextJSON = JObject.FromObject(new
            {
                /*
    --> The most correct is that the JSON-LD C# lib does that...
                 
    "comment" : {
      "@id" : "http://www.w3.org/2000/01/rdf-schema#comment"
    },
    "label" : {
      "@id" : "http://www.w3.org/2000/01/rdf-schema#label"
    },
    "seeAlso" : {
      "@id" : "http://www.w3.org/2000/01/rdf-schema#seeAlso"
    },
    "consistsOf" : {
      "@id" : "https://w3id.org/saref#consistsOf",
      "@type" : "@id"
    },
    "hasManufacturer" : {
      "@id" : "https://w3id.org/saref#hasManufacturer"
    },
                 */

                InterIoTMsg = "http://inter-iot.eu/message/",
                InterIoT = "http://inter-iot.eu/",
                sarefInst = "https://w3id.org/saref/instances/",
                schema = "http://schema.org/",
                qu = "http://purl.oclc.org/NET/ssnx/qu/qu#",
                owl = "http://www.w3.org/2002/07/owl#",
                m3 = "http://purl.org/iot/vocab/m3-lite#",
                saref = "https://w3id.org/saref#",
                xsd = "http://www.w3.org/2001/XMLSchema#",
                skos = "http://www.w3.org/2004/02/skos/core#",
                dim = "http://purl.oclc.org/NET/ssnx/qu/dim#",
                rdfs = "http://www.w3.org/2000/01/rdf-schema#",
                saref4envi = "https://w3id.org/def/saref4envi#",
                dct = "http://purl.org/dc/terms/",
                rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#",
                xml = "http://www.w3.org/XML/1998/namespace",
                dcterms = "http://purl.org/dc/terms/",
                time = "http://www.w3.org/2006/time#",
                foaf = "http://xmlns.com/foaf/0.1/",
                om = "http://www.wurvoc.org/vocabularies/om-1.8/",
                geo = "http://www.w3.org/2003/01/geo/wgs84_pos#", //World Geodetic System (WGS84) - GIS Geography
                dc = "http://purl.org/dc/elements/1.1/"
            });
            return contextJSON;
        }

        private JObject GetGraph1JSON()
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

        private JObject GetGeoLocationJSON()
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

        private string TranslateIsMeasuredIn(SensorData sensorData)
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
                default:
                    // missing: kPa||Celcius* (pressure and temperature)
                    break;
            }

            //Console.WriteLine("sensorData.ToString()=" + sensorData.ToString());

            return result;
        }
        
        private string TranslateRelatesToProperty(string signalName)
        {
            string result = "saref:SpeedUnit_MeterPerSecond";

            if (signalName == Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_X)
                result = "sarefInst:Acceleration_Vehicle";
            else if (signalName == Shimmer3Configuration.SignalNames.ECG_LL_RA
                        || signalName == Shimmer3Configuration.SignalNames.ECG_LA_RA
                        || signalName == Shimmer3Configuration.SignalNames.ECG_VX_RL)
                result = "sarefInst:HeartElectricalActivity_Person";
            
            return result;
        }

        
        private string TranslateMeasurementType(string signalName)
        {
            string result = "saref:SpeedMeasurement";

            if (signalName == Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_X)
                result = "saref:AccelerationMeasurement";
            else if (signalName == Shimmer3Configuration.SignalNames.ECG_LL_RA
                        || signalName == Shimmer3Configuration.SignalNames.ECG_LA_RA
                        || signalName == Shimmer3Configuration.SignalNames.ECG_VX_RL)
                result = "saref4health:ECGMeasurement";

            return result;
        }

        private List<JObject> SaveMeasurementsAsJSONLD(ObjectCluster objectCluster)
        {
            List<JObject> sarefMakesMeasurementItemList = new List<JObject>();
            List<Double> data = objectCluster.GetData();
            List<String> dataNames = objectCluster.GetNames();
            List<String> dataUnits = objectCluster.GetUnits();

            SensorData dataTimestamp = objectCluster.GetData(dataNames.IndexOf("System Timestamp"));
            
            SensorData dataECG_LL_RA = objectCluster.GetData(Shimmer3Configuration.SignalNames.ECG_LL_RA, "CAL");
            SensorData dataECG_LA_RA = objectCluster.GetData(Shimmer3Configuration.SignalNames.ECG_LA_RA, "CAL");
            SensorData dataECG_VX_RL = objectCluster.GetData(Shimmer3Configuration.SignalNames.ECG_VX_RL, "CAL");

            CompactECGData(dataTimestamp, dataECG_LL_RA, dataECG_LA_RA, dataECG_VX_RL);

            SensorData dataAccelX = objectCluster.GetData(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_X, "CAL");
            Measurement msgAccelX = TranslateMeasurement(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_X, dataAccelX, dataTimestamp.Data);


            // TODO: Format saref4health:ECGMeasurementsSeries
            //Measurement msgECG_LL_RA = TranslateMeasurement(Shimmer3Configuration.SignalNames.ECG_LL_RA, dataECG_LL_RA, dataTimestamp.Data);
            //Measurement msgECG_LA_RA = TranslateMeasurement(Shimmer3Configuration.SignalNames.ECG_LA_RA, dataECG_LA_RA, dataTimestamp.Data);
            //Measurement msgECG_VX_RL = TranslateMeasurement(Shimmer3Configuration.SignalNames.ECG_VX_RL, dataECG_VX_RL, dataTimestamp.Data);

            sarefMakesMeasurementItemList.Add(msgAccelX.JSONLDobject);
            //sarefMakesMeasurementItemList.Add(msgECG_LL_RA.JSONLDobject);
            //sarefMakesMeasurementItemList.Add(msgECG_LA_RA.JSONLDobject);
            //sarefMakesMeasurementItemList.Add(msgECG_VX_RL.JSONLDobject);
            
            return sarefMakesMeasurementItemList;
        }

        private string GeneratePID(Measurement msg)
        {
            string result = "sarefInst:SpeedMeasurement__Test.1.1_1511466006.9682777"; // string.Empty;

            result = msg.Type + "_Test.1.1_" + msg.HasTimestamp;

            return result;
        }

        private string GeneratePID(ECGMeasurementsSeries msg)
        {
            string result = "sarefInst:SpeedMeasurement__Test.1.1_1511466006.9682777"; // string.Empty;

            result = msg.Type + "_Test.1.1_" + msg.HasTimestamp;

            return result;
        }

        private string GeneratePID(JsonObjectAttribute msg)
        {
            string result = "sarefInst:Message_Test.1.1_1511466006.9682777"; 

            //result = msg.Type + "_Test.1.1_" + msg.HasTimestamp;

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

        private Measurement TranslateMeasurement(string signalName, SensorData sensorData, double timestamp)
        {
            Measurement msg = new Measurement();
            msg.HasTimestamp = timestamp;
            msg.IsMeasuredIn = TranslateIsMeasuredIn(sensorData); //"saref:SpeedUnit_MeterPerSecond";
            msg.RelatesToProperty = TranslateRelatesToProperty(signalName);
            msg.Type = TranslateMeasurementType(signalName); //"saref:SpeedMeasurement";

            msg.Label = "Measurement of Shimmer 3 ECG [" + signalName + "]_" + timestamp;
            msg.HasValue = sensorData.Data;

            msg.Id = GeneratePID(msg);

            JObject sarefMakesMeasurementItemJSON = JObject.FromObject(new
            {
                @id = msg.Id,
                @type = msg.Type,
                @label = msg.Label,
                saref_hasTimestamp = msg.HasTimestamp,
                saref_hasValue = msg.HasValue,
                saref_isMeasuredIn = msg.IsMeasuredIn,
                saref_relatesToProperty = msg.RelatesToProperty
            });

            msg.JSONLDobject = sarefMakesMeasurementItemJSON;
            
            return msg;
        }
        
        private ECGMeasurementsSeries TranslateECGMeasurementsSeries(List<KeyValuePair<double, double>> values)
        {
            //System.Console.WriteLine("[TranslateECGMeasurementsSeries] values.Count=" + values.Count);
            if (values.Count == 0)
                return null;

            double timestamp = values[0].Key;
            ECGMeasurementsSeries msg = new ECGMeasurementsSeries();
            msg.HasTimestamp = timestamp;
            msg.IsMeasuredIn = "sarefInst:ElectricPotential_MilliVolts"; // TranslateIsMeasuredIn(sensorData); //"saref:SpeedUnit_MeterPerSecond";
            msg.RelatesToProperty = "sarefInst:HeartElectricalActivity_Person"; // TranslateRelatesToProperty(signalName);
            msg.Type = "saref4health:ECGMeasurementsSeries"; //TranslateMeasurementType(signalName); //"saref:SpeedMeasurement";

            msg.Label = "ECG measurements series - Lead XPTO at " + timestamp;
            
            List<double> seriesValues = new List<double>();
            int i = 0;
            foreach (KeyValuePair<double, double> measurement in values)
            {
                //Needs improvement: copy array / matrix functions
                seriesValues.Add(measurement.Value);
                //System.Console.WriteLine(i+"[" + measurement.Key + "] " + measurement.Value);
                //i++;
            }
            msg.HasValue = seriesValues;
            
            msg.Id = GeneratePID(msg);

            JObject sarefMakesMeasurementItemJSON = JObject.FromObject(new
            {
                @id = msg.Id,
                @type = msg.Type,
                @label = msg.Label,
                saref_hasTimestamp = msg.HasTimestamp,
                saref_hasValue = msg.HasValue,
                saref_isMeasuredIn = msg.IsMeasuredIn,
                saref_relatesToProperty = msg.RelatesToProperty
            });

            msg.JSONLDobject = sarefMakesMeasurementItemJSON;
            
            return msg;
        }

        private void CompactECGData(SensorData dataTimestamp, SensorData dataECG_LL_RA, SensorData dataECG_LA_RA, SensorData dataECG_VX_RL)
        {
            /*
            KeyValuePair<double, double> timeseriesECG_LL_RA = new KeyValuePair<double, double>(dataTimestamp.Data, dataECG_LL_RA.Data);
            var storage = SimpleStorage.EditGroup("listTimeseriesECG");
            var value = storage.Get<List<KeyValuePair<double,double>>>("ECG_LL_RA");
            if (value == null)
            {
                value = new List<KeyValuePair<double, double>>();
            }

            value.Add(timeseriesECG_LL_RA);

            storage.Put<List<KeyValuePair<double, double>>>("ECG_LL_RA", value);
            */

            StoreECGleadValue(Shimmer3Configuration.SignalNames.ECG_LA_RA, dataTimestamp.Data, dataECG_LA_RA.Data);
            StoreECGleadValue(Shimmer3Configuration.SignalNames.ECG_LL_RA, dataTimestamp.Data, dataECG_LL_RA.Data);
            
            // Derived LL-LA
            StoreECGleadValue(ECG_LL_LA, dataTimestamp.Data, dataECG_LA_RA.Data - dataECG_LL_RA.Data);

            StoreECGleadValue(Shimmer3Configuration.SignalNames.ECG_VX_RL, dataTimestamp.Data, dataECG_VX_RL.Data);

        }

        /*
        private SimpleStorage storage;
        private SimpleStorage GetStorage()
        {
            if (storage == null)
                storage = SimpleStorage.EditGroup(storageGroupNameECGleads);
            return storage;
        }
        */

        private List<KeyValuePair<double, double>> valuesLead1_ECG_LA_RA;
        private List<KeyValuePair<double, double>> GetValuesLead1_ECG_LA_RA()
        {
            if (valuesLead1_ECG_LA_RA == null)
                valuesLead1_ECG_LA_RA = new List<KeyValuePair<double, double>>();
            return valuesLead1_ECG_LA_RA;
        }

        private List<KeyValuePair<double, double>> valuesLead2_ECG_LL_RA;
        private List<KeyValuePair<double, double>> GetValuesLead2_ECG_LL_RA()
        {
            if (valuesLead2_ECG_LL_RA == null)
                valuesLead2_ECG_LL_RA = new List<KeyValuePair<double, double>>();
            return valuesLead2_ECG_LL_RA;
        }

        private List<KeyValuePair<double, double>> valuesLead3_ECG_LL_LA;
        private List<KeyValuePair<double, double>> GetValuesLead3_ECG_LL_LA()
        {
            if (valuesLead3_ECG_LL_LA == null)
                valuesLead3_ECG_LL_LA = new List<KeyValuePair<double, double>>();
            return valuesLead3_ECG_LL_LA;
        }
        
        private List<KeyValuePair<double, double>> valuesUnipolarLeadVx_RL;
        private List<KeyValuePair<double, double>> GetValuesUnipolarLeadVx_RL()
        {
            if (valuesUnipolarLeadVx_RL == null)
                valuesUnipolarLeadVx_RL = new List<KeyValuePair<double, double>>();
            return valuesUnipolarLeadVx_RL;
        }

        private const string ECG_LL_LA = "LL_LA";

        private void StoreECGleadValue(string leadName, double timestamp, double leadValue)
        {
            KeyValuePair<double, double> timeseriesECGlead = new KeyValuePair<double, double>(timestamp, leadValue);

            if (leadName == Shimmer3Configuration.SignalNames.ECG_LA_RA)
            {
                valuesLead1_ECG_LA_RA = GetValuesLead1_ECG_LA_RA();
                lock (valuesLead1_ECG_LA_RA)
                {
                    valuesLead1_ECG_LA_RA.Add(timeseriesECGlead);
                }
            }
            else if (leadName == Shimmer3Configuration.SignalNames.ECG_LL_RA)
            {
                valuesLead2_ECG_LL_RA = GetValuesLead2_ECG_LL_RA();
                lock (valuesLead2_ECG_LL_RA)
                {
                    valuesLead2_ECG_LL_RA.Add(timeseriesECGlead);
                }
            }
            else if (leadName == ECG_LL_LA)
            {
                valuesLead3_ECG_LL_LA = GetValuesLead3_ECG_LL_LA();
                lock (valuesLead3_ECG_LL_LA)
                {
                    valuesLead3_ECG_LL_LA.Add(timeseriesECGlead);
                }
            }
            else if (leadName == Shimmer3Configuration.SignalNames.ECG_VX_RL)
            {
                valuesUnipolarLeadVx_RL = GetValuesUnipolarLeadVx_RL();
                lock (valuesUnipolarLeadVx_RL)
                {
                    valuesUnipolarLeadVx_RL.Add(timeseriesECGlead);
                }
            }


            /*
            storage = GetStorage();            
            lock (storage)
            {
                var value = storage.Get<List<KeyValuePair<double, double>>>(leadName);
                if (value == null)
                    value = new List<KeyValuePair<double, double>>();
                System.Console.WriteLine("[StoreECGleadValue] value.Count=" + value.Count);
                value.Add(timeseriesECGlead);
                storage.Put<List<KeyValuePair<double, double>>>(leadName, value);
            }
            */
        }

        private ECGMeasurementsSeries CreateMessageAndDeleteListLead1_ECG_LA_RA()
        {
            ECGMeasurementsSeries measurementSeriesLA_RA;
            valuesLead1_ECG_LA_RA = GetValuesLead1_ECG_LA_RA();
            lock (valuesLead1_ECG_LA_RA)
            {
                System.Console.WriteLine("valuesLead1_ECG_LA_RA.Count=" + valuesLead1_ECG_LA_RA.Count);

                measurementSeriesLA_RA = TranslateECGMeasurementsSeries(valuesLead1_ECG_LA_RA);
                //System.Console.WriteLine(measurementSeriesLA_RA.JSONLDobject);
                
                // Delete values from memory (empty list)
                valuesLead1_ECG_LA_RA.Clear();
            }
            return measurementSeriesLA_RA;
        }

        private ECGMeasurementsSeries CreateMessageAndDeleteListLead2_ECG_LL_RA()
        {
            ECGMeasurementsSeries measurementSeriesLL_RA;
            valuesLead2_ECG_LL_RA = GetValuesLead2_ECG_LL_RA();
            lock (valuesLead2_ECG_LL_RA)
            {
                System.Console.WriteLine("valuesLead2_ECG_LL_RA.Count=" + valuesLead2_ECG_LL_RA.Count);

                measurementSeriesLL_RA = TranslateECGMeasurementsSeries(valuesLead2_ECG_LL_RA);
                //System.Console.WriteLine(measurementSeriesLL_RA.JSONLDobject);
                
                // Delete values from memory (empty list)
                valuesLead2_ECG_LL_RA.Clear();
            }
            return measurementSeriesLL_RA;
        }

        private ECGMeasurementsSeries CreateMessageAndDeleteListLead3_ECG_LL_LA()
        {
            ECGMeasurementsSeries measurementSeriesLL_LA;
            valuesLead3_ECG_LL_LA = GetValuesLead3_ECG_LL_LA();
            lock (valuesLead3_ECG_LL_LA)
            {
                System.Console.WriteLine("valuesLead3_ECG_LL_LA.Count=" + valuesLead3_ECG_LL_LA.Count);

                measurementSeriesLL_LA = TranslateECGMeasurementsSeries(valuesLead3_ECG_LL_LA);
                //System.Console.WriteLine(measurementSeriesLL_LA.JSONLDobject);
                
                // Delete values from memory (empty list)
                valuesLead3_ECG_LL_LA.Clear();
            }
            return measurementSeriesLL_LA;
        }

        private ECGMeasurementsSeries CreateMessageAndDeleteListUnipolarLeadVx_RL()
        {
            ECGMeasurementsSeries measurementSeriesVx_RL;
            valuesUnipolarLeadVx_RL = GetValuesUnipolarLeadVx_RL();
            lock (valuesUnipolarLeadVx_RL)
            {
                System.Console.WriteLine("valuesUnipolarLeadVx_RL.Count=" + valuesUnipolarLeadVx_RL.Count);

                measurementSeriesVx_RL = TranslateECGMeasurementsSeries(valuesUnipolarLeadVx_RL);
                //System.Console.WriteLine(measurementSeriesVx_RL.JSONLDobject);
                
                // Delete values from memory (empty list)
                valuesUnipolarLeadVx_RL.Clear();
            }
            return measurementSeriesVx_RL;
        }

        private JObject GetGraph2JSON(JObject geoLocationJSON, List<JObject> sarefMakesMeasurementItemList)
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

        private JObject GetGraph2JSON(List<JObject> sarefMakesMeasurementItemList)
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

        private JObject GetECGDeviceJSON(List<JObject> listSensorsOfDevice)
        {
            JObject eCGDeviceJSON = JObject.FromObject(new
            {
                @id = "sarefInst:Shimmer3ECG_unit_T9JRN42_" + Android.OS.Build.Serial, // _[ID Shimmer ECG 3]_[Mobile ID] --> can be useful for security...
                @type = "saref4health:ECGDevice",
                @label = new
                {
                    @language = "en",
                    @value = "Shimmer3 ECG unit T9J-RN42 through smartphone (gateway) " + Android.OS.Build.Serial
                },
                rdfs_seeAlso = "http://www.shimmersensing.com/products/ecg-development-kit#specifications-tab",
                saref_hasManufacturer = "Shimmer",
                saref_consistsOf = listSensorsOfDevice
            });
            return eCGDeviceJSON;
        }

        private JObject GetECGSensorJSON(ECGMeasurementsSeries measurementSeries, string type, string id, string label)
        {
            JObject eCGSensorJSON = JObject.FromObject(new
            {
                @id = id,
                @type = type,
                @label = new
                {
                    @language = "en",
                    @value = label
                },
                saref_hasFrequencyMeasurement = "sarefInst:FrequencyOf512Hertz",
                saref_measuresProperty = "sarefInst:HeartElectricalActivity_Person",
                saref_makesMeasurement = measurementSeries.JSONLDobject
            });
            return eCGSensorJSON;
        }

        // "Container" not defined in the ontology. The idea is that this container includes the 4 leads, informing common aspects, e.g. frequency
        private JObject GetECGSensorContainerJSON(JObject sensor_Lead1_ECG_LA_RA, JObject sensor_Lead2_ECG_LL_RA, JObject sensor_Lead3_ECG_LL_LA, JObject sensor_LeadVx_RL)
        {
            List<JObject> leads = new List<JObject>();
            leads.Add(sensor_Lead1_ECG_LA_RA);
            leads.Add(sensor_Lead2_ECG_LL_RA);
            leads.Add(sensor_Lead3_ECG_LL_LA);
            leads.Add(sensor_LeadVx_RL);

            JObject eCGSensorJSON = JObject.FromObject(new
            {
                @id = "sarefInst:Shimmer3ECGSensor_T9JRN42",
                @type = "saref4health:ECGSensor",
                @label = new
                {
                    @language = "en",
                    @value = "Shimmer3 ECG sensor T9J-RN42"
                },
                rdfs_comment = "Shimmer3 ECG sensor : \"four-lead ECG solution\".",
                redfs_seeAlso = "http://www.shimmersensing.com/products/ecg-development-kit#specifications-tab",
                saref_hasFrequencyMeasurement = "sarefInst:FrequencyOf512Hertz",
                saref_measuresProperty = "sarefInst:HeartElectricalActivity_Person",
                saref_consistsOf = leads
            });
            return eCGSensorJSON;
        }
        /*
        private string FormatJSONLD(ObjectCluster objectCluster)
        {
            JObject contextJSON = GetContextJSON();            
            JObject graph1JSON = GetGraph1JSON();

            List<JObject> sarefMakesMeasurementItemList = new List<JObject>();
            List<JObject> sarefMakesMeasurementItemsJSON = GetSarefMakesMeasurementItemsJSON(objectCluster);

            //JObject geoLocationJSON = GetGeoLocationJSON();
            //JObject graph2JSON = GetGraph2JSON(geoLocationJSON, sarefMakesMeasurementItemList);
            JObject graph2JSON = GetGraph2JSON(sarefMakesMeasurementItemsJSON);

            string result = string.Empty;

            result = JSONUtils.ToPrettyString(graph2JSON);

            JObject graph1arr = JObject.FromObject(new
            {
                @graph = new[]
                {
                    graph1JSON
                }
            });

            JObject graph2arr = JObject.FromObject(new
            {
                @graph = new[]
                {
                    graph2JSON
                }
            });

            JObject resultJSON = JObject.FromObject(new
            {
                context = contextJSON,
                @graph = new []
                {
                    graph1arr,
                    graph2arr
                }
            });

            
            result = JSONUtils.ToPrettyString(resultJSON);

            result = result.Replace("\"geo_", "\"geo:").Replace("\"InterIoTMsg_", "\"InterIoTMsg:").Replace("\"saref_", "\"saref:");
            result = result.Replace("\"id\"", "\"@id\"").Replace("\"label\"", "\"@label\"").Replace("\"graph\"", "\"@graph\"").Replace("\"context\"", "\"@context\"").Replace("\"language\"", "\"@language\"").Replace("\"type\"", "\"@type\"").Replace("\"value\"", "\"@value\"");

            return result;

            /*
            string context = JSONUtils.ToPrettyString(contextJSON);
            string graph1 = JSONUtils.ToPrettyString(graph1JSON);
            string graph2 = JSONUtils.ToPrettyString(graph2JSON);
            string geoLocation = JSONUtils.ToPrettyString(geoLocationJSON);

            JsonLdOptions options = new JsonLdOptions();
            
            try
            {
                JObject jsonLDmessage = JsonLdProcessor.Compact(geoLocationJSON, contextJSON, options);
                result = JSONUtils.ToPrettyString(jsonLDmessage);
            }
            catch (Exception ex)
            { }
            */


        //}

        private string FormatJSONLD01(ObjectCluster objectCluster)
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

        private JToken GenerateContextForJSONLD()
        {
            Dictionary<string, string> dicContexts = new Dictionary<string, string>();
            dicContexts.Add("schema", "http://schema.org/");
            dicContexts.Add("owl", "http://www.w3.org/2002/07/owl#");

            JToken result = JsonConvert.SerializeObject(dicContexts);
            return result;

            /*

  "@context":{
    "InterIoTMsg":"http://inter-iot.eu/message/",
    "InterIoT":"http://inter-iot.eu/",
    "sarefInst" : "https://w3id.org/saref/instances/",
    "schema":"http://schema.org/",
    "qu":"http://purl.oclc.org/NET/ssnx/qu/qu#",
    "owl":"http://www.w3.org/2002/07/owl#",
    "saref":"https://w3id.org/saref#",
    "xsd":"http://www.w3.org/2001/XMLSchema#",
    "skos":"http://www.w3.org/2004/02/skos/core#",
    "dim":"http://purl.oclc.org/NET/ssnx/qu/dim#",
    "rdfs":"http://www.w3.org/2000/01/rdf-schema#",
    "dct":"http://purl.org/dc/terms/",
    "rdf":"http://www.w3.org/1999/02/22-rdf-syntax-ns#",
    "xml":"http://www.w3.org/XML/1998/namespace",
    "dcterms":"http://purl.org/dc/terms/",
    "time":"http://www.w3.org/2006/time#",
    "foaf":"http://xmlns.com/foaf/0.1/",
    "om":"http://www.wurvoc.org/vocabularies/om-1.8/",
    "geo":"http://www.w3.org/2003/01/geo/wgs84_pos#"
  }
               
             */

        }

        /*
        private JToken GetJson(JToken j)
        {
            try
            {
                if (j.Type == JTokenType.Null) return null;
                using (Stream manifestStream = File.OpenRead("W3C\\" + (string)j))
                using (TextReader reader = new StreamReader(manifestStream))
                using (JsonReader jreader = new Newtonsoft.Json.JsonTextReader(reader)
                {
                    DateParseHandling = DateParseHandling.None
                })
                {
                    return JToken.ReadFrom(jreader);
                }
            }
            catch
            {
                return null;
            }
        }
        */


        // Create timer of X seconds https://developer.xamarin.com/api/type/System.Timers.Timer/
        // https://forums.xamarin.com/discussion/22443/how-to-use-timer-in-xamarin-forms
    }
}


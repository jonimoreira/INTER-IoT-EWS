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
using Microsoft.Azure.Devices.Client;
using System.Text;
using System.Threading.Tasks;

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
            // Testing context generation
            //JObject test = GetContextJSON_SAREF4health();

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

                // Setup device frequency (data rate)
                double samplingRate = 512.0;
                switch (hasFrequencyMeasurement)
                {
                    case "sarefInst:FrequencyOf256Hertz":
                        samplingRate = 256.0;
                        break;
                    case "sarefInst:FrequencyOf512Hertz":
                        samplingRate = 512.0;
                        break;
                    default:
                        break;
                }

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
                buttonConnectedClicked = true;
                shimmer.StartConnectThread();
            };

            // Get our button from the layout resource,
            // and attach an event to it
            Button buttonDisconnect = FindViewById<Button>(Resource.Id.buttonDisconnect);

            buttonDisconnect.Click += delegate {
                shimmer.Disconnect();
            };

            StartTimer();
            InitializeAzureIoTHub(); // Test sending to IoTHub
        }

        bool buttonConnectedClicked = false;

        static DeviceClient deviceClient;
        static string iotHubUri = "MyDrivingIoTHubEWS.azure-devices.net";
        static string deviceKey = "hI336rVjlnimW9VdaHYOSqfWq83VAf+Tkdc6VJKrhUA="; // From IoT Hub UI (IoT devices) Id: "ZY224DC54P"; // From MyDrivingDB.Device (table) => "bef8d779-a60d-4ac2-86f6-fd7be93022be";
        static string deviceId = "ZY224DC54P";

        private static async void SendToAzureIoTHub(JObject eCGDeviceJSON)
        {
            var messageString = eCGDeviceJSON.ToString(Formatting.Indented);//JsonConvert.SerializeObject(eCGDeviceJSON);
            var message = new Microsoft.Azure.Devices.Client.Message(Encoding.ASCII.GetBytes(messageString));
            //message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");

            await deviceClient.SendEventAsync(message);
        }


        /// <summary>
        /// Testing IoTHub
        /// </summary>
        private void InitializeAzureIoTHub()
        {
            deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey), TransportType.Http1);

            /*
            Console.WriteLine("Simulated device\n");
            deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey), TransportType.Http1);
            //deviceClient.ProductInfo = "HappyPath_Simulated-CSharp";
            SendDeviceToCloudMessagesAsync();
            Console.ReadLine();
            */
        }

        private static async void SendDeviceToCloudMessagesAsync()
        {
            double minTemperature = 20;
            double minHumidity = 60;
            int messageId = 1;
            System.Random rand = new System.Random();

            while (true)
            {
                double currentTemperature = minTemperature + rand.NextDouble() * 15;
                double currentHumidity = minHumidity + rand.NextDouble() * 20;

                var telemetryDataPoint = new
                {
                    messageId = messageId++,
                    deviceId = deviceId,
                    temperature = currentTemperature,
                    humidity = currentHumidity
                };
                var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var message = new Microsoft.Azure.Devices.Client.Message(Encoding.ASCII.GetBytes(messageString));
                message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");

                await deviceClient.SendEventAsync(message);
                Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);

                await Task.Delay(1000);
            }
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
                        buttonConnectedClicked = false;
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
                    
                    SaveData(objectCluster);

                    SensorData dataAccelX = objectCluster.GetData(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_X, "CAL");
                    SensorData dataTimestamp = objectCluster.GetData(objectCluster.GetNames().IndexOf("System Timestamp"));
                    SensorData dataECG_LL_RA = objectCluster.GetData(Shimmer3Configuration.SignalNames.ECG_LL_RA, "CAL");
                    SensorData dataECG_LA_RA = objectCluster.GetData(Shimmer3Configuration.SignalNames.ECG_LA_RA, "CAL");
                    SensorData dataECG_VX_RL = objectCluster.GetData(Shimmer3Configuration.SignalNames.ECG_VX_RL, "CAL");

                    SensorData batteryLevel = objectCluster.GetData(Shimmer3Configuration.SignalNames.V_SENSE_BATT, "CAL");

                    //data[dataNames.IndexOf("System Timestamp")]
                    //SensorData dataAccelY = objectCluster.GetData(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_Y, "CAL");
                    //SensorData dataAccelZ = objectCluster.GetData(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_Z, "CAL");
                    //SensorData dataECG
                    RunOnUiThread(() => tvAccelX.Text = "AccelX: " + dataAccelX.Data + " || batteryLevel(" + batteryLevel.Unit + "):" + batteryLevel.Data); 
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

            if (buttonConnectedClicked)
                shimmer.StartConnectThread();

        }

        System.Timers.Timer timer = new System.Timers.Timer();
        
        public void StartTimer()
        {
            timer.Interval = 5000;
            timer.Elapsed += OnTimedEvent;
            timer.Enabled = true;
            
        }

        private void MeasureObjectSize(JObject eCGDeviceJSON)
        {
            
            int count = System.Text.ASCIIEncoding.Unicode.GetByteCount(eCGDeviceJSON.ToString(Formatting.None));
            Console.WriteLine("Message size (KB): " + ConvertBytesToKilobytes(count));

        }

        static double ConvertBytesToKilobytes(long bytes)
        {
            return (bytes / 1024f);
        }


        private void SaveFile(JObject eCGDeviceJSON)
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
                string result = eCGDeviceJSON.ToString(Formatting.Indented);
                //result = HardCodeReplaceJSONLDforECGDeviceLevel(result);

                streamWriter.Write(result);
            }
        }

        private string HardCodeReplaceJSONLD(string input)
        {
            string resuult = input.Replace("\"id\"", "\"@id\"").Replace("\"label\"", "\"@label\"").Replace("\"graph\"", "\"@graph\"").Replace("\"context\"", "\"@context\"").Replace("\"language\"", "\"@language\"").Replace("\"type\"", "\"@type\"").Replace("\"value\"", "\"@value\"").Replace("\"geo_", "\"geo:").Replace("\"InterIoTMsg_", "\"InterIoTMsg:").Replace("\"saref_", "\"saref:").Replace("rdfs_","rdfs:"); 
            return resuult;
        }

        private string HardCodeReplaceJSONLDforECGDeviceLevel(string input)
        {
            string resuult = input.Replace("\"id\":", "\"@id\":").Replace("\"type\":", "\"@type\":").Replace("\"context\":", "\"@context\":");
            return resuult;
        }

        private string hasFrequencyMeasurement = "sarefInst:FrequencyOf256Hertz"; // "sarefInst:FrequencyOf512Hertz";
        private string measuresPropertyECGdata = "sarefInst:HeartElectricalActivity_Person";
        private string isMeasuredInECGdata = "sarefInst:ElectricPotential_MilliVolts";

        private JObject FormatMessageSAREF4health()
        {
            // Generate Context and Graph1
            JObject contextJSON = GetContextJSON_SAREF4health();
            // JObject graph1JSON = GetGraph1JSON(); // For IPSM

            // Generate ECGSampleSequence
            JObject measurementSeriesLA_RA = CreateMessageAndDeleteList_SAREF4health("Lead1_ECG_LA_RA");
            JObject measurementSeriesLL_RA = CreateMessageAndDeleteList_SAREF4health("Lead2_ECG_LL_RA");
            JObject measurementSeriesLL_LA = CreateMessageAndDeleteList_SAREF4health("Lead3_ECG_LL_LA");
            JObject measurementSeriesVx_RL = CreateMessageAndDeleteList_SAREF4health("UnipolarLeadVx_RL");

            // Generate ECGLeadBipolarLimb (a ECGSensor)
            JObject sensor_Lead1_ECG_LA_RA = GetSensorJSON_SAREF4health(measurementSeriesLA_RA, "https://w3id.org/def/saref4health#ECGLeadBipolarLimb", "sarefInst:ECGLead_I_code131329", "Lead I (LA-RA)", measuresPropertyECGdata);
            JObject sensor_Lead2_ECG_LL_RA = GetSensorJSON_SAREF4health(measurementSeriesLL_RA, "https://w3id.org/def/saref4health#ECGLeadBipolarLimb", "sarefInst:ECGLead_II_code131330", "Lead II (LL-RA)", measuresPropertyECGdata);
            JObject sensor_Lead3_ECG_LL_LA = GetSensorJSON_SAREF4health(measurementSeriesLL_LA, "https://w3id.org/def/saref4health#ECGLeadBipolarLimb", "sarefInst:ECGLead_III_code131389", "Lead III (LL-LA)", measuresPropertyECGdata);

            // Generate ECGLeadUnipolar (a ECGSensor)
            JObject sensor_LeadVx_RL = GetSensorJSON_SAREF4health(measurementSeriesVx_RL, "https://w3id.org/def/saref4health#ECGLeadUnipolar", "sarefInst:ECGLead_Vx_RL_code131389", "Lead Vx-RL", measuresPropertyECGdata);

            // Generate ECGDevice message (Device composed at least of an ECGSensor ("container"))
            List<JObject> listSensorsOfDevice = new List<JObject>();
            listSensorsOfDevice.Add(sensor_Lead1_ECG_LA_RA);
            listSensorsOfDevice.Add(sensor_Lead2_ECG_LL_RA);
            listSensorsOfDevice.Add(sensor_Lead3_ECG_LL_LA);
            listSensorsOfDevice.Add(sensor_LeadVx_RL);

            // Add accelerometer sensor with last received data (cross axial energy - tri-axial based) and a measurement indicating a collision was detected during the time interval
            lock (accelerationCrossAxialList)
            {
                if (accelerationCrossAxialList.Count > 0)
                {
                    Measurement lastCrossAxialComputed = accelerationCrossAxialList[accelerationCrossAxialList.Count - 1];
                    
                    // Add collisionDetected as a measurement 
                    SensorData collisionDetectedObj = new SensorData("boolean", Convert.ToDouble(collisionDetected));
                    if (timestampCollisionDetected == 0.0)
                        timestampCollisionDetected = lastCrossAxialComputed.HasTimestamp;
                    Measurement collisionDetectedMeasurement = TranslateMeasurement("collisionDetected", collisionDetectedObj, timestampCollisionDetected);

                    JObject sensor_Accelerometer = GetShimmerAccelerometerSensorJSON_SAREF4health(lastCrossAxialComputed, collisionDetectedMeasurement);
                    
                    listSensorsOfDevice.Add(sensor_Accelerometer);
                    
                    ClearDetectCollisionVariables();
                }
            }

            // Add battery consumption sensor with last received data 
            lock (batteryLevelList)
            {
                Measurement lastBatteryLevel = batteryLevelList[batteryLevelList.Count - 1];
                JObject sensor_BatteryLevel = GetSensorJSON_SAREF4health(lastBatteryLevel.JSONLDobject, "saref:Sensor", "sarefInst:Shimmer3BatteryLevelSensor_T9JRN42", "Battery level sensor of Shimmer 3  (id: T9JRN42)", "sarefInst:BatteryLevel");
                listSensorsOfDevice.Add(sensor_BatteryLevel);
            }

            JObject eCGDeviceJSON = GetECGDeviceJSON_SAREF4health(contextJSON, listSensorsOfDevice);
            
            return eCGDeviceJSON;
        }

        private void OnTimedEvent(object sender, System.Timers.ElapsedEventArgs e)
        {
            System.Console.WriteLine("OnTimedEvent: " + DateTime.Now.ToLongTimeString());
            //string idDevice = Android.OS.Build.Serial;

            // Create JSON-LD message of device measurements
            // Options to save data: SAREF2health, SAREF, FHIR RDF

            JObject eCGDeviceJSON = FormatMessageSAREF4health();
            
            string result = eCGDeviceJSON.ToString(Formatting.Indented);
            result = HardCodeReplaceJSONLDforECGDeviceLevel(result);
            System.Console.WriteLine(result);
            eCGDeviceJSON = JObject.Parse(result);

            try
            {
                //SaveFile(eCGDeviceJSON);
                SendToAzureIoTHub(eCGDeviceJSON);
                //MeasureObjectSize(eCGDeviceJSON);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("ERROR!! " + ex.Message);
            }
            
        }

        private void SaveData(ObjectCluster objectCluster)
        {
            SaveMeasurements(objectCluster);
        }
        
        private List<Measurement> batteryLevelList;
        private List<Measurement> GetBatteryLevelList()
        {
            if (batteryLevelList == null)
                batteryLevelList = new List<SAREF4health.Measurement>();
            return batteryLevelList;
        }

        private List<Measurement> accelerationCrossAxialList;
        private List<Measurement> GetAccelerationCrossAxialList()
        {
            if (accelerationCrossAxialList == null)
                accelerationCrossAxialList = new List<SAREF4health.Measurement>();
            return accelerationCrossAxialList;
        }

        private bool collisionDetected = false;

        private void SaveMeasurements(ObjectCluster objectCluster)
        {
            
            List<Double> data = objectCluster.GetData();
            List<String> dataNames = objectCluster.GetNames();
            List<String> dataUnits = objectCluster.GetUnits();

            SensorData dataTimestamp = objectCluster.GetData(dataNames.IndexOf("System Timestamp"));
            
            SensorData dataECG_LL_RA = objectCluster.GetData(Shimmer3Configuration.SignalNames.ECG_LL_RA, "CAL");
            SensorData dataECG_LA_RA = objectCluster.GetData(Shimmer3Configuration.SignalNames.ECG_LA_RA, "CAL");
            SensorData dataECG_VX_RL = objectCluster.GetData(Shimmer3Configuration.SignalNames.ECG_VX_RL, "CAL");

            CompactECGData(dataTimestamp, dataECG_LL_RA, dataECG_LA_RA, dataECG_VX_RL);

            SensorData dataAccelX = objectCluster.GetData(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_X, "CAL");
            SensorData dataAccelY = objectCluster.GetData(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_Y, "CAL");
            SensorData dataAccelZ = objectCluster.GetData(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_Z, "CAL");

            // Compute cross-axial energy
            double crossAxialEnergy = Math.Pow(dataAccelX.Data, 2.0) + Math.Pow(dataAccelY.Data, 2.0) + Math.Pow(dataAccelZ.Data, 2.0);
            SensorData crossAxialEnergyObj = new SensorData("cross-axial-energy", crossAxialEnergy);
            Measurement accelerationCrossAxial = TranslateMeasurement("accelerationCrossAxial", crossAxialEnergyObj, dataTimestamp.Data);
            accelerationCrossAxialList = GetAccelerationCrossAxialList();
            lock (accelerationCrossAxialList)
            {
                accelerationCrossAxialList.Add(accelerationCrossAxial);
                collisionDetected = DetectCollision(crossAxialEnergy);

                if (collisionDetected)
                {
                    timestampCollisionDetected = dataTimestamp.Data;
                    Console.WriteLine("Collision detected!!!");
                }
            }

            // Battery level (in mVolts)
            SensorData batteryLevel = objectCluster.GetData(Shimmer3Configuration.SignalNames.V_SENSE_BATT, "CAL");
            batteryLevelList = GetBatteryLevelList();
            lock (batteryLevelList)
            {
                Measurement batteryLevelMeasurement = TranslateMeasurement("batteryLevel", batteryLevel, dataTimestamp.Data);
                batteryLevelList.Add(batteryLevelMeasurement);
            }
        }
        
        private double sumCrossAxialValues = 0.0;
        private int countCrossAxialValues = 0;
        private const double InitialVariance = 0.0; //7.8; // 61.1
        private double varianceCrossAxialValues = InitialVariance; // if acceleration is measured in m/s^2 => equivalent to a change of 10km/h in 1 second. 
        private double timestampCollisionDetected = 0.0;

        private bool DetectCollision(double currentCrossAxialEnergy)
        {
            bool result = false;
            sumCrossAxialValues += currentCrossAxialEnergy;
            countCrossAxialValues++;
            double meanCrossAxialValues = sumCrossAxialValues / countCrossAxialValues;
            varianceCrossAxialValues += Math.Pow(currentCrossAxialEnergy - meanCrossAxialValues, 2);
            double standardDeviation = 100; // Math.Sqrt(varianceCrossAxialValues);

            if ((currentCrossAxialEnergy < meanCrossAxialValues - standardDeviation) 
                || (currentCrossAxialEnergy > meanCrossAxialValues + standardDeviation))
                result = true;

            return result;
        }

        private void ClearDetectCollisionVariables()
        {
            sumCrossAxialValues = 0.0;
            countCrossAxialValues = 0;
            varianceCrossAxialValues = InitialVariance; // if acceleration is measured in m/s^2 => equivalent to a change of 10km/h in 1 second.
            collisionDetected = false;
            timestampCollisionDetected = 0.0;
            lock (accelerationCrossAxialList)
                accelerationCrossAxialList.Clear();
        }

        private string GeneratePID(Measurement msg)
        {
            string result = msg.RelatesToProperty + "_Test.X.Y_" + msg.HasTimestamp; // string.Empty;

            //result = msg.Type + "_Test.1.1_" + msg.HasTimestamp;

            return result;
        }

        private string GeneratePID_SAREF4health_ECGSampleSequence(string type, string timestamp)
        {
            string result = "sarefInst:SpeedMeasurement__Test.1.1_1511466006.9682777"; // string.Empty;

            result = type + "_Test.1.1_" + timestamp;

            return result;
        }

        private string GeneratePID(JsonObjectAttribute msg)
        {
            string result = "sarefInst:Message_Test.1.1_1511466006.9682777"; 

            //result = msg.Type + "_Test.1.1_" + msg.HasTimestamp;

            return result;
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

        private string TranslateRelatesToProperty(string signalName)
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


        private string TranslateMeasurementType(string signalName)
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
        
        private JObject TranslateECGSampleSequence_SAREF4health(List<KeyValuePair<double, double>> values)
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
  '@type' : 'https://w3id.org/def/saref4health#ECGSampleSequence',
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

        private void CompactECGData(SensorData dataTimestamp, SensorData dataECG_LL_RA, SensorData dataECG_LA_RA, SensorData dataECG_VX_RL)
        {
            
            StoreECGleadValue(Shimmer3Configuration.SignalNames.ECG_LA_RA, dataTimestamp.Data, dataECG_LA_RA.Data);
            StoreECGleadValue(Shimmer3Configuration.SignalNames.ECG_LL_RA, dataTimestamp.Data, dataECG_LL_RA.Data);
            
            // Derived LL-LA
            StoreECGleadValue(ECG_LL_LA, dataTimestamp.Data, dataECG_LA_RA.Data - dataECG_LL_RA.Data);

            StoreECGleadValue(Shimmer3Configuration.SignalNames.ECG_VX_RL, dataTimestamp.Data, dataECG_VX_RL.Data);

        }
        
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
                        
        }

        private JObject CreateMessageAndDeleteList_SAREF4health(string lead)
        {
            JObject measurementSeries = null;
            switch (lead)
            {
                case "Lead1_ECG_LA_RA":
                    valuesLead1_ECG_LA_RA = GetValuesLead1_ECG_LA_RA();
                    measurementSeries = CreateMessageAndDeleteList_SAREF4health(valuesLead1_ECG_LA_RA);
                    break;
                case "Lead2_ECG_LL_RA":
                    valuesLead2_ECG_LL_RA = GetValuesLead2_ECG_LL_RA();
                    measurementSeries = CreateMessageAndDeleteList_SAREF4health(valuesLead2_ECG_LL_RA);
                    break;
                case "Lead3_ECG_LL_LA":
                    valuesLead3_ECG_LL_LA = GetValuesLead3_ECG_LL_LA();
                    measurementSeries = CreateMessageAndDeleteList_SAREF4health(valuesLead3_ECG_LL_LA);
                    break;
                case "UnipolarLeadVx_RL":
                    valuesUnipolarLeadVx_RL = GetValuesUnipolarLeadVx_RL();
                    measurementSeries = CreateMessageAndDeleteList_SAREF4health(valuesUnipolarLeadVx_RL);
                    break;
                default:
                    break;
            }
            return measurementSeries;
        }

        private JObject CreateMessageAndDeleteList_SAREF4health(List<KeyValuePair<double, double>> valuesLead)
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

        private int FormatECGMeasurementValue(double originalValue)
        {
            int result = (int) (originalValue * 100.0);
            return result;
        }
        
        private JObject contextJSON_SAREF4health;

        private JObject GetContextJSON_SAREF4health()
        {
            if (contextJSON_SAREF4health != null)
                return contextJSON_SAREF4health;

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
    'sarefInst' : 'https://w3id.org/saref/instances#',
    'owl' : 'http://www.w3.org/2002/07/owl#',
    'prefix' : 'http://purl.oclc.org/NET/ssnx/qu/prefix#',
    'saref' : 'https://w3id.org/saref#',
    'xsd' : 'http://www.w3.org/2001/XMLSchema#',
    'dim' : 'http://purl.oclc.org/NET/ssnx/qu/dim#',
    'skos' : 'http://www.w3.org/2004/02/skos/core#',
    'rdfs' : 'http://www.w3.org/2000/01/rdf-schema#',
    'saref4envi' : 'https://w3id.org/def/saref4envi#',
    'geo' : 'http://www.w3.org/2003/01/geo/wgs84_pos#',
    'dct' : 'http://purl.org/dc/terms/',
    'xml' : 'http://www.w3.org/XML/1998/namespace',
    'dcterms' : 'http://purl.org/dc/terms/',
    'vann' : 'http://purl.org/vocab/vann/',
    'foaf' : 'http://xmlns.com/foaf/0.1/',
    'om' : 'http://www.wurvoc.org/vocabularies/om-1.8/',
    'cc' : 'http://creativecommons.org/ns#',
    'quantity' : 'http://purl.oclc.org/NET/ssnx/qu/quantity#',
    'qu' : 'http://purl.oclc.org/NET/ssnx/qu/qu#',
    'm3' : 'http://purl.org/iot/vocab/m3-lite#',
    'unit' : 'http://purl.oclc.org/NET/ssnx/qu/unit#',
    'rdf' : 'http://www.w3.org/1999/02/22-rdf-syntax-ns#',
    'qu-rec20' : 'http://purl.oclc.org/NET/ssnx/qu/qu-rec20#',
    'time' : 'http://www.w3.org/2006/time#',
    'dc' : 'http://purl.org/dc/elements/1.1/'
  
}
            ";

            contextJSON_SAREF4health = JObject.Parse(context);

            return contextJSON_SAREF4health;
        }


        private JObject GetECGDeviceJSON_SAREF4health(JObject contextJSON, List<JObject> listSensorsOfDevice)
        {

            string deviceId = "sarefInst:Shimmer3ECG_unit_T9JRN42_" + Android.OS.Build.Serial; // _[ID Shimmer ECG 3]_[Mobile ID] --> can be useful for security...;

/*
            string listSensorsOfDeviceStr = string.Empty;
            foreach (JObject json in listSensorsOfDevice)
            {
                listSensorsOfDeviceStr += "," + json.ToString(Formatting.Indented);
            }
            listSensorsOfDeviceStr = listSensorsOfDeviceStr.Substring(1);

            string eCGDeviceJSONstr = @"
{
   '@context' : " + contextJSON.ToString(Formatting.Indented) + @",
   '@id' : '" + deviceId + @"',
   '@type' : 'https://w3id.org/def/saref4health#ECGDevice',
   'comment' : 'Shimmer3 ECG unit: INTER-IoT-EWS project, composed of ECG sensors and other features (e.g. Accelerometer).',
   'label' : 'Shimmer3 ECG unit T9J-RN42',
   'seeAlso' : 'http://www.shimmersensing.com/products/ecg-development-kit#specifications-tab',
   'hasFrequencyMeasurement' : '" + hasFrequencyMeasurement + @"',
   'accomplishes' : 'sarefInst:RecordingECGSession_01',
   'hasManufacturer' : 'Shimmer',
   'hasTypicalConsumption : 'sarefInst:Shimmer3ECGBattery',
   'consistsOf : [" + listSensorsOfDeviceStr + @"]
}
            ";
            JObject eCGDeviceJSON = JObject.Parse(eCGDeviceJSONstr);
            */

            
            JObject eCGDeviceJSON = JObject.FromObject(new
            {
                @context = contextJSON,
                @id = deviceId,
                @type = "https://w3id.org/def/saref4health#ECGDevice",
                comment = "Shimmer3 ECG unit: INTER-IoT-EWS project, composed of ECG sensors and other features (e.g. Accelerometer).",
                label = "Shimmer3 ECG unit T9J-RN42",
                seeAlso = "http://www.shimmersensing.com/products/ecg-development-kit#specifications-tab",
                hasFrequencyMeasurement = hasFrequencyMeasurement,
                accomplishes = "sarefInst:RecordingECGSession_01",
                hasManufacturer = "Shimmer",
                hasTypicalConsumption = "sarefInst:Shimmer3ECGBattery",
                consistsOf = listSensorsOfDevice
            });
            
            return eCGDeviceJSON;
        }

        private JObject GetSensorJSON_SAREF4health(JObject measurementsSeries, string type, string id, string label, string measuresProperty)
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

        private JObject GetShimmerAccelerometerSensorJSON_SAREF4health(Measurement measurement, Measurement collisionDetectedMeasurement)
        {
            List<JObject> measurements = new List<JObject>();
            measurements.Add(measurement.JSONLDobject);
            measurements.Add(collisionDetectedMeasurement.JSONLDobject);

            JObject accelJSON = JObject.FromObject(new
            {
                @id = "sarefInst:Shimmer3AccelerometerSensor_T9JRN42",
                @type = "https://w3id.org/def/saref4health#AccelerometerSensor",
                comment = "Shimmer3 Accelerometer sensor",
                label = "Shimmer3 Accelerometer T9J-RN42",
                measuresProperty = "sarefInst:Acceleration_Vehicle",
                makesMeasurement = measurements
            });
            return accelJSON;
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
            

        }
        
    }
}


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
using Plugin.CurrentActivity;

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
                switch (jsonLdSaref4health.hasFrequencyMeasurement["@id"].ToString())
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

            // GeoLocator stuff
            CrossCurrentActivity.Current.Init(this, bundle);
            geolocatorManager = new ShimmerCaptureXamarin.GeolocatorManager();
            geolocatorManager.ExecuteStartTrackingAsync();
            
            StartTimer();
            InitializeAzureIoTHub(); // Test sending to IoTHub
            contextJSON_SAREF4health = jsonLdSaref4health.GetContextJSON_SAREF4health();
        }
        
        public Formatting formattingJsonLd = Formatting.None;

        bool buttonConnectedClicked = false;

        static DeviceClient deviceClient;

        static string deviceId = "XXXXXXXXXXXXXXXX";

        static string iotHubUri = "XXXXXXXXXXXXXXXX";
                
        static string deviceKey = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"; 
        
        private SAREF4health.JsonLD jsonLdSaref4health = new SAREF4health.JsonLD();
        private VehicleCollisionDetection vehicleCollisionDetection = new VehicleCollisionDetection();
        private JObject contextJSON_SAREF4health;

        private System.Timers.Timer timer = new System.Timers.Timer();
        private int timerIntervalInMs = 5000;

        private GeolocatorManager geolocatorManager;
        
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
            {
                accelerationCrossAxialList = new List<SAREF4health.Measurement>();
            }

            return accelerationCrossAxialList;
        }
        
        private List<Measurement> accelerationX_ECGdevice_List;
        private List<Measurement> GetAccelerationX_ECGdevice_List()
        {
            if (accelerationX_ECGdevice_List == null)
                accelerationX_ECGdevice_List = new List<SAREF4health.Measurement>();
            return accelerationX_ECGdevice_List;
        }

        private List<Measurement> accelerationY_ECGdevice_List;
        private List<Measurement> GetAccelerationY_ECGdevice_List()
        {
            if (accelerationY_ECGdevice_List == null)
                accelerationY_ECGdevice_List = new List<SAREF4health.Measurement>();
            return accelerationY_ECGdevice_List;
        }

        private List<Measurement> accelerationZ_ECGdevice_List;
        private List<Measurement> GetAccelerationZ_ECGdevice_List()
        {
            if (accelerationZ_ECGdevice_List == null)
                accelerationZ_ECGdevice_List = new List<SAREF4health.Measurement>();
            return accelerationZ_ECGdevice_List;
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


        private static async void SendToAzureIoTHub(JObject eCGDeviceJSON)
        {
            var messageString = eCGDeviceJSON.ToString(Formatting.None);//JsonConvert.SerializeObject(eCGDeviceJSON);
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
                    isStreaming = false;
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
                        isStreaming = true;
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

                    RunOnUiThread(() => tvAccelX.Text = "Mobile>Cloud interval (ms): " + timerIntervalInMs + "|| AccelX: " + dataAccelX.Data + " || batteryLevel(" + batteryLevel.Unit + "):" + batteryLevel.Data); 
                    RunOnUiThread(() => tvTimestamp.Text = "Timestamp: " + dataTimestamp.Data); 
                    RunOnUiThread(() => tvECG.Text = "ECG LL_RA: " + dataECG_LL_RA.Data + " | LA_RA: " + dataECG_LA_RA.Data + " | VX_RL: " + dataECG_VX_RL.Data);
                    break;
            }

            if (buttonConnectedClicked)
                shimmer.StartConnectThread();

        }
        
        public void StartTimer()
        {
            timer.Interval = timerIntervalInMs;
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
                string result = eCGDeviceJSON.ToString(formattingJsonLd);
                //result = HardCodeReplaceJSONLDforECGDeviceLevel(result);

                streamWriter.Write(result);
            }
        }

        private JObject FormatMessageSAREF4health_FieldGateway()
        {
            JObject contextJSON = contextJSON_SAREF4health;
            List<JObject> devicesComposingMobile = new List<JObject>();

            // ECG device (unit)
            JObject eCGDeviceJSON = FormatMessageSAREF4health_ECGDevice();
            devicesComposingMobile.Add(eCGDeviceJSON);

            AddProcessedAccelerometer(devicesComposingMobile);

            JObject mobileDevice = jsonLdSaref4health.GetFieldGatewayMobileDeviceJSON_SAREF4health(contextJSON, devicesComposingMobile, geolocatorManager);

            return mobileDevice;
        }

        private double GetAverage(List<Measurement> measurements)
        {
            double result = 0;
            int counter = 0;

            foreach (Measurement measurement in measurements)
            {
                result += measurement.HasValue;
                counter++;
            }

            if (counter > 0)
                result = result / counter;

            return result;
        }

        private JObject FormatMessageSAREF4health_ECGDevice()
        {
            // Generate ECGSampleSequence
            JObject measurementSeriesLA_RA = CreateMessageAndDeleteList_SAREF4health("Lead1_ECG_LA_RA");
            JObject measurementSeriesLL_RA = CreateMessageAndDeleteList_SAREF4health("Lead2_ECG_LL_RA");
            JObject measurementSeriesLL_LA = CreateMessageAndDeleteList_SAREF4health("Lead3_ECG_LL_LA");
            JObject measurementSeriesVx_RL = CreateMessageAndDeleteList_SAREF4health("UnipolarLeadVx_RL");

            // Generate ECGLeadBipolarLimb (a ECGSensor)
            JObject sensor_Lead1_ECG_LA_RA = jsonLdSaref4health.GetSensorJSON_SAREF4health(measurementSeriesLA_RA, "saref4health:ECGLeadBipolarLimb", "sarefInst:ECGLead_I_code131329", "Lead I (LA-RA)", jsonLdSaref4health.measuresPropertyECGdata);
            JObject sensor_Lead2_ECG_LL_RA = jsonLdSaref4health.GetSensorJSON_SAREF4health(measurementSeriesLL_RA, "saref4health:ECGLeadBipolarLimb", "sarefInst:ECGLead_II_code131330", "Lead II (LL-RA)", jsonLdSaref4health.measuresPropertyECGdata);
            JObject sensor_Lead3_ECG_LL_LA = jsonLdSaref4health.GetSensorJSON_SAREF4health(measurementSeriesLL_LA, "saref4health:ECGLeadBipolarLimb", "sarefInst:ECGLead_III_code131389", "Lead III (LL-LA)", jsonLdSaref4health.measuresPropertyECGdata);

            // Generate ECGLeadUnipolar (a ECGSensor)
            JObject sensor_LeadVx_RL = jsonLdSaref4health.GetSensorJSON_SAREF4health(measurementSeriesVx_RL, "saref4health:ECGLeadUnipolar", "sarefInst:ECGLead_Vx_RL_code131389", "Lead Vx-RL", jsonLdSaref4health.measuresPropertyECGdata);

            // Generate ECGDevice message (Device composed at least of an ECGSensor ("container"))
            List<JObject> listSensorsOfDevice = new List<JObject>();
            listSensorsOfDevice.Add(sensor_Lead1_ECG_LA_RA);
            listSensorsOfDevice.Add(sensor_Lead2_ECG_LL_RA);
            listSensorsOfDevice.Add(sensor_Lead3_ECG_LL_LA);
            listSensorsOfDevice.Add(sensor_LeadVx_RL);

            lock (accelerationX_ECGdevice_List)
            {
                if (accelerationX_ECGdevice_List.Count > 0)
                {
                    JObject sensor_AccelerometerX = jsonLdSaref4health.GetShimmerAccelerometerSensorJSON_SAREF4health(GetAverageAcceleration(accelerationX_ECGdevice_List), GetAverageAcceleration(accelerationY_ECGdevice_List), GetAverageAcceleration(accelerationZ_ECGdevice_List));
                    listSensorsOfDevice.Add(sensor_AccelerometerX);
                    accelerationX_ECGdevice_List.Clear();
                }
            }
            
            // Add battery consumption sensor with last received data 
            lock (batteryLevelList)
            {
                if (batteryLevelList.Count > 0)
                {
                    Measurement lastBatteryLevel = batteryLevelList[batteryLevelList.Count - 1];
                    JObject sensor_BatteryLevel = jsonLdSaref4health.GetSensorJSON_SAREF4health(lastBatteryLevel.JSONLDobject, "saref:Sensor", "sarefInst:Shimmer3BatteryLevelSensor_T9JRN42", "Battery level sensor of Shimmer 3  (id: T9JRN42)", jsonLdSaref4health.sarefInst_BatteryLevel);
                    listSensorsOfDevice.Add(sensor_BatteryLevel);
                    batteryLevelList.Clear();
                }
            }

            JObject eCGDeviceJSON = jsonLdSaref4health.GetECGDeviceJSON_SAREF4health(listSensorsOfDevice);
            
            return eCGDeviceJSON;
        }

        private bool isStreaming = false;

        private void OnTimedEvent(object sender, System.Timers.ElapsedEventArgs e)
        {
            System.Console.WriteLine("OnTimedEvent: " + DateTime.Now.ToLongTimeString());

            if (!isStreaming)
                return;

            JObject fieldGatewayDevice_Smartphone = FormatMessageSAREF4health_FieldGateway();
            
            try
            {
                //SaveFile(fieldGatewayDevice_Smartphone);
                SendToAzureIoTHub(fieldGatewayDevice_Smartphone);
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

            accelerationX_ECGdevice_List = GetAccelerationX_ECGdevice_List();
            lock (accelerationX_ECGdevice_List)
            {
                Measurement accelerationX_ECGdeviceMeasurement = jsonLdSaref4health.TranslateMeasurement(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_X, dataAccelX, dataTimestamp.Data);
                accelerationX_ECGdevice_List.Add(accelerationX_ECGdeviceMeasurement);
            }

            accelerationY_ECGdevice_List = GetAccelerationY_ECGdevice_List();
            lock (accelerationY_ECGdevice_List)
            {
                Measurement accelerationY_ECGdeviceMeasurement = jsonLdSaref4health.TranslateMeasurement(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_Y, dataAccelY, dataTimestamp.Data);
                accelerationY_ECGdevice_List.Add(accelerationY_ECGdeviceMeasurement);
            }

            accelerationZ_ECGdevice_List = GetAccelerationZ_ECGdevice_List();
            lock (accelerationZ_ECGdevice_List)
            {
                Measurement accelerationZ_ECGdeviceMeasurement = jsonLdSaref4health.TranslateMeasurement(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_Z, dataAccelZ, dataTimestamp.Data);
                accelerationZ_ECGdevice_List.Add(accelerationZ_ECGdeviceMeasurement);
            }

            // Compute cross-axial energy from ECG Device accelermoter
            double crossAxialEnergy = ComputeCrossAxialFunction(dataAccelX, dataAccelY, dataAccelZ); 

            SensorData crossAxialEnergyObj = new SensorData("cross-axial-energy", crossAxialEnergy);
            Measurement accelerationCrossAxial = jsonLdSaref4health.TranslateMeasurement("accelerationCrossAxial", crossAxialEnergyObj, dataTimestamp.Data);
            accelerationCrossAxialList = GetAccelerationCrossAxialList();
            lock (accelerationCrossAxialList) //lock(vehicleCollisionDetection)
            {
                accelerationCrossAxialList.Add(accelerationCrossAxial);
                vehicleCollisionDetection.collisionDetected = DetectCollision(crossAxialEnergy);

                if (vehicleCollisionDetection.collisionDetected)
                {
                    vehicleCollisionDetection.timestampCollisionDetected = dataTimestamp.Data;
                    Console.WriteLine("Collision detected!!!");
                }
            }

            // Battery level (in mVolts) of ECG device
            SensorData batteryLevel = objectCluster.GetData(Shimmer3Configuration.SignalNames.V_SENSE_BATT, "CAL");
            batteryLevelList = GetBatteryLevelList();
            lock (batteryLevelList)
            {
                Measurement batteryLevelMeasurement = jsonLdSaref4health.TranslateMeasurement("batteryLevel", batteryLevel, dataTimestamp.Data);
                batteryLevelList.Add(batteryLevelMeasurement);
            }
        }

        private double ComputeCrossAxialFunction(SensorData dataAccelX, SensorData dataAccelY, SensorData dataAccelZ)
        {
            double Gforce = 9.806; // m/s2 
            return Math.Pow(dataAccelX.Data, 2.0) + Math.Pow(dataAccelY.Data, 2.0) + Math.Pow(dataAccelZ.Data - Gforce, 2.0);
        }

        private bool DetectCollision(double currentCrossAxialEnergy)
        {
            vehicleCollisionDetection.crossAxialEnergyWhenCollisionDetected = currentCrossAxialEnergy;
            bool result = false;
            vehicleCollisionDetection.sumCrossAxialValues += currentCrossAxialEnergy;
            vehicleCollisionDetection.countCrossAxialValues++;
            vehicleCollisionDetection.meanCrossAxialValues = vehicleCollisionDetection.sumCrossAxialValues / vehicleCollisionDetection.countCrossAxialValues;
            vehicleCollisionDetection.varianceCrossAxialValues += Math.Pow(currentCrossAxialEnergy - vehicleCollisionDetection.meanCrossAxialValues, 2);

            vehicleCollisionDetection.sumOfDerivation += (currentCrossAxialEnergy) * (currentCrossAxialEnergy);
            vehicleCollisionDetection.sumOfDerivationAverage = vehicleCollisionDetection.sumOfDerivation / vehicleCollisionDetection.countCrossAxialValues;
            vehicleCollisionDetection.standardDeviation = Math.Sqrt(vehicleCollisionDetection.sumOfDerivationAverage - (vehicleCollisionDetection.meanCrossAxialValues * vehicleCollisionDetection.meanCrossAxialValues));

            //double standardDeviation = CalculateStandardDeviation(); // 100; // Math.Sqrt(varianceCrossAxialValues);
            // Anomaly detection
            double minVal = double.MinValue; // vehicleCollisionDetection.Threshold + vehicleCollisionDetection.meanCrossAxialValues - vehicleCollisionDetection.standardDeviation;
            double maxVal = vehicleCollisionDetection.Threshold + vehicleCollisionDetection.meanCrossAxialValues + vehicleCollisionDetection.standardDeviation;
            /*
            if ((currentCrossAxialEnergy < minVal) 
                || (currentCrossAxialEnergy > maxVal))
                result = true;
                */
            if (Math.Sqrt(currentCrossAxialEnergy) > vehicleCollisionDetection.Threshold)
                result = true;

            return result;
            //return true;
        }


        private void AddProcessedAccelerometer(List<JObject> devicesComposingMobile)
        {
            // Add accelerometer sensor with last received data (cross axial energy - tri-axial based) and a measurement indicating a collision was detected during the time interval
            lock (accelerationCrossAxialList) lock (vehicleCollisionDetection)
                {
                    if (accelerationCrossAxialList.Count > 0)
                    {
                        List<JObject> processedMeasurements = new List<JObject>();

                        Measurement lastCrossAxialComputed = accelerationCrossAxialList[accelerationCrossAxialList.Count - 1];
                        double avgCrossAxial = GetAverage(accelerationCrossAxialList);
                        lastCrossAxialComputed.HasValue = avgCrossAxial;
                        lastCrossAxialComputed.Label = "Average value of the calculated cross-axial function within the smartphone_to_cloud frequency (" + timerIntervalInMs + " ms)";
                        processedMeasurements.Add(lastCrossAxialComputed.JSONLDobject);

                        // Add collisionDetected, mean, stdDeviation and value as measurements if a collision was detected
                        SensorData collisionDetectedObj = new SensorData("boolean", Convert.ToDouble(vehicleCollisionDetection.collisionDetected));
                        if (vehicleCollisionDetection.collisionDetected)
                        {
                            // Measurement collision detected (timestamp when it was detected, bool = true)
                            Measurement collisionDetectedMeasurement = jsonLdSaref4health.TranslateMeasurement("collisionDetected", collisionDetectedObj, vehicleCollisionDetection.timestampCollisionDetected);
                            processedMeasurements.Add(collisionDetectedMeasurement.JSONLDobject);

                            // Measurements of mean, standard deviation and cross axial value when detected
                            SensorData collisionDetectedObjMean = new SensorData("crossaxialfunction", vehicleCollisionDetection.meanCrossAxialValues);
                            Measurement meanDetectedMeasurement = jsonLdSaref4health.TranslateMeasurement("cross-axial-energy_mean", collisionDetectedObjMean, vehicleCollisionDetection.timestampCollisionDetected);
                            processedMeasurements.Add(meanDetectedMeasurement.JSONLDobject);

                            SensorData collisionDetectedObjStd = new SensorData("crossaxialfunction", vehicleCollisionDetection.standardDeviation);
                            Measurement stddevDetectedMeasurement = jsonLdSaref4health.TranslateMeasurement("cross-axial-energy_std-dev", collisionDetectedObjStd, vehicleCollisionDetection.timestampCollisionDetected);
                            processedMeasurements.Add(stddevDetectedMeasurement.JSONLDobject);

                            SensorData collisionDetectedObjCrossAxial = new SensorData("crossaxialfunction", vehicleCollisionDetection.crossAxialEnergyWhenCollisionDetected);
                            Measurement crossaxDetectedMeasurement = jsonLdSaref4health.TranslateMeasurement("cross-axial-energy_max", collisionDetectedObjCrossAxial, vehicleCollisionDetection.timestampCollisionDetected);
                            crossaxDetectedMeasurement.Label = "value processed when collision detected";
                            processedMeasurements.Add(crossaxDetectedMeasurement.JSONLDobject);

                            SensorData collisionDetectedObjThreshold = new SensorData("m/(sec^2)", vehicleCollisionDetection.Threshold);
                            Measurement thresholdDetectedMeasurement = jsonLdSaref4health.TranslateMeasurement("ThresholdGforce", collisionDetectedObjThreshold, vehicleCollisionDetection.timestampCollisionDetected);
                            processedMeasurements.Add(thresholdDetectedMeasurement.JSONLDobject);

                        }

                        // Create Processed Accelerometer: accelerometer that is processed by the smartphone using acceleration data from ECG device
                        JObject sensorProcessedAccelerometer = jsonLdSaref4health.GetSensorJSON_SAREF4health(processedMeasurements, "saref:Sensor", "sarefInst:ProcessedAccelerometer", "Accelerometer measurements processed by mobile app (ShimmerLD) with acceleration data from Shimmer3 ECG unit", jsonLdSaref4health.sarefInst_ProcessedAccelerometer);
                        devicesComposingMobile.Add(sensorProcessedAccelerometer);
                        vehicleCollisionDetection.ClearDetectCollisionVariables(accelerationCrossAxialList);
                    }
                }
        }

        private Measurement GetAverageAcceleration(List<Measurement> measurementsAxis)
        {
            if (measurementsAxis.Count == 0)
                return null;

            Measurement result = new Measurement();
            lock (measurementsAxis)
            {
                double accelValueTotal = 0;
                int accelMeasurementsCount = 0;
                foreach (Measurement measurement in measurementsAxis)
                {
                    if (accelMeasurementsCount == 0)
                        result = measurement;

                    accelValueTotal += measurement.HasValue;
                    accelMeasurementsCount++;                    
                }
                result.HasValue = accelValueTotal / accelMeasurementsCount;
            }
            return result;
        }

        private void CompactECGData(SensorData dataTimestamp, SensorData dataECG_LL_RA, SensorData dataECG_LA_RA, SensorData dataECG_VX_RL)
        {
            
            StoreECGleadValue(Shimmer3Configuration.SignalNames.ECG_LA_RA, dataTimestamp.Data, dataECG_LA_RA.Data);
            StoreECGleadValue(Shimmer3Configuration.SignalNames.ECG_LL_RA, dataTimestamp.Data, dataECG_LL_RA.Data);
            
            // Derived LL-LA
            StoreECGleadValue(ECG_LL_LA, dataTimestamp.Data, dataECG_LA_RA.Data - dataECG_LL_RA.Data);

            StoreECGleadValue(Shimmer3Configuration.SignalNames.ECG_VX_RL, dataTimestamp.Data, dataECG_VX_RL.Data);

        }
        
        
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
                    measurementSeries = jsonLdSaref4health.CreateMessageAndDeleteList_SAREF4health(valuesLead1_ECG_LA_RA, lead);
                    break;
                case "Lead2_ECG_LL_RA":
                    valuesLead2_ECG_LL_RA = GetValuesLead2_ECG_LL_RA();
                    measurementSeries = jsonLdSaref4health.CreateMessageAndDeleteList_SAREF4health(valuesLead2_ECG_LL_RA, lead);
                    break;
                case "Lead3_ECG_LL_LA":
                    valuesLead3_ECG_LL_LA = GetValuesLead3_ECG_LL_LA();
                    measurementSeries = jsonLdSaref4health.CreateMessageAndDeleteList_SAREF4health(valuesLead3_ECG_LL_LA, lead);
                    break;
                case "UnipolarLeadVx_RL":
                    valuesUnipolarLeadVx_RL = GetValuesUnipolarLeadVx_RL();
                    measurementSeries = jsonLdSaref4health.CreateMessageAndDeleteList_SAREF4health(valuesUnipolarLeadVx_RL, lead);
                    break;
                default:
                    break;
            }
            return measurementSeries;
        }
        
    }
}


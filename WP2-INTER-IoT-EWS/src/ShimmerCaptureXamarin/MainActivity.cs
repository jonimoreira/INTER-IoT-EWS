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

namespace ShimmerCaptureXamarin
{

    public class SAREFmessage
    {
        public SAREFmessage()
        {
        }

        private string isMeasuredIn;
        private string relatesToProperty;
        private string type;
        private string id;
        private string label;
        private double hasTimestamp;

        public string IsMeasuredIn
        {
            get
            {
                return isMeasuredIn;
            }

            set
            {
                isMeasuredIn = value;
            }
        }

        public string RelatesToProperty
        {
            get
            {
                return relatesToProperty;
            }

            set
            {
                relatesToProperty = value;
            }
        }

        public string Type
        {
            get
            {
                return type;
            }

            set
            {
                type = value;
            }
        }

        public string Id
        {
            get
            {
                return id;
            }

            set
            {
                id = value;
            }
        }

        public string Label
        {
            get
            {
                return label;
            }

            set
            {
                label = value;
            }
        }

        public double HasTimestamp
        {
            get
            {
                return hasTimestamp;
            }

            set
            {
                hasTimestamp = value;
            }
        }
    }
    
    [Activity(Label = "ShimmerCaptureXamarin", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        ShimmerLogAndStreamXamarin shimmer;
        TextView tvShimmerState;
        TextView tvAccelX;
        protected override void OnCreate(Bundle bundle)
        {
            //string test = FormatJSONLD(null);
            // Formatar a mensagem na mao mesmo... 
            //return;

            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);


            tvShimmerState = FindViewById<TextView>(Resource.Id.textViewShimmerState);
            tvAccelX = FindViewById<TextView>(Resource.Id.textViewAccelX);
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
                int enabledSensors = ((int)ShimmerBluetooth.SensorBitmapShimmer3.SENSOR_A_ACCEL | (int)ShimmerBluetooth.SensorBitmapShimmer3.SENSOR_EXG1_24BIT | (int)ShimmerBluetooth.SensorBitmapShimmer3.SENSOR_EXG2_24BIT); // | (int)ShimmerBluetooth.ChannelContentsShimmer3.Temperature); 
                int accelRange = 0; // ((int)ShimmerBluetooth.SENSITIVITY_MATRIX_WIDE_RANGE_ACCEL_8G_SHIMMER3);
                int gsrRange = ((int)ShimmerBluetooth.SensorBitmapShimmer3.SENSOR_GSR);
                double samplingRate = 128.0; // 40.0; // 512.0;
                //byte[] defaultECGReg1 = new byte[10] { 0x00, 0xA0, 0x10, 0x40, 0x40, 0x2D, 0x00, 0x00, 0x02, 0x03 }; //see ShimmerBluetooth.SHIMMER3_DEFAULT_ECG_REG1
                //byte[] defaultECGReg2 = new byte[10] { 0x00, 0xA0, 0x10, 0x40, 0x47, 0x00, 0x00, 0x00, 0x02, 0x01 }; //see ShimmerBluetooth.SHIMMER3_DEFAULT_ECG_REG2
                byte[] defaultECGReg1 = ShimmerBluetooth.SHIMMER3_DEFAULT_TEST_REG1; //also see ShimmerBluetooth.SHIMMER3_DEFAULT_ECG_REG1
                byte[] defaultECGReg2 = ShimmerBluetooth.SHIMMER3_DEFAULT_TEST_REG2; //also see ShimmerBluetooth.SHIMMER3_DEFAULT_ECG_REG2

                //shimmer.GetEnabledSensors()
                string bluetoothAddress = "00:06:66:88:db:ca".ToUpper();
                shimmer = new ShimmerLogAndStreamXamarin("ShimmerXamarin", bluetoothAddress, samplingRate, accelRange, gsrRange, enabledSensors, false, false, false, 0, 0, defaultECGReg1, defaultECGReg2, false);
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
                    String result="";
                    String resultNames = "";
                    foreach (Double d in data)
                    {
                        result = d.ToString() + "||" + result;
                    }
                    foreach (String s in dataNames)
                    {
                        resultNames = s + "||" + resultNames;
                    }
                    System.Console.WriteLine(resultNames);
                    System.Console.WriteLine(result);

                    SaveData(objectCluster);

                    SensorData dataAccelX = objectCluster.GetData(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_X, "CAL");
                    //SensorData dataAccelY = objectCluster.GetData(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_Y, "CAL");
                    //SensorData dataAccelZ = objectCluster.GetData(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_Z, "CAL");
                    //SensorData dataECG
                    RunOnUiThread(() => tvAccelX.Text = "AccelX: " + dataAccelX.Data); 
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
            timer.Interval = 1000;
            timer.Elapsed += OnTimedEvent;
            timer.Enabled = true;
            
        }

        private void OnTimedEvent(object sender, System.Timers.ElapsedEventArgs e)
        {
            var listMeasurements = Application.Context.GetSharedPreferences("listMeasurements", FileCreationMode.Private);
            string idDevice = Android.OS.Build.Serial;

        }
        
        private void SaveData(ObjectCluster objectCluster)
        {
            // Format data with JSON-LD -> SAREF:Measurements
            // TODO: GetMeasurementItem only (on save) -> (on timer) create header (graphs), idea - multiple measurements 1 header per message to the cloud (timer)
            string data = string.Empty; // FormatJSONLD(objectCluster);

            List<JObject> sarefMakesMeasurementItemList = FormatMakesMeasurementItemList(objectCluster);
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

            
        }

        private JObject GetContextJSON()
        {
            JObject contextJSON = JObject.FromObject(new
            {
                InterIoTMsg = "http://inter-iot.eu/message/",
                InterIoT = "http://inter-iot.eu/",
                sarefInst = "https://w3id.org/saref/instances/",
                schema = "http://schema.org/",
                qu = "http://purl.oclc.org/NET/ssnx/qu/qu#",
                owl = "http://www.w3.org/2002/07/owl#",
                saref = "https://w3id.org/saref#",
                xsd = "http://www.w3.org/2001/XMLSchema#",
                skos = "http://www.w3.org/2004/02/skos/core#",
                dim = "http://purl.oclc.org/NET/ssnx/qu/dim#",
                rdfs = "http://www.w3.org/2000/01/rdf-schema#",
                dct = "http://purl.org/dc/terms/",
                rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#",
                xml = "http://www.w3.org/XML/1998/namespace",
                dcterms = "http://purl.org/dc/terms/",
                time = "http://www.w3.org/2006/time#",
                foaf = "http://xmlns.com/foaf/0.1/",
                om = "http://www.wurvoc.org/vocabularies/om-1.8/",
                geo = "http://www.w3.org/2003/01/geo/wgs84_pos#"

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

        private List<JObject> GetSarefMakesMeasurementItemsJSON(ObjectCluster objectCluster)
        {
            List<JObject> sarefMakesMeasurementItemList = new List<JObject>();
            List<Double> data = objectCluster.GetData();
            List<String> dataNames = objectCluster.GetNames();
            List<String> dataUnits = objectCluster.GetUnits();

            double timestamp = data[dataNames.IndexOf("System Timestamp")];

            for (int i = 0; i < data.Count; i++)
            {
                Double dataValue = data[i];
                String dataName = dataNames[i];
                String dataUnit = dataUnits[i];

                SAREFmessage msg = TranslateMeasurement(dataName, dataUnit);
                msg.HasTimestamp = timestamp;

                JObject sarefMakesMeasurementItemJSON = JObject.FromObject(new
                {
                    @id = msg.Id,
                    @type = msg.Type,
                    @label = msg.Label,
                    saref_hasTimestamp = msg.HasTimestamp,
                    saref_hasValue = dataValue,
                    saref_isMeasuredIn = msg.IsMeasuredIn,
                    saref_relatesToProperty = msg.RelatesToProperty
                });

                sarefMakesMeasurementItemList.Add(sarefMakesMeasurementItemJSON);

            }
            
            return sarefMakesMeasurementItemList;
        }

        private string GeneratePID(SAREFmessage msg)
        {
            string result = "sarefInst:SpeedMeasurement__Test.1.1_1511466006.9682777"; // string.Empty;

            result = msg.Label
           
            return result;
        }

        private SAREFmessage TranslateMeasurement(string dataName, string dataUnit)
        {
            SAREFmessage msg = new ShimmerCaptureXamarin.SAREFmessage();
            msg.IsMeasuredIn = "saref:SpeedUnit_MeterPerSecond";
            msg.RelatesToProperty = "saref:VelocityOrSpeed_Vehicle";
            msg.Label = "Example of a speed measurement observed by a mobile device";
            msg.Type = "saref:SpeedMeasurement";

            switch (dataName)
            {
                case "EXG2 CH2":
                    
                    if (dataUnit == "")
                    {
                    }

                    break;
                case "EXG2 CH2||EXG2 CH2||EXG2 CH1||EXG2 CH1||EXG2 Sta||EXG1 CH2||EXG1 CH2||EXG1 CH1||EXG1 CH1||EXG1 Status|":
                    break;
                case "Low Noise Accelerometer Z||Low Noise Accelerometer Z||Low Noise Accelerometer Y||Low Noise Accelerometer Y||Low Noise Accelerometer X||Low Noise Accelerometer X|":
                    break;
                default:
                    break;
            }

            msg.Id = GeneratePID(msg);

            return msg;
        }


        private JObject GetGraph2JSON(JObject geoLocationJSON, List<JObject> sarefMakesMeasurementItemList)
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
                geo_location = geoLocationJSON,
                saref_makesMeasurement = sarefMakesMeasurementItemList

            });
            return graph2JSON;
        }

        private List<JObject> FormatMakesMeasurementItemList(ObjectCluster objectCluster)
        {
            List<JObject> sarefMakesMeasurementItemsJSON = GetSarefMakesMeasurementItemsJSON(objectCluster);
            
            return sarefMakesMeasurementItemsJSON;
        }

        private string FormatJSONLD(ObjectCluster objectCluster)
        {
            JObject contextJSON = GetContextJSON();            
            JObject graph1JSON = GetGraph1JSON();

            JObject geoLocationJSON = GetGeoLocationJSON();
            
            List<JObject> sarefMakesMeasurementItemList = new List<JObject>();
            List<JObject> sarefMakesMeasurementItemsJSON = GetSarefMakesMeasurementItemsJSON(objectCluster);
            //sarefMakesMeasurementItemList.Add(sarefMakesMeasurementItemsJSON);

            JObject graph2JSON = GetGraph2JSON(geoLocationJSON, sarefMakesMeasurementItemList);

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


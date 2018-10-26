using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using INTERIoTEWS.Context.DataObjects.SOSA;
using INTERIoTEWS.SituationIdentificationManager.SituationIdentificationREST.Util;
using INTERIoTEWS.UIprototype.SemanticTranslationsValidation;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Parsing;
using VDS.RDF.Query;

namespace INTERIoTEWS.UIprototype
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            LoadMap();
            //PlotTripPoint();
            //PlotTripRoute();

            comboBox1.SelectedIndex = 0;
            comboBox1_SelectedIndexChanged(null, null);
        }

        private const double defaultLatitude = 52.2390134;
        private const double defaultLongitude = 6.857026;

        private void LoadMap()
        {
            //gmap.MapProvider = GMap.NET.MapProviders.BingMapProvider.Instance;
            gmap.MapProvider = GMap.NET.MapProviders.GoogleMapProvider.Instance;
            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerAndCache;
            gmap.SetPositionByKeywords("Enschede");
            //gmap.Position = new GMap.NET.PointLatLng(48.8589507, 2.2775175);
            gmap.Position = new GMap.NET.PointLatLng(defaultLatitude, defaultLongitude);
            gmap.ShowCenter = false;
            gmap.CanDragMap = true;
            gmap.DragButton = MouseButtons.Left;
            gmap.OnMarkerClick += new MarkerClick(gmap_OnMarkerClick);
        }

        private void gmap_OnMarkerClick(GMapMarker item, MouseEventArgs e)
        {
            textBox4.Text = item.Position.Lat + "," + item.Position.Lng + "   " + item.Tag;
        }

        private void PlotTripPoint()
        {
            GMapOverlay markers = new GMapOverlay("markers");
            GMapMarker marker = new GMarkerGoogle(
                new PointLatLng(52.2390134, 6.857026),
                GMarkerGoogleType.blue_pushpin);
            /*
            marker.ToolTip.Fill = Brushes.Black;
            marker.ToolTip.Foreground = Brushes.White;
            marker.ToolTip.Stroke = Pens.Black;
            marker.ToolTip.TextPadding = new Size(20, 20);
            */
            markers.Markers.Add(marker);
            gmap.Overlays.Add(markers);
        }

        private void PlotTripRoute()
        {
            GMapOverlay routes = new GMapOverlay("routes");
            List<PointLatLng> points = new List<PointLatLng>();
            points.Add(new PointLatLng(52.2390134, 6.857026));
            points.Add(new PointLatLng(52.23910161, 6.85700639));
            points.Add(new PointLatLng(52.23904452, 6.85698334));
            points.Add(new PointLatLng(52.23812652, 6.858351));
            points.Add(new PointLatLng(52.23408247, 6.86049936));
            GMapRoute route = new GMapRoute(points, "Test X.X.X");
            route.Stroke = new Pen(Color.Red, 3);
            routes.Routes.Add(route);
            gmap.Overlays.Add(routes);
        }
        
        private TripPointHelper ReadData()
        {
            //string folderPath = @"D:\Projects\InterIOT\Workplan\WP2-INTER-IoT-EWS\data\T1.1.2";
            string folderPath = textBox2.Text;
            DirectoryInfo info = new DirectoryInfo(folderPath);
            FileInfo[] files = info.GetFiles().OrderBy(p => p.Name).ToArray();

            textBox1.Text = "files count:" + files.Count() + Environment.NewLine;
            
            int i = 0;
            TripPointHelper = null;
            double lastHeartRate = 80;

            foreach (FileInfo file in files)
            {
                /*
                JObject jObject = JObject.Parse(file.OpenText().ToString());
                textBox1.Text += jObject["label"] + Environment.NewLine;
                */
                bool fileIsHealthData = false;
                textBox1.Text += "file: " + file.Name + Environment.NewLine;

                var jsonLdParser = new JsonLdParser();
                TripleStore tStore = new TripleStore();
                string contents = File.ReadAllText(file.FullName);
                using (var reader = new System.IO.StringReader(contents))
                {
                    jsonLdParser.Load(tStore, reader);
                }

                string sparqlQuery = sparqlQuery01;

                Object results = tStore.ExecuteQuery(sparqlQuery);

                //SparqlQuery query = sparqlparser.ParseFromString(sparqlQuery);
                //Object results = processor.ProcessQuery(query);

                Point point = null;

                if (results is SparqlResultSet)
                {
                    SparqlResultSet rset = (SparqlResultSet)results;
                    textBox1.Text += "query1 count:" + rset.Count + Environment.NewLine;

                    foreach (SparqlResult spqlResult in rset)
                    {
                        fileIsHealthData = true;
                        LiteralNode latValueNode = (LiteralNode)spqlResult.Value("lat");
                        double lat = latValueNode.AsValuedNode().AsDouble();
                        LiteralNode lonValueNode = (LiteralNode)spqlResult.Value("long");
                        double lon = lonValueNode.AsValuedNode().AsDouble();
                        LiteralNode measurementTimeNode = (LiteralNode)spqlResult.Value("measTime");
                        DateTime measDateTime = DateTime.Parse(measurementTimeNode.Value);
                        string deviceId = spqlResult["device"].ToString();
                        string sesorId = spqlResult["sensor"].ToString();
                        LiteralNode Acceleration_Average_AxisX = (LiteralNode)spqlResult.Value("Acceleration_Average_AxisX");
                        double acceleration_Average_AxisX = Acceleration_Average_AxisX.AsValuedNode().AsDouble();
                        LiteralNode Acceleration_Average_AxisY = (LiteralNode)spqlResult.Value("Acceleration_Average_AxisY");
                        double acceleration_Average_AxisY = Acceleration_Average_AxisY.AsValuedNode().AsDouble();
                        LiteralNode Acceleration_Average_AxisZ = (LiteralNode)spqlResult.Value("Acceleration_Average_AxisZ");
                        double acceleration_Average_AxisZ = Acceleration_Average_AxisZ.AsValuedNode().AsDouble();

                        //textBox1.Text += "device: " + deviceId + ", sensor: " + sesorId + ", measTime: " + measDateTime.ToString("o") + ", lat: " + lat + ", lon: " + lon + Environment.NewLine;
                        //textBox1.Text += "sensor: " + sesorId + ", measTime: " + measDateTime.ToString("o") + ", lat: " + lat + ", lon: " + lon + ", accelX: " + acceleration_Average_AxisX + ", accelY: " + acceleration_Average_AxisY + ", accelZ: " + acceleration_Average_AxisZ + Environment.NewLine;

                        point = new UIprototype.Point(lat, lon, measDateTime, acceleration_Average_AxisX, acceleration_Average_AxisY, acceleration_Average_AxisZ, contents);
                        point.OriginalFileName = file.Name;
                        TripPointHelper.TripPoint.Add(i, point);
                        i++;
                        break;
                    }
                }

                sparqlQuery = sparqlQuery03;
                results = tStore.ExecuteQuery(sparqlQuery);

                if (results is SparqlResultSet && point != null)
                {
                    SparqlResultSet rset = (SparqlResultSet)results;
                    textBox1.Text += "query3 count:" + rset.Count + Environment.NewLine;

                    foreach (SparqlResult spqlResult in rset)
                    {
                        fileIsHealthData = true;
                        string deviceECG = spqlResult["deviceECG"].ToString();
                        LiteralNode Acceleration_Average_AxisX = (LiteralNode)spqlResult.Value("measXValue");
                        double acceleration_Average_AxisX = Acceleration_Average_AxisX.AsValuedNode().AsDouble();
                        LiteralNode Acceleration_Average_AxisY = (LiteralNode)spqlResult.Value("measYValue");
                        double acceleration_Average_AxisY = Acceleration_Average_AxisY.AsValuedNode().AsDouble();
                        LiteralNode Acceleration_Average_AxisZ = (LiteralNode)spqlResult.Value("measZValue");
                        double acceleration_Average_AxisZ = Acceleration_Average_AxisZ.AsValuedNode().AsDouble();

                        if (point != null)
                        {
                            point.AccelerationX_ECG = acceleration_Average_AxisX;
                            point.AccelerationY_ECG = acceleration_Average_AxisY;
                            point.AccelerationZ_ECG = acceleration_Average_AxisZ;
                        }

                        LiteralNode heartRateLiteral = (LiteralNode)spqlResult.Value("measHRValue");
                        double heartRate = heartRateLiteral.AsValuedNode().AsDouble();
                        if (heartRate < 0)
                        {
                            heartRate = lastHeartRate;
                        }
                        point.HeartRate = heartRate;
                        lastHeartRate = heartRate;

                        LiteralNode batteryLevelLiteral = (LiteralNode)spqlResult.Value("measBatteryECGValue");
                        double batteryLevel = batteryLevelLiteral.AsValuedNode().AsDouble();
                        point.BatteryLevelECG = batteryLevel;
                        break;
                    }
                }

                sparqlQuery = sparqlQuery04;
                results = tStore.ExecuteQuery(sparqlQuery);

                if (results is SparqlResultSet && point != null)
                {
                    SparqlResultSet rset = (SparqlResultSet)results;
                    textBox1.Text += "query4 count:" + rset.Count + Environment.NewLine;

                    foreach (SparqlResult spqlResult in rset)
                    {
                        fileIsHealthData = true;
                        LiteralNode measurementTimeNode = (LiteralNode)spqlResult.Value("measTime");
                        DateTime measDateTime = DateTime.Parse(measurementTimeNode.Value);
                        LiteralNode measCrossAxialFunctionValueLiteral = (LiteralNode)spqlResult.Value("measCrossAxialFunctionValue");
                        double measCrossAxialFunctionValue = measCrossAxialFunctionValueLiteral.AsValuedNode().AsDouble();

                        point.VehicleCollisionDetected = true;
                        point.VehicleCollisionDateTime = measDateTime;
                        point.VehicleCollisionCrossAxial = measCrossAxialFunctionValue;
                        break;
                    }
                }

                if (!fileIsHealthData)
                {
                    point = new UIprototype.Point(0, 0, DateTime.UtcNow, 0, 0, 0, contents);
                    point.OriginalFileName = file.Name;
                    point.HeartRate = double.MaxValue;
                    TripPointHelper.TripPoint.Add(i, point);
                    i++;
                }
            }
            
            return TripPointHelper;

        }
        
        private void PlotTripRoute(TripPointHelper tripPointHelper)
        {
            GMapOverlay routes = new GMapOverlay("routes");
            List<PointLatLng> points = new List<PointLatLng>();

            //textBox1.Text = string.Empty;
            Point lastPoint = null;
            Point firstPoint = null;
            var list = tripPointHelper.TripPoint.Keys.ToList();
            list.Sort();
            Dictionary<string, double> repeatedPoints = new Dictionary<string, double>();
            foreach (int i in list)
            {
                Point point = tripPointHelper.TripPoint[i];
                string latlon = point.Latitude + "_" + point.Longitude;
                if (repeatedPoints.ContainsKey(latlon))
                    repeatedPoints[latlon]++;
                else
                    repeatedPoints.Add(latlon, 1);

                PointLatLng pointLatLng = new PointLatLng(point.Latitude, point.Longitude);

                // check if the same lat/lon was already plotted (device not in movment)
                // check if point is out of range from the prior point
                if (!ContainsPoint(pointLatLng, repeatedPoints) && IsInTheRangeOfPosition(point, lastPoint))
                {
                    points.Add(pointLatLng);
                    AddMarker(point, GMarkerGoogleType.red_small, "file:" + point.OriginalFileName);
                    //textBox1.Text += "[" + point.DateTimeMessageSent.ToString("o") + "] Lat:" + point.Latitude + " |Lon:" + point.Longitude + Environment.NewLine;

                    if (lastPoint == null)
                    {
                        AddMarker(point, GMarkerGoogleType.green_big_go, "Start");
                        firstPoint = point;
                    }

                    lastPoint = point;
                }

                if (point.VehicleCollisionDetected)
                {
                    AddMarker(point, GMarkerGoogleType.red_big_stop, "Collision! " + point.VehicleCollisionDateTime.ToLongTimeString() + "_" + point.VehicleCollisionCrossAxial + "_"  + point.OriginalFileName);
                }
            }

            var moreThanOnePoint = repeatedPoints.Where(x => x.Value > 1).ToList();
            textBox1.Text += "Points repeated: " + moreThanOnePoint.Count;

            AddMarker(lastPoint, GMarkerGoogleType.yellow_big_pause, "End");

            GMapRoute route = new GMapRoute(points, comboBox1.SelectedItem.ToString());
            route.Stroke = new Pen(Color.Red, 3);
            routes.Routes.Add(route);
            gmap.Overlays.Add(routes);
            gmap.UpdateRouteLocalPosition(route);

            if (firstPoint != null)
            {
                gmap.Position = new GMap.NET.PointLatLng(firstPoint.Latitude, firstPoint.Longitude);
            }
            //gmap.Update();
            //gmap.Overlays[0].IsVisibile = false;
            //gmap.Overlays[0].IsVisibile = true;
        }

        private bool ContainsPoint(PointLatLng pointLatLng, Dictionary<string, double> repeatedPoints)
        {
            if (repeatedPoints.ContainsKey(pointLatLng.Lat + "_" + pointLatLng.Lng))
                if (repeatedPoints[pointLatLng.Lat + "_" + pointLatLng.Lng] > 1)
                    return true;

            return false;
        }

        private bool IsInTheRangeOfPosition(PointLatLng pointCurrent, PointLatLng pointLast)
        {
            return IsInTheRangeOfPosition(new Point(pointCurrent.Lat, pointCurrent.Lng, DateTime.Now,0,0,0,""), new Point(pointLast.Lat, pointLast.Lng, DateTime.Now, 0, 0, 0, ""));
        }


        private bool IsInTheRangeOfPosition(Point pointCurrent, Point pointLast)
        {
            if (pointCurrent.Latitude == 0 || pointCurrent.Longitude == 0)
                return false;
            return true;
            if (pointLast != null)
            {
                double threshold = 0.0007;
                if ((Math.Abs(pointCurrent.Latitude - pointLast.Latitude) > threshold)
                    || (Math.Abs(pointCurrent.Longitude - pointLast.Longitude) > threshold))
                    return false;
            }
            
            return true;
        }

        private void AddMarker(Point point, GMarkerGoogleType markerType, string tooltipText)
        {
            if (point == null)
                return;

            GMapOverlay markers = new GMapOverlay("markers");
            GMapMarker marker = new GMarkerGoogle(new PointLatLng(point.Latitude, point.Longitude), markerType);

            if (tooltipText.StartsWith("file:"))
            {
                marker.Tag = tooltipText;
            }
            else if (tooltipText != string.Empty)
            {
                marker.ToolTipMode = MarkerTooltipMode.OnMouseOver;
                marker.ToolTipText = tooltipText;
            }
            

            markers.Markers.Add(marker);
            gmap.Overlays.Add(markers);
            gmap.UpdateMarkerLocalPosition(marker);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            gmap.Overlays.Clear();

            ReadData();
            
            PlotTripRoute(tripPointHelper);
            PlotCharts(tripPointHelper);
            
        }
        

        const double Gforce = 9.806; // m/s2 

        private void PlotCharts(TripPointHelper tripPointHelper)
        {
            chart1.Series[0].Points.Clear();
            chart1.Series[1].Points.Clear();
            chart1.Series[2].Points.Clear();

            chart2.Series[0].Points.Clear();

            chart3.Series[0].Points.Clear();
            chart3.Series[1].Points.Clear();

            chart4.Series[0].Points.Clear();

            for (int i = 0; i < tripPointHelper.TripPoint.Count; i++)
            {
                Point point = tripPointHelper.TripPoint[i];

                if (point.HeartRate == double.MaxValue)
                    continue;

                // Acceleration
                chart1.Series[0].Points.AddXY(i + 1, Math.Abs(point.AccelerationX));
                chart1.Series[1].Points.AddXY(i + 1, Math.Abs(point.AccelerationY));
                chart1.Series[2].Points.AddXY(i + 1, Math.Abs(point.AccelerationZ) - Gforce);

                if (point.AccelerationX_ECG != double.MinValue)
                {
                    chart1.Series[3].Points.AddXY(i + 1, Math.Abs(point.AccelerationX_ECG));
                    chart1.Series[4].Points.AddXY(i + 1, Math.Abs(point.AccelerationY_ECG));
                    chart1.Series[5].Points.AddXY(i + 1, Math.Abs(point.AccelerationZ_ECG) - Gforce);
                }

                chart1.Series[0].Points[chart1.Series[0].Points.Count-1].AxisLabel = point.DateTimeMessageSent.ToLongTimeString();

                // Heart rate
                chart2.Series[0].Points.AddXY(i + 1, Math.Abs(point.HeartRate));
                chart2.Series[0].Points[chart2.Series[0].Points.Count-1].AxisLabel = point.DateTimeMessageSent.ToLongTimeString();

                // Cross-Axial Function
                double crossAxialComputedHere_Mobile = ComputeCrossAxialFunction(point.AccelerationX, point.AccelerationY, point.AccelerationZ);
                chart3.Series[0].Points.AddXY(i + 1, crossAxialComputedHere_Mobile);

                if (point.AccelerationX_ECG != double.MinValue)
                {
                    double crossAxialComputedHere_ECG = ComputeCrossAxialFunction(point.AccelerationX_ECG, point.AccelerationY_ECG, point.AccelerationZ_ECG);
                    chart3.Series[1].Points.AddXY(i + 1, crossAxialComputedHere_ECG);
                }

                if (point.VehicleCollisionDetected)
                {
                    if (chart3.Series.Count == 2)
                    {
                        Series collisionDetectedFromECG = new Series("Computed when deteced");
                        collisionDetectedFromECG.ChartType = SeriesChartType.Point;
                        collisionDetectedFromECG.Points.AddXY(i + 1, point.VehicleCollisionCrossAxial);
                        chart3.Series.Add(collisionDetectedFromECG);
                    }
                    else
                    {
                        chart3.Series[2].Points.AddXY(i + 1, point.VehicleCollisionCrossAxial);
                    }
                    //chart3.Series[2].Points[i].AxisLabel = point.VehicleCollisionDateTime.ToLongTimeString();
                }

                // Battery Level
                if (point.BatteryLevelECG != double.MinValue)
                {
                    chart4.Series[1].Points.AddXY(i + 1, point.BatteryLevelECG);
                    chart4.Series[1].Points[chart4.Series[1].Points.Count-1].AxisLabel = point.DateTimeMessageSent.ToLongTimeString();
                }
            }

            chart1.ChartAreas[0].AxisX.Interval = tripPointHelper.TripPoint.Count/5;
            chart1.ChartAreas[0].AxisX.TextOrientation = System.Windows.Forms.DataVisualization.Charting.TextOrientation.Rotated270;
            chart1.Update();

            chart2.ChartAreas[0].AxisX.Interval = tripPointHelper.TripPoint.Count / 5;
            chart2.Update();

            chart3.ChartAreas[0].AxisX.Interval = tripPointHelper.TripPoint.Count / 5;
            chart3.Update();

            chart4.ChartAreas[0].AxisX.Interval = tripPointHelper.TripPoint.Count / 5;
            chart4.Update();
        }

        private double ComputeCrossAxialFunction(double accelerationX, double accelerationY, double accelerationZ)
        {
            return Math.Pow(accelerationX, 2) + Math.Pow(accelerationY, 2) + Math.Pow(accelerationZ-Gforce, 2);
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string folderPath = @"D:\Projects\InterIOT\Workplan\WP2-INTER-IoT-EWS\data\";

            if (comboBox1.SelectedIndex >-1)
            {
                string folder = comboBox1.SelectedItem.ToString().Split(':').ToArray()[0];
                textBox2.Text = folderPath + folder;
            }

        }

        private void gmap_Load(object sender, EventArgs e)
        {

        }

        private void chart2_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (countTestCasesExecuted == 0)
                DisableOverlapingSituationTypes();

            Form2 f = new Form2();
            f.comboBox1.SelectedItem = comboBox1.SelectedItem;
            f.button1.Enabled = false;
            f.button2.Enabled = false;

            if (radioButton3.Checked)
            {
                f.ExecuteTestCaseWithINTER_MW(this.TripPointHelper, int.Parse(textBox3.Text));
            }
            else
                f.ExecuteTestCase(this.TripPointHelper, int.Parse(textBox3.Text));


            f.ShowDialog();
        }

        public void ExecuteTestCaseWithINTER_MW(TripPointHelper tripPointHelper_in, int threadSleep)
        {
            CreateOutputFolder();

            this.TripPointHelper = tripPointHelper_in;

            //new Task(() => { SendDataToAzureIoTHub(threadSleep); }).Start();

            // Test 01) send data directly to ContextManager acting as IoT Hub

            string folderPath = textBox2.Text;
            DirectoryInfo info = new DirectoryInfo(folderPath);
            FileInfo[] files = info.GetFiles().OrderBy(p => p.Name).ToArray();

            SetText("[ExecuteTestCaseWithINTER_MW] files count:" + files.Count() + Environment.NewLine, false);
            foreach (FileInfo file in files)
            {
                string contents = File.ReadAllText(file.FullName);
                JObject jObject = JObject.Parse(contents);
                
                string url = "https://inter-iot-ews-contextmanagerrest-v0.azurewebsites.net/api/trip/";

                if (RunningLocal)
                    url = "http://localhost:53268/api/trip/";

                var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(jObject.ToString());
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var responseText = streamReader.ReadToEnd();
                }

                //break;
                SetText("Data sent: " + file.Name, true);

            }
        }

        private TripPointHelper tripPointHelper;

        public TripPointHelper TripPointHelper
        {
            get
            {
                if (tripPointHelper == null)
                    tripPointHelper = new TripPointHelper();
                return tripPointHelper;
            }

            set
            {
                tripPointHelper = value;
            }
        }

        public void ExecuteTestCase(TripPointHelper tripPointHelper_in, int threadSleep)
        {
            CreateOutputFolder();

            this.TripPointHelper = tripPointHelper_in;
            new Task(() => { SendDataToAzureIoTHub(threadSleep); }).Start();
        }

        private void CreateOutputFolder()
        {
            string filePathBasics = GetTextBox2() + @"\output\";
            System.IO.Directory.CreateDirectory(filePathBasics);

            string[] subdirEntries = Directory.GetDirectories(filePathBasics);
            string subFolder = "Execution_";
            int i = subdirEntries.Length + 1;
            subFolder = subFolder + i.ToString().PadLeft(3, '0') + "_" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + @"\";

            filePathBasics = filePathBasics + subFolder;
            System.IO.Directory.CreateDirectory(filePathBasics);
            
            outputFolderPath = filePathBasics;
        }

        static Microsoft.Azure.Devices.Client.DeviceClient deviceClient;

        //static string iotHubUri = "INTER-IoT-EWS-hub-b1.azure-devices.net";
        static string iotHubUri = "NTER-IoT-EWS-hub-02.azure-devices.net";
        
        static string deviceKey = "XXXXXXXXXXXX";
        static string deviceId = "XXXXXXXXXXX";

        public void SendDataToAzureIoTHub(int threadSleep)
        {
            MessageBox.Show("SendDataToAzureIoTHub: start");
            new Task(() => { SimulateINTERIoT_MW(); }).Start();
            // Wait a bit until the app is ready to read from IoT Hub
            Thread.Sleep(5000);

            if (deviceClient == null)
                deviceClient = Microsoft.Azure.Devices.Client.DeviceClient.Create(iotHubUri, new Microsoft.Azure.Devices.Client.DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey), Microsoft.Azure.Devices.Client.TransportType.Http1);

            for (int i = 0; i < this.TripPointHelper.TripPoint.Keys.Count(); i++)
            // Some issue going on with the order...
            //for (int i = this.TripPointHelper.TripPoint.Keys.Count()-1; i >= 0; i--)
            {
                Thread.Sleep(threadSleep);

                Point point = this.TripPointHelper.TripPoint[i];
                var messageString = point.OriginalFileContent;
                var message = new Microsoft.Azure.Devices.Client.Message(Encoding.ASCII.GetBytes(messageString));

                deviceClient.SendEventAsync(message);

            }
            MessageBox.Show("SendDataToAzureIoTHub: end");
            countTestCasesExecuted++;
        }

        // To listen directly from Azure IoT Hub and call PUT /api/deviceobservations/{deviceId}
        static string connectionString = "XXXXXX";

        static string iotHubD2cEndpoint = "messages/events";
        static EventHubClient eventHubClient;

        bool isConnectedToIoTHub = false;
        public void SimulateINTERIoT_MW()
        {
            eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, iotHubD2cEndpoint);

            var d2cPartitions = eventHubClient.GetRuntimeInformation().PartitionIds;

            CancellationTokenSource cts = new CancellationTokenSource();

            var tasks = new List<Task>();
            foreach (string partition in d2cPartitions)
            {
                if (!isConnectedToIoTHub)
                {
                    SetText("Connected to IoT Hub" + Environment.NewLine, true);
                    isConnectedToIoTHub = true;
                }
                tasks.Add(ReceiveMessagesFromIoTHub(partition, cts.Token));
            }
            Task.WaitAll(tasks.ToArray());
        }

        private async Task ReceiveMessagesFromIoTHub2(string partition, CancellationToken ct)
        {
            var eventHubReceiver = eventHubClient.GetDefaultConsumerGroup().CreateReceiver(partition, DateTime.UtcNow);
            while (true)
            {
                if (ct.IsCancellationRequested) break;
                EventData eventData = await eventHubReceiver.ReceiveAsync();
                if (eventData == null) continue;

                //SetText("Message received" + Environment.NewLine, true);

                string data = Encoding.UTF8.GetString(eventData.GetBytes());
                //Console.WriteLine("Message received. Partition: {0} Data: '{1}'", partition, data);

                JObject messageJson = JObject.Parse(data);
                if (messageJson["@type"] != null)
                {
                    string domain = "other";
                    switch (messageJson["@type"].ToString())
                    {
                        case "saref:Device":
                            domain = "health";

                            //new Task(() => { PlotRoutePointFromMessage(data); }).Start();

                            break;
                        case "edxl_cap:AlertMessage":
                        case "edxl_de:EDXLDistribution":
                            domain = "emergency";

                            //new Task(() => { PlotMarkerFromEmergencyMessage(data); }).Start();

                            break;
                        case "LogiTrans:TransportEvent":
                            domain = "logistics";
                            break;
                        default:

                            break;
                    }

                    SetText("Message received, type: " + domain + Environment.NewLine, true);


                    new Task(() => { SaveFile(domain, data); }).Start();


                }

            }
        }



        bool firstMessageArrived = false;
        private async Task ReceiveMessagesFromIoTHub(string partition, CancellationToken ct)
        {
            var eventHubReceiver = eventHubClient.GetDefaultConsumerGroup().CreateReceiver(partition, DateTime.UtcNow);
            while (true)
            {
                if (ct.IsCancellationRequested) break;
                EventData eventData = await eventHubReceiver.ReceiveAsync();
                if (eventData == null) continue;

                /*
                if (!firstMessageArrived)
                {
                    SetText("First message arrived", true);
                    firstMessageArrived = true;
                }
                */
                SetText("Message received" + Environment.NewLine, true);

                string data = Encoding.UTF8.GetString(eventData.GetBytes());
                Console.WriteLine("Message received. Partition: {0} Data: '{1}'", partition, data);

                // Save in MongoDB
                //mongoDB.SaveDocument(data);

                JObject messageJson = JObject.Parse(data);
                if (messageJson["@type"] != null)
                {
                    string domain = "other";
                    switch (messageJson["@type"].ToString())
                    {
                        case "saref:Device":                            
                            domain = "health";

                            new Task(() => { PlotRoutePointFromMessage(data); }).Start();

                            break;
                        case "edxl_cap:AlertMessage":
                        case "edxl_de:EDXLDistribution":
                            domain = "emergency";

                            new Task(() => { PlotMarkerFromEmergencyMessage(data); }).Start();

                            break;
                        case "LogiTrans:TransportEvent":                            
                            domain = "logistics";
                            break;
                        default:
                            
                            break;
                    }

                    new Task(() => { SaveFile(domain, data); }).Start();


                }

            }
        }

        private void PlotMarkerFromEmergencyMessage(string data)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            int countLog = 0;
            //SetText("[" + countLog++ + "] Message arrived: " + comboBox1.SelectedItem.ToString() + Environment.NewLine, true);
            SetText("[" + countLog++ + "] Message arrived: " + Environment.NewLine, true);
            
            var jsonLdParser = new JsonLdParser();
            TripleStore tStore = new TripleStore();
            string contents = data;
            using (var reader = new System.IO.StringReader(contents))
            {
                jsonLdParser.Load(tStore, reader);
            }
            SetText("[" + countLog++ + "] After loading triplestore (deltaT): " + StopStartWatch(stopWatch) + Environment.NewLine, true);
            
            string sparqlQuery = sparqlQuery05;

            Object results = tStore.ExecuteQuery(sparqlQuery);

            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                
                SetText("[" + countLog++ + "] sparqlQuery05 (EDXL) count:" + rset.Count + " || DeltaT: " + StopStartWatch(stopWatch) + Environment.NewLine, true);

                foreach (SparqlResult spqlResult in rset)
                {
                    string urgency = spqlResult["urgency"].ToString();
                    string urgencyType = spqlResult["urgencyType"].ToString();
                    string severity = spqlResult["severity"].ToString();
                    string severityType = spqlResult["severityType"].ToString();
                    string area = spqlResult["area"].ToString();

                    LiteralNode pointLatLonLiteral = (LiteralNode)spqlResult.Value("pointLatLon");
                    string pointLatLon = pointLatLonLiteral.AsValuedNode().AsString();

                    pointLatLon = pointLatLon.Trim().Replace("Point(", string.Empty).Replace(")", string.Empty);
                    double lat = double.Parse(pointLatLon.Split(' ')[0].Trim().Replace(".", ",")); // TODO: can cause error... 
                    double lon = double.Parse(pointLatLon.Split(' ')[1].Trim().Replace(".", ","));

                    LiteralNode headlineL = (LiteralNode)spqlResult.Value("headline");
                    string headline = headlineL.AsValuedNode().AsString();
                    LiteralNode senderNameL = (LiteralNode)spqlResult.Value("senderName");
                    string senderName = senderNameL.AsValuedNode().AsString();
                    LiteralNode descriptionL = (LiteralNode)spqlResult.Value("description");
                    string description = descriptionL.AsValuedNode().AsString();
                    LiteralNode instructionL = (LiteralNode)spqlResult.Value("instruction");
                    string instruction = instructionL.AsValuedNode().AsString();
                    
                    SetText("[" + countLog++ + "] EDXL message.lat:" + lat + ", lon: " + lon + ", urgencyType: " + urgencyType + ", severityType: " + severityType + " || DeltaT: " + StopStartWatch(stopWatch) + Environment.NewLine, true);
                    
                    string toolTip = @"Detected from Situation Identifier!" + Environment.NewLine +
                        "Urgency: " + urgencyType + Environment.NewLine +
                        "Severity: " + severityType + Environment.NewLine +
                        "headline: " + headline + Environment.NewLine +
                        "senderName: " + senderName + Environment.NewLine +
                        "description: " + description + Environment.NewLine +
                        "instruction: " + instruction;

                    if (lat == 0 && lon == 0)
                    {
                        lat = defaultLatitude;
                        lon = defaultLongitude;
                    }

                    GMapOverlay markers = new GMapOverlay("markers");
                    GMapMarker marker = new GMarkerGoogle(new PointLatLng(lat, lon), GMarkerGoogleType.red_big_stop);
                    marker.ToolTipMode = MarkerTooltipMode.Always;
                    marker.ToolTipText = toolTip;
                    markers.Markers.Add(marker);
                    gmap.Overlays.Add(markers);

                    SetText("[" + countLog++ + "] After plotting || DeltaT: " + StopStartWatch(stopWatch) + Environment.NewLine, true);
                    
                    break;
                }
            }
        }

        private void PlotMarkerFromEmergencyMessage2(string data)
        {
            GMapOverlay routes = null;
            GMapRoute route = null;
            List<PointLatLng> points = null;
            PointLatLng lastPoint = new PointLatLng();
            
            if (gmap.Overlays.Count > 0)
            {
                routes = gmap.Overlays[0];
                route = (routes.Routes.Count > 0)? routes.Routes[0]: new GMapRoute("routes");
                points = route.Points;
                if (route.Points.Count > 0)
                    lastPoint = route.Points[route.Points.Count - 1];
            }
            else
            {
                routes = new GMapOverlay("routes");
                points = new List<PointLatLng>();
            }
            
            var jsonLdParser = new JsonLdParser();
            TripleStore tStore = new TripleStore();
            string contents = data;
            using (var reader = new System.IO.StringReader(contents))
            {
                jsonLdParser.Load(tStore, reader);
            }

            string sparqlQuery = sparqlQuery05;

            Object results = tStore.ExecuteQuery(sparqlQuery);
            
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                SetText("EDXL count:" + rset.Count + Environment.NewLine, true);

                foreach (SparqlResult spqlResult in rset)
                {
                    string urgency = spqlResult["urgency"].ToString();
                    string urgencyType = spqlResult["urgencyType"].ToString();
                    string severity = spqlResult["severity"].ToString();
                    string severityType = spqlResult["severityType"].ToString();
                    string area = spqlResult["area"].ToString();
                    
                    LiteralNode pointLatLonLiteral = (LiteralNode)spqlResult.Value("pointLatLon");
                    string pointLatLon = pointLatLonLiteral.AsValuedNode().AsString();

                    pointLatLon = pointLatLon.Trim().Replace("Point(", string.Empty).Replace(")", string.Empty);
                    double lat = double.Parse(pointLatLon.Split(' ')[0].Trim().Replace(".", ",")); // TODO: can cause error... 
                    double lon = double.Parse(pointLatLon.Split(' ')[1].Trim().Replace(".", ","));

                    LiteralNode headlineL = (LiteralNode)spqlResult.Value("headline");
                    string headline = headlineL.AsValuedNode().AsString();
                    LiteralNode senderNameL = (LiteralNode)spqlResult.Value("senderName");
                    string senderName = senderNameL.AsValuedNode().AsString();
                    LiteralNode descriptionL = (LiteralNode)spqlResult.Value("description");
                    string description = descriptionL.AsValuedNode().AsString();
                    LiteralNode instructionL = (LiteralNode)spqlResult.Value("instruction");
                    string instruction = instructionL.AsValuedNode().AsString();


                    SetText("EDXL message. lat:" + lat + ", lon:" + lon + ", urgencyType:" + urgencyType + ", severityType:" + severityType + Environment.NewLine, true);

                    string toolTip = @"Detected from Situation Identifier!" + Environment.NewLine +
                        "Urgency: " + urgencyType + Environment.NewLine +
                        "Severity: " + severityType + Environment.NewLine +
                        "headline: " + headline + Environment.NewLine +
                        "senderName: " + senderName + Environment.NewLine +
                        "description: " + description + Environment.NewLine +
                        "instruction: " + instruction;

                    if (lat == 0 || lon == 0)
                    {
                        lat = defaultLatitude;
                        lon = defaultLongitude;
                    }

                    GMapOverlay markers = new GMapOverlay("markers");
                    GMapMarker marker = new GMarkerGoogle(new PointLatLng(lat, lon), GMarkerGoogleType.red_big_stop);
                    marker.ToolTipMode = MarkerTooltipMode.Always;
                    marker.ToolTipText = toolTip;
                    markers.Markers.Add(marker);
                    gmap.Overlays.Add(markers);

                    break;
                }
            }
        }

        private string sparqlQuery01 = @"
                PREFIX saref: <https://w3id.org/saref#>
                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                PREFIX geo: <http://www.w3.org/2003/01/geo/wgs84_pos#>
                PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
                PREFIX sarefInst: <https://w3id.org/saref/instances#>


            SELECT ?device ?location ?lat ?long 
                   ?sensor ?measurement ?measTime
                   ?Acceleration_Average_AxisX ?Acceleration_Average_AxisY ?Acceleration_Average_AxisZ 
                WHERE  
	                {
					?device geo:location ?location.
	                ?location geo:lat ?lat.
	                ?location geo:long ?long.
                    
                    ?sensor saref:makesMeasurement ?measurementX.
	                ?measurementX saref:hasTimestamp ?measTime. 
	                ?measurementX saref:relatesToProperty sarefInst:Acceleration_Average_AxisX.
                    ?measurementX saref:hasValue ?Acceleration_Average_AxisX.
                    ?sensor saref:makesMeasurement ?measurementY.
	                ?measurementY saref:relatesToProperty sarefInst:Acceleration_Average_AxisY.
                    ?measurementY saref:hasValue ?Acceleration_Average_AxisY.
                    ?sensor saref:makesMeasurement ?measurementZ.
	                ?measurementZ saref:relatesToProperty sarefInst:Acceleration_Average_AxisZ.
                    ?measurementZ saref:hasValue ?Acceleration_Average_AxisZ.
	                } 
                ORDER BY ?measTime 

            ";
        // removed: ?device saref:consistsOf ?sensor. 

        private string sparqlQuery02 = @"
                PREFIX saref: <https://w3id.org/saref#>
                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                PREFIX geo: <http://www.w3.org/2003/01/geo/wgs84_pos#>
                PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
                PREFIX sarefInst: <https://w3id.org/saref/instances#>
                PREFIX saref4health: <https://w3id.org/def/saref4health#>


            SELECT ?device ?location ?lat ?long 
                   ?sensor ?measurement ?measTime
                   ?Acceleration_Average_AxisX ?Acceleration_Average_AxisY ?Acceleration_Average_AxisZ 
                   ?deviceECG
                WHERE
                {  
	                {
					?device geo:location ?location.
	                ?location geo:lat ?lat.
	                ?location geo:long ?long.
                    ?device saref:consistsOf ?sensor. 
                    ?sensor saref:makesMeasurement ?measurementX.
	                ?measurementX saref:hasTimestamp ?measTime. 
	                ?measurementX saref:relatesToProperty sarefInst:Acceleration_Average_AxisX.
                    ?measurementX saref:hasValue ?Acceleration_Average_AxisX.
                    ?sensor saref:makesMeasurement ?measurementY.
	                ?measurementY saref:relatesToProperty sarefInst:Acceleration_Average_AxisY.
                    ?measurementY saref:hasValue ?Acceleration_Average_AxisY.
                    ?sensor saref:makesMeasurement ?measurementZ.
	                ?measurementZ saref:relatesToProperty sarefInst:Acceleration_Average_AxisZ.
                    ?measurementZ saref:hasValue ?Acceleration_Average_AxisZ.
	                } 

                    UNION
                    
                    {
					?device saref:consistsOf ?deviceECG. 
                    ?deviceECG a saref4health:ECGDevice.
	                } 

                }
                ORDER BY ?measTime 

            ";
        /*
         ?deviceECG saref:consistsOf ?sensorECGLeadUnipolar.
                    ?sensorECGLeadUnipolar a saref4health:ECGLeadUnipolar.
                    ?sensor2 saref:makesMeasurement ?measurementX2.
	                ?measurementX2 saref:relatesToProperty sarefInst:Acceleration_Average_AxisX.
                    ?measurementX2 saref:hasValue ?Acceleration_Average_AxisX2.
             */
             private string sparqlQuery03 = @"
                PREFIX saref: <https://w3id.org/saref#>
                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                PREFIX geo: <http://www.w3.org/2003/01/geo/wgs84_pos#>
                PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
                PREFIX sarefInst: <https://w3id.org/saref/instances#>
                PREFIX saref4health: <https://w3id.org/def/saref4health#>


            SELECT ?device ?deviceECG ?sensor1 ?measX 
                    ?measXValue ?measYValue ?measZValue
                    ?measHRValue ?measBatteryECGValue
                WHERE
                    {
					?device saref:consistsOf ?deviceECG. 
                    ?deviceECG a saref4health:ECGDevice.
                    ?deviceECG saref:consistsOf ?sensor1. 
                    ?sensor1 saref:makesMeasurement ?measX.
                    ?measX saref:relatesToProperty sarefInst:Acceleration_Average_AxisX.
                    ?measX saref:hasValue ?measXValue.
                    ?sensor1 saref:makesMeasurement ?measY.
                    ?measY saref:relatesToProperty sarefInst:Acceleration_Average_AxisY.
                    ?measY saref:hasValue ?measYValue.
                    ?sensor1 saref:makesMeasurement ?measZ.
                    ?measZ saref:relatesToProperty sarefInst:Acceleration_Average_AxisZ.
                    ?measZ saref:hasValue ?measZValue.
                    ?deviceECG saref:consistsOf ?sensorProcessedHeartRate. 
                    ?sensorProcessedHeartRate saref:measuresProperty sarefInst:ProcessedHeartRate.
                    ?sensorProcessedHeartRate saref:makesMeasurement ?measHR.
                    ?measHR saref:hasValue ?measHRValue.
                    ?deviceECG saref:consistsOf ?sensorBattery. 
                    ?sensorBattery saref:measuresProperty sarefInst:BatteryLevel.
                    ?sensorBattery saref:makesMeasurement ?measBatteryECG.
                    ?measBatteryECG saref:hasValue ?measBatteryECGValue
	                } 

                
                ORDER BY ?measTime 

            ";

        private string sparqlQuery04 = @"
                PREFIX saref: <https://w3id.org/saref#>
                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                PREFIX geo: <http://www.w3.org/2003/01/geo/wgs84_pos#>
                PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
                PREFIX sarefInst: <https://w3id.org/saref/instances#>


            SELECT ?measTime ?measCrossAxialFunctionValue
                WHERE  
	                {
					?device saref:consistsOf ?sensor. 
                    ?sensor saref:measuresProperty sarefInst:ProcessedAccelerometer.
                    ?sensor saref:makesMeasurement ?measVehicleCollisionDetected.
	                ?measVehicleCollisionDetected saref:relatesToProperty sarefInst:VehicleCollisionDetectedFromECGDeviceAccelerometerComputedByMobile.
                    ?measVehicleCollisionDetected saref:hasValue ?valueVehicleCollisionDetected.
                    ?measVehicleCollisionDetected saref:hasTimestamp ?measTime.
                    ?sensor saref:makesMeasurement ?measCrossAxialFunction.
                    ?measCrossAxialFunction saref:relatesToProperty sarefInst:CrossAxialFunction.
                    ?measCrossAxialFunction saref:hasValue ?measCrossAxialFunctionValue
                    FILTER(?valueVehicleCollisionDetected = 1)
	                } 

            ";


        private string sparqlQuery05 = @"
                PREFIX edxl_cap: <http://fpc.ufba.br/ontologies/edxl_cap#>
                PREFIX edxl_cap_inst: <http://fpc.ufba.br/ontologies/edxl_cap/instances#>
                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
                PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>


            SELECT ?alertMessage ?label
                    ?info 
                    ?urgency ?urgencyType
                    ?severity ?severityType
                    ?area ?pointLatLon
                    ?headline ?senderName ?description ?instruction
                WHERE  
	                {
					?alertMessage a edxl_cap:AlertMessage. 
                    ?alertMessage rdfs:label ?label.
                    ?alertMessage edxl_cap:hasInfo ?info.
                    ?info edxl_cap:hasUrgency ?urgency.
                    ?urgency a ?urgencyType.
                    ?info edxl_cap:hasSeverity ?severity.
                    ?severity a ?severityType.
                    ?info edxl_cap:hasArea ?area.
                    ?area edxl_cap:areaDesc ?pointLatLon.
                    ?info edxl_cap:headline ?headline.
                    ?info edxl_cap:senderName ?senderName.
                    ?info edxl_cap:description ?description.
                    ?info edxl_cap:instruction ?instruction.
                    FILTER (?urgencyType != edxl_cap:Urgency && ?severityType != edxl_cap:Severity)
	                } 

            ";

        /*
         
            SELECT ?alertMessage ?label 
                    ?urgency ?urgencyType 
                    ?severity ?severityType
                    ?pointLatLon
                WHERE  
	                {
					?alertMessage a edxl_cap:AlertMessage. 
                    ?alertMessage rdfs:label ?label.
                    ?alertMessage edxl_cap:hasInfo ?info.
                    ?info edxl_cap:hasUrgency ?urgency.
                    ?urgency a ?urgencyType.
                    ?info edxl_cap:hasSeverity ?severity.
                    ?severity a ?severityType.
                    ?info edxl_cap:hasArea ?area.
                    ?area edxl_cap:areaDesc ?pointLatLon.
	                } 

             */

        Dictionary<string, double> repeatedPointsFromMessage = new Dictionary<string, double>();

        private string StopStartWatch(Stopwatch stopWatch)
        {
            lock (stopWatch)
            {
                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                stopWatch.Start();
                return elapsedTime;
            }
        }

        private void PlotRoutePointFromMessage(string data)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            int countLog = 0;
            //SetText("[" + countLog++ + "] Message arrived: " + comboBox1.SelectedItem.ToString() + Environment.NewLine, true);
            SetText("[" + countLog++ + "] Message arrived: " + Environment.NewLine, true);

            PointLatLng lastPoint = new PointLatLng(999, 999);
            var jsonLdParser = new JsonLdParser();
            TripleStore tStore = new TripleStore();
            string contents = data;
            using (var reader = new System.IO.StringReader(contents))
            {
                jsonLdParser.Load(tStore, reader);
            }
            SetText("[" + countLog++ + "] After loading triplestore (deltaT): " + StopStartWatch(stopWatch) + Environment.NewLine, true);

            string sparqlQuery = sparqlQuery01;

            SparqlQueryParser sparqlparser = new SparqlQueryParser();
            SparqlQuery query = sparqlparser.ParseFromString(sparqlQuery);
            query.Timeout = 999999;

            //Object results = tStore.ExecuteQuery(sparqlQuery);
            Object results = null;

            try
            {
                results = tStore.ExecuteQuery(query);
            }
            catch (Exception ex)
            { }

            //SparqlQuery query = sparqlparser.ParseFromString(sparqlQuery);
            //Object results = processor.ProcessQuery(query);

            if (results != null && results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                SetText("[" + countLog++ + "] sparqlQuery01 result count: " + rset.Count + " || DeltaT: " + StopStartWatch(stopWatch) + Environment.NewLine, true);

                foreach (SparqlResult spqlResult in rset)
                {
                    LiteralNode latValueNode = (LiteralNode)spqlResult.Value("lat");
                    double lat = double.Parse(latValueNode.Value.Replace(".", ",")); // latValueNode.AsValuedNode().AsDouble();
                    LiteralNode lonValueNode = (LiteralNode)spqlResult.Value("long");
                    double lon = double.Parse(lonValueNode.Value.Replace(".", ",")); // lonValueNode.AsValuedNode().AsDouble();
                    

                    LiteralNode measurementTimeNode = (LiteralNode)spqlResult.Value("measTime");
                    DateTime measDateTime = DateTime.Parse(measurementTimeNode.Value);
                    string deviceId = spqlResult["device"].ToString();
                    string sesorId = spqlResult["sensor"].ToString();
                    LiteralNode Acceleration_Average_AxisX = (LiteralNode)spqlResult.Value("Acceleration_Average_AxisX");
                    double acceleration_Average_AxisX = Acceleration_Average_AxisX.AsValuedNode().AsDouble();
                    LiteralNode Acceleration_Average_AxisY = (LiteralNode)spqlResult.Value("Acceleration_Average_AxisY");
                    double acceleration_Average_AxisY = Acceleration_Average_AxisY.AsValuedNode().AsDouble();
                    LiteralNode Acceleration_Average_AxisZ = (LiteralNode)spqlResult.Value("Acceleration_Average_AxisZ");
                    double acceleration_Average_AxisZ = Acceleration_Average_AxisZ.AsValuedNode().AsDouble();

                    SetText("[" + countLog++ + "] sensor: " + sesorId + ", measTime: " + measDateTime.ToString("o") + ", lat: " + lat + ", lon: " + lon + ", accelX: " + acceleration_Average_AxisX + ", accelY: " + acceleration_Average_AxisY + ", accelZ: " + acceleration_Average_AxisZ + " || DeltaT: " + StopStartWatch(stopWatch) + Environment.NewLine, true);

                    string latlon = lat + "_" + lon;
                    if (repeatedPointsFromMessage.ContainsKey(latlon))
                        repeatedPointsFromMessage[latlon]++;
                    else
                        repeatedPointsFromMessage.Add(latlon, 1);

                    PointLatLng pointLatLng = new PointLatLng(lat, lon);

                    if (lastPoint.Lat == 999 || IsInTheRangeOfPosition(pointLatLng, lastPoint))
                    {
                        Point point = new Point(pointLatLng.Lat, pointLatLng.Lng, DateTime.Now, 0, 0, 0, "");
                        AddMarker(point, GMarkerGoogleType.red_small, string.Empty);
                        
                    }

                    double ca_val = ComputeCrossAxialFunction(acceleration_Average_AxisX, acceleration_Average_AxisY, acceleration_Average_AxisZ);
                    SetCrossAxialText(ca_val);

                    SetText("[" + countLog++ + "] After plotting || DeltaT: " + StopStartWatch(stopWatch) + Environment.NewLine, true);
                    break;
                }
            }

            sparqlQuery = sparqlQuery03;
            results = null;
            try
            {
                results = tStore.ExecuteQuery(sparqlQuery);
            }
            catch (Exception ex)
            { }

            if (results != null && results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                SetText("[" + countLog++ + "] Heart rate query: " + rset.Count + " || DeltaT: " + StopStartWatch(stopWatch) + Environment.NewLine, true);
                
                foreach (SparqlResult spqlResult in rset)
                {
                    LiteralNode heartRateLiteral = (LiteralNode)spqlResult.Value("measHRValue");
                    double heartRate = heartRateLiteral.AsValuedNode().AsDouble();

                    SetHeartRateText(heartRate);
                    
                    SetText("Heart rate computed: " + heartRate + " (bpm) at " + DateTime.Now.ToString("o") + Environment.NewLine, true);
                    break;
                }

            }
        }

        private void PlotRoutePointFromMessage2(string data)
        {
            GMapOverlay routes = null;
            GMapRoute route = null;
            PointLatLng lastPoint = new PointLatLng(999,999);

            if (gmap.Overlays.Count > 0)
            {
                routes = gmap.Overlays.Where(x => x.Id == "routes").FirstOrDefault();
                route = (routes != null && routes.Routes != null && routes.Routes.Count > 0)? routes.Routes[0]: new GMapRoute("routes");
                if (route.Points.Count > 0)
                    lastPoint = route.Points[route.Points.Count - 1];
            }
            else
            {
                routes = new GMapOverlay("routes");
            }


            var jsonLdParser = new JsonLdParser();
            TripleStore tStore = new TripleStore();
            string contents = data;
            using (var reader = new System.IO.StringReader(contents))
            {
                jsonLdParser.Load(tStore, reader);
            }
            SetText("After loading triplestore: " + DateTime.Now.ToString("o") + Environment.NewLine, true);
            
            string sparqlQuery = sparqlQuery01;

            SparqlQueryParser sparqlparser = new SparqlQueryParser();
            SparqlQuery query = sparqlparser.ParseFromString(sparqlQuery);
            query.Timeout = 999999;
            
            //Object results = tStore.ExecuteQuery(sparqlQuery);
            Object results = tStore.ExecuteQuery(query);

            //SparqlQuery query = sparqlparser.ParseFromString(sparqlQuery);
            //Object results = processor.ProcessQuery(query);

            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                SetText("sparqlQuery01 result count:" + rset.Count + " at " + DateTime.Now.ToString("o") + Environment.NewLine, true);

                foreach (SparqlResult spqlResult in rset)
                {
                    LiteralNode latValueNode = (LiteralNode)spqlResult.Value("lat");
                    double lat = latValueNode.AsValuedNode().AsDouble();
                    LiteralNode lonValueNode = (LiteralNode)spqlResult.Value("long");
                    double lon = lonValueNode.AsValuedNode().AsDouble();
                    LiteralNode measurementTimeNode = (LiteralNode)spqlResult.Value("measTime");
                    DateTime measDateTime = DateTime.Parse(measurementTimeNode.Value);
                    string deviceId = spqlResult["device"].ToString();
                    string sesorId = spqlResult["sensor"].ToString();
                    LiteralNode Acceleration_Average_AxisX = (LiteralNode)spqlResult.Value("Acceleration_Average_AxisX");
                    double acceleration_Average_AxisX = Acceleration_Average_AxisX.AsValuedNode().AsDouble();
                    LiteralNode Acceleration_Average_AxisY = (LiteralNode)spqlResult.Value("Acceleration_Average_AxisY");
                    double acceleration_Average_AxisY = Acceleration_Average_AxisY.AsValuedNode().AsDouble();
                    LiteralNode Acceleration_Average_AxisZ = (LiteralNode)spqlResult.Value("Acceleration_Average_AxisZ");
                    double acceleration_Average_AxisZ = Acceleration_Average_AxisZ.AsValuedNode().AsDouble();

                    //textBox1.Text += "device: " + deviceId + ", sensor: " + sesorId + ", measTime: " + measDateTime.ToString("o") + ", lat: " + lat + ", lon: " + lon + Environment.NewLine;
                    SetText("[" + DateTime.Now.ToString("o") + "] sensor: " + sesorId + ", measTime: " + measDateTime.ToString("o") + ", lat: " + lat + ", lon: " + lon + ", accelX: " + acceleration_Average_AxisX + ", accelY: " + acceleration_Average_AxisY + ", accelZ: " + acceleration_Average_AxisZ + Environment.NewLine, true);

                    string latlon = lat + "_" + lon;
                    if (repeatedPointsFromMessage.ContainsKey(latlon))
                        repeatedPointsFromMessage[latlon]++;
                    else
                        repeatedPointsFromMessage.Add(latlon, 1);

                    PointLatLng pointLatLng = new PointLatLng(lat, lon);

                    // check if the same lat/lon was already plotted (device not in movment)
                    // check if point is out of range from the prior point
                    //if (!ContainsPoint(pointLatLng, repeatedPointsFromMessage) && IsInTheRangeOfPosition(pointLatLng, lastPoint))
                    if (lastPoint.Lat == 999 || IsInTheRangeOfPosition(pointLatLng, lastPoint))
                    {
                        //points.Add(pointLatLng);

                        if (route == null)
                            route = new GMapRoute("routes");
                        
                        route.Points.Add(pointLatLng);
                        Point point = new Point(pointLatLng.Lat, pointLatLng.Lng, DateTime.Now, 0, 0, 0, "");
                        AddMarker(point, GMarkerGoogleType.red_small, string.Empty);

                        route.Stroke = new Pen(Color.Red, 3);
                        if (routes != null && routes.Routes != null && routes.Routes.Count == 0)
                        {
                            routes.Routes.Add(route);
                            gmap.Overlays.Add(routes);
                        }
                        else if (routes != null && routes.Routes != null)
                        {                            
                            routes.Routes[0] = route;
                            gmap.Overlays[0] = routes;
                        }
                        gmap.UpdateRouteLocalPosition(route);
                        
                        //gmap.Position = new GMap.NET.PointLatLng(pointLatLng.Lat, pointLatLng.Lng);

                    }
                    // Add acceleration data
                    Chart chart1_otherthread = GetChart1();
                    int i = chart1_otherthread.Series[0].Points.Count;
                    //chart1_otherthread.Series[0].Points.AddXY(i + 1, acceleration_Average_AxisX);
                    //chart1.Series[1].Points.AddXY(i + 1, acceleration_Average_AxisX);
                    //chart1.Series[2].Points.AddXY(i + 1, acceleration_Average_AxisX);
                    //chart1_otherthread.Series[0].Points[i].AxisLabel = measDateTime.ToLongTimeString();

                    //chart1_otherthread.ChartAreas[0].AxisY.Interval = tripPointHelper.TripPoint.Count / 5;
                    //chart1_otherthread.ChartAreas[0].AxisX.TextOrientation = System.Windows.Forms.DataVisualization.Charting.TextOrientation.Rotated270;

                    SetText("[" + DateTime.Now.ToString("o") + "] After plotting in map" + Environment.NewLine, true);


                }
            }

            sparqlQuery = sparqlQuery03;
            results = tStore.ExecuteQuery(sparqlQuery);
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                SetText("Heart rate query: " + rset.Count + " at " + DateTime.Now.ToString("o") + Environment.NewLine, true);

                foreach (SparqlResult spqlResult in rset)
                {
                    LiteralNode heartRateLiteral = (LiteralNode)spqlResult.Value("measHRValue");
                    double heartRate = heartRateLiteral.AsValuedNode().AsDouble();

                    SetText("Heart rate computed: " + heartRate + " (bpm) at " + DateTime.Now.ToString("o") + Environment.NewLine, true);
                }

            }
        }

        private string outputFolderPath = string.Empty;

        private void SaveFile(string deviceId, JToken data)
        {
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            //string filePath = @"D:\Projects\InterIOT\Workplan\WP2-INTER-IoT-EWS\data\out\" + deviceId + "_" + unixTimestamp.ToString() + "_" + Guid.NewGuid() + ".json";
            string filePathBasics = outputFolderPath;
            
            string filePath = filePathBasics + deviceId + "_" + unixTimestamp.ToString() + "_" + Guid.NewGuid() + ".json";
            using (StreamWriter outputFile = new StreamWriter(filePath))
            {
                outputFile.Write(data.ToString());
            }
            SetText("File saved at " + DateTime.Now.ToString("o") + " : " + filePath + Environment.NewLine, true);

        }

        private void SaveFile(string prefix, string data, string filetype)
        {
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            string filePathBasics = outputFolderPath;

            string filePath = filePathBasics + prefix + "_" + unixTimestamp.ToString() + "_" + Guid.NewGuid() + filetype;
            using (StreamWriter outputFile = new StreamWriter(filePath))
            {
                outputFile.Write(data.ToString());
            }
            SetText("File saved at " + DateTime.Now.ToString("o") + " : " + filePath + Environment.NewLine, true);

        }

        delegate void SetTextCallback(string text, bool concat);

        private void SetText(string text, bool concat)
        {
            if (this.textBox1.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text, concat });
            }
            else
            {
                string textWithTime = "[" + DateTime.Now.ToString("o") + "] " + text;
                if (concat)
                    this.textBox1.Text += textWithTime;
                else
                    this.textBox1.Text = ">> " + textWithTime;

            }
        }

        delegate void SetHeartRateTextCallback(double hr_val);
        private void SetHeartRateText(double hr_val)
        {
            if (this.label3.InvokeRequired)
            {
                SetHeartRateTextCallback d = new SetHeartRateTextCallback(SetHeartRateText);
                this.Invoke(d, new object[] { hr_val });
            }
            else
            {
                this.label3.Text = hr_val.ToString();               
            }
        }

        delegate void SetCrossAxialTextCallback(double ca_val);
        private void SetCrossAxialText(double ca_val)
        {
            if (this.label4.InvokeRequired)
            {
                SetCrossAxialTextCallback d = new SetCrossAxialTextCallback(SetCrossAxialText);
                this.Invoke(d, new object[] { ca_val });
            }
            else
            {
                this.label4.Text = ca_val.ToString();
            }
        }

        delegate string GetComboSelectedItemCallback();

        private string GetComboSelectedItem()
        {
            if (this.textBox1.InvokeRequired)
            {
                GetComboSelectedItemCallback d = new GetComboSelectedItemCallback(GetComboSelectedItem);
                return this.Invoke(d, new object[] {  }).ToString();
            }
            else
            {
                return comboBox1.SelectedItem.ToString();
            }
        }

        delegate string GetTextBox2Callback();

        private string GetTextBox2()
        {
            if (this.textBox1.InvokeRequired)
            {
                GetTextBox2Callback d = new GetTextBox2Callback(GetTextBox2);
                return this.Invoke(d, new object[] { }).ToString();
            }
            else
            {
                return textBox2.Text;
            }
        }

        delegate Chart GetChart1Callback();
        
        private Chart GetChart1()
        {
            if (this.textBox1.InvokeRequired)
            {
                GetChart1Callback d = new GetChart1Callback(GetChart1);
                return (Chart) this.Invoke(d, new object[] { });
            }
            else
            {
                return chart1;
            }
        }

        bool RunningLocal = false;

        private static int countTestCasesExecuted = 0;

        private void DisableOverlapingSituationTypes()
        {
            
            try
            {
                // Call ContextManager to start listening (subscribe) IoT Hub 
                string baseUrlCM = "https://inter-iot-ews-contextmanagerrest-v0.azurewebsites.net/api/deviceobservations/";

                if (RunningLocal)
                    baseUrlCM = "http://localhost:53268/api/deviceobservations/";

                string subscribeMsg = "translation-" + ((radioButton1.Checked) ? "1" : (radioButton2.Checked) ? "2" : "3");

                string url1 = baseUrlCM + subscribeMsg;
                MessageBox.Show("Setup url1=" + url1);

                HttpWebRequest requestCM = (HttpWebRequest)WebRequest.Create(url1);
                requestCM.AutomaticDecompression = DecompressionMethods.GZip;
                using (HttpWebResponse response = (HttpWebResponse)requestCM.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    string html = reader.ReadToEnd();
                }

                string baseUrl = "http://inter-iot-ews-situationidentificationmanagerrest-v0.azurewebsites.net/api/deviceobservations/";
                if (RunningLocal)
                    baseUrl = "http://localhost:53269/api/deviceobservations/";

                // First reset all STs
                string url = baseUrl + "reset-" + subscribeMsg;
                MessageBox.Show("DisableOverlapingSituationTypes url=" + url);

                HttpWebRequest requestReset = (HttpWebRequest)WebRequest.Create(url);
                requestReset.AutomaticDecompression = DecompressionMethods.GZip;
                using (HttpWebResponse response = (HttpWebResponse)requestReset.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    string html = reader.ReadToEnd();
                }
                
                string situationTypesToDisable = "UC01_ST02,UC01_ST03,UC01_ST04";

                if (comboBox1.SelectedItem.ToString().StartsWith("UC01_ST01"))
                    situationTypesToDisable = "UC01_ST02,UC01_ST03,UC01_ST04,UC01_ST05,UC02_ST01,UC02_ST02,UC02_ST03,UC02_ST04,UC03_ST01,UC03_ST02,UC04_ST01,UC04_ST02,UC04_ST03";
                else if (comboBox1.SelectedItem.ToString().StartsWith("UC01_ST02"))
                    situationTypesToDisable = "UC01_ST01,UC01_ST03,UC01_ST04,UC01_ST05,UC02_ST01,UC02_ST02,UC02_ST03,UC02_ST04,UC03_ST01,UC03_ST02,UC04_ST01,UC04_ST02,UC04_ST03";
                else if (comboBox1.SelectedItem.ToString().StartsWith("UC01_ST03"))
                    situationTypesToDisable = "UC01_ST02,UC01_ST01,UC01_ST04,UC01_ST05,UC02_ST01,UC02_ST02,UC02_ST03,UC02_ST04,UC03_ST01,UC03_ST02,UC04_ST01,UC04_ST02,UC04_ST03";
                else if (comboBox1.SelectedItem.ToString().StartsWith("UC01_ST04"))
                    situationTypesToDisable = "UC01_ST01,UC01_ST03,UC01_ST02,UC01_ST05,UC02_ST01,UC02_ST02,UC02_ST03,UC02_ST04,UC03_ST01,UC03_ST02,UC04_ST01,UC04_ST02,UC04_ST03";
                else if (comboBox1.SelectedItem.ToString().StartsWith("UC01_ST05"))
                    situationTypesToDisable = "UC01_ST01,UC01_ST02,UC01_ST03,UC01_ST04,UC02_ST01,UC02_ST02,UC02_ST03,UC02_ST04,UC03_ST01,UC03_ST02,UC04_ST01,UC04_ST02,UC04_ST03";
                else if (comboBox1.SelectedItem.ToString().StartsWith("UC02_ST01"))
                    situationTypesToDisable = "UC01_ST01,UC01_ST02,UC01_ST03,UC01_ST04,UC01_ST05,UC02_ST02,UC02_ST03,UC02_ST04,UC03_ST01,UC03_ST02,UC04_ST01,UC04_ST02,UC04_ST03";
                else if (comboBox1.SelectedItem.ToString().StartsWith("UC02_ST02"))
                    return; // situationTypesToDisable = "UC01_ST01,UC01_ST02,UC01_ST03,UC01_ST04,UC01_ST05,UC02_ST01,UC02_ST03,UC02_ST04,UC03_ST01,UC03_ST02,UC04_ST01,UC04_ST02,UC04_ST03";
                else if (comboBox1.SelectedItem.ToString().StartsWith("UC02_ST03"))
                    situationTypesToDisable = "UC01_ST01,UC01_ST02,UC01_ST03,UC01_ST04,UC01_ST05,UC02_ST01,UC02_ST02,UC02_ST04,UC03_ST01,UC03_ST02,UC04_ST01,UC04_ST02,UC04_ST03";
                else if (comboBox1.SelectedItem.ToString().StartsWith("UC02_ST04"))
                    situationTypesToDisable = "UC01_ST01,UC01_ST02,UC01_ST03,UC01_ST04,UC01_ST05,UC02_ST01,UC02_ST02,UC02_ST03,UC03_ST01,UC03_ST02,UC04_ST01,UC04_ST02,UC04_ST03";
                else if (comboBox1.SelectedItem.ToString().StartsWith("UC03_ST01"))
                    situationTypesToDisable = "UC01_ST01,UC01_ST02,UC01_ST03,UC01_ST04,UC01_ST05,UC02_ST01,UC02_ST02,UC02_ST03,UC02_ST04,UC03_ST02,UC04_ST01,UC04_ST02,UC04_ST03";
                else if (comboBox1.SelectedItem.ToString().StartsWith("UC03_ST02"))
                    situationTypesToDisable = "UC01_ST01,UC01_ST02,UC01_ST03,UC01_ST04,UC01_ST05,UC02_ST01,UC02_ST02,UC02_ST03,UC02_ST04,UC03_ST01,UC04_ST01,UC04_ST02,UC04_ST03";
                else if (comboBox1.SelectedItem.ToString().StartsWith("UC04_ST01"))
                    situationTypesToDisable = "UC01_ST01,UC01_ST02,UC01_ST03,UC01_ST04,UC01_ST05,UC02_ST01,UC02_ST02,UC02_ST03,UC02_ST04,UC03_ST01,UC03_ST02,UC04_ST02,UC04_ST03";
                else if (comboBox1.SelectedItem.ToString().StartsWith("UC04_ST02"))
                    situationTypesToDisable = "UC01_ST01,UC01_ST02,UC01_ST03,UC01_ST04,UC01_ST05,UC02_ST01,UC02_ST02,UC02_ST03,UC02_ST04,UC03_ST01,UC03_ST02,UC04_ST01,UC04_ST03";
                else if (comboBox1.SelectedItem.ToString().StartsWith("UC04_ST03"))
                    situationTypesToDisable = "UC01_ST01,UC01_ST02,UC01_ST03,UC01_ST04,UC01_ST05,UC02_ST01,UC02_ST02,UC02_ST03,UC02_ST04,UC03_ST01,UC03_ST02,UC04_ST01,UC04_ST02";

                url = baseUrl + situationTypesToDisable;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.AutomaticDecompression = DecompressionMethods.GZip;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    string html = reader.ReadToEnd();
                    MessageBox.Show("STs disabled: " + situationTypesToDisable);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error on [DisableOverlapingSituationTypes]:" + ex.Message);
            }
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            SendDataSampleToIPSM();
        }

        public void SendDataSampleToIPSM()
        {
            
            try
            {
                // http://grieg.ibspan.waw.pl:8888/swagger/#!/Translation/post_translation
                /*
 {
  "alignIDs": [
    {
      "name": "string",
      "version": "string"
    }
  ],
  "graphStr": "string"
}
                 */
                //string url = "http://grieg.ibspan.waw.pl:8888/translation";
                //string url = "http://168.63.44.177:8888/translation";
                string url = "http://168.61.102.226:8888/translation";

                JObject payload = new JObject();

                string folderPath = textBox2.Text;
                DirectoryInfo info = new DirectoryInfo(folderPath);
                FileInfo[] files = info.GetFiles().OrderBy(p => p.Name).ToArray();
                textBox1.Text = "files count:" + files.Count() + Environment.NewLine;
                CreateOutputFolder();

                foreach (FileInfo file in files)
                {
                    string contents = File.ReadAllText(file.FullName);
                    payload = JObject.Parse(contents);
                    JObject messageFormattedINTER_IoT_GraphSrtucture = AddINTER_IoT_GraphSrtucture(payload);

                    JObject align = new JObject();
                    align.Add("name", "SAREF_CO");
                    align.Add("version", "0.59");
                    JArray aligns = new JArray();
                    aligns.Add(align);

                    JObject message = new JObject();
                    message.Add("alignIDs", aligns);
                    //string graphStr = messageFormattedINTER_IoT_GraphSrtucture.ToString(Newtonsoft.Json.Formatting.None).Replace("\"", "'");
                    string graphStr = messageFormattedINTER_IoT_GraphSrtucture.ToString(Newtonsoft.Json.Formatting.None);
                    //string graphStr = payload.ToString(Newtonsoft.Json.Formatting.None);
                    message.Add("graphStr", graphStr);

                    SaveFile("forIPSM", message);
                    
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.Method = "POST";

                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        streamWriter.Write(message);
                    }

                    SetText("[" + DateTime.Now.ToString("o") + "] Before translation" + Environment.NewLine, true);
                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var responseText = streamReader.ReadToEnd();

                        JObject result = JObject.Parse(responseText);
                        JObject resultTranslation = new JObject();
                        if (result["message"].ToString() == "Message translation successful")
                        {
                            resultTranslation = JObject.Parse(result["graphStr"].ToString());
                            SetText("[" + DateTime.Now.ToString("o") + "] After translation" + Environment.NewLine, true);
                        }

                        string filePath = outputFolderPath + "/output_" + file.Name + "_" + Guid.NewGuid() + ".json";
                        using (StreamWriter outputFile = new StreamWriter(filePath))
                        {
                            outputFile.Write(resultTranslation.ToString(Newtonsoft.Json.Formatting.Indented));
                            SetText("[" + DateTime.Now.ToString("o") + "] file saved: " + filePath + Environment.NewLine, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error on [SendDataSampleToIPSM]:" + ex.Message);
                SetText("Error on [SendDataSampleToIPSM]:" + ex.Message, true);
            }
            
        }


        private JObject AddINTER_IoT_GraphSrtucture(JObject messageJson)
        {
            string withGraphs = @"
{
	'@graph': [
		{
			'@graph': [
				{
					'@id': 'InterIoTMsg:meta66b05c61-d687-45a3-b5fb-6864bbec3b69',
					'@type': [
						'InterIoTMsg:Platform_register',
						'InterIoTMsg:meta'
					],
					'InterIoTMsg:conversationID': 'conv99528eba-eb2d-47e8-9ee6-9dd40d19f89a',
					'InterIoTMsg:dateTimeStamp': '2017-05-22T22:19:30.281+02:00',
					'InterIoTMsg:messageID': 'msg7e484a2c-f959-486e-8da0-31143f457234'
				}
			],
			'@id': 'InterIoTMsg:metadata'
		},
		{
			'@graph': [
                " + messageJson.ToString() + @"
            ],
			'@id': 'InterIoTMsg:payload'
		}
	],
	'@context': {
		'InterIoTMsg': 'http://inter-iot.eu/message/',
		'InterIoT': 'http://inter-iot.eu/',
		'rdf': 'http://www.w3.org/1999/02/22-rdf-syntax-ns#',
		'rdfs': 'http://www.w3.org/2000/01/rdf-schema#',
		'xsd': 'http://www.w3.org/2001/XMLSchema#'
	}
}
            ";


            JObject result = JObject.Parse(withGraphs);

            return result;
        }



        private void button4_Click(object sender, EventArgs e)
        {
            string result = ConvertTimestampXSDdateTime(double.Parse(textBox5.Text));
            MessageBox.Show(result);
        }

        public string ConvertTimestampXSDdateTime(double timestamp)
        {
            string result = string.Empty;

            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dateTime = dateTime.AddMilliseconds(timestamp).ToLocalTime();
            result = dateTime.ToString("o"); // SoapDateTime.ToString(dateTime);            

            return result;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            CreateOutputFolder();
            new Task(() => { SimulateINTERIoT_MW(); }).Start();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Form2 fr = new Form2();
            fr.Show();
            this.Close();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Form3 fr = new Form3();
            fr.ShowDialog();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Form4 fr = new UIprototype.Form4();
            fr.ShowDialog();
        }

        private void button9_Click(object sender, EventArgs e)
        {

        }

        private void button10_Click(object sender, EventArgs e)
        {
            SemanticTranslationsValidator validator = new SemanticTranslationsValidator();
            ExecuteSemanticTranslationsValidationAccuracy(validator);
            //ExecuteSemanticTranslationsValidationEfficiency(validator);
        }

        private void ExecuteSemanticTranslationsValidationAccuracy(SemanticTranslationsValidator validator)
        {
            string folderPath = textBox2.Text;
            DirectoryInfo info = new DirectoryInfo(folderPath);
            FileInfo[] files = info.GetFiles().OrderBy(p => p.Name).ToArray();
            textBox1.Text = "files count:" + files.Count() + Environment.NewLine;
            CreateOutputFolder();


            foreach (FileInfo file in files)
            {
                string executionId = Guid.NewGuid().ToString();

                string contents = File.ReadAllText(file.FullName);
                JObject payload = JObject.Parse(contents);

                // In Opt1.SPARQL case we compare the RDF (SAREF) with List<Observations> 
                List<Observation> resultOpt1SPARQL = ComputeSemanticTranslationsOpt1SPARQL(payload);
                AccuracyResult resultAccuracyOpt1SPARQL = validator.ComputeAccuracyOutputTypeOpt1SPARQL(payload, resultOpt1SPARQL);
                validator.AddResultOpt1SPARQL(file.FullName, payload, resultOpt1SPARQL, resultAccuracyOpt1SPARQL);

                textBox1.Text += Environment.NewLine + "Result for [" + file.FullName + "]: " + resultAccuracyOpt1SPARQL.OutputType + Environment.NewLine;
                if (resultAccuracyOpt1SPARQL.MissingInformation.Inconsistencies.Count > 0)
                {
                    foreach (Inconsistency inconsistency in resultAccuracyOpt1SPARQL.MissingInformation.Inconsistencies)
                    {
                        switch (inconsistency.Type)
                        {
                            case InconsistencyType.Syntax:
                                textBox1.Text += "Syntax inconsistency found: " + inconsistency.TranslationPart + " || CountSAREF: " + inconsistency.CountSAREF + " || CountSSN_SOSA: " + inconsistency.CountSSN_SOSA + Environment.NewLine;
                                break;
                            case InconsistencyType.Semantic:
                                textBox1.Text += "Semantic inconsistency found: " + inconsistency.TranslationPart + " || CountSAREF: " + inconsistency.MissingAsSAREF + Environment.NewLine;
                                break;
                            default:
                                break;
                        }
                    }
                }

                /*
                // In Opt2.IPSM case we compare the RDF (SAREF) with the RDF (produced by IPSM) 
                JObject resultOpt2IPSM = ComputeSemanticTranslationsOpt2IPSM(payload);
                OutputType resultAccuracyOpt2IPSM = validator.ComputeAccuracyOutputTypeOpt2IPSM(payload, resultOpt2IPSM);
                validator.AddResultOpt2IPSM(file.FullName, payload, resultOpt2IPSM, resultAccuracyOpt2IPSM);
                */
            }

            validator.CompareAccuracy();

        }

        private void ExecuteSemanticTranslationsValidationEfficiency(SemanticTranslationsValidator validator)
        {
            string folderPath = textBox2.Text;
            DirectoryInfo info = new DirectoryInfo(folderPath);
            FileInfo[] files = info.GetFiles().OrderBy(p => p.Name).ToArray();
            textBox1.Text = "files count:" + files.Count() + Environment.NewLine;
            CreateOutputFolder();

            foreach (FileInfo file in files)
            {
                string executionId = Guid.NewGuid().ToString();

                string contents = File.ReadAllText(file.FullName);
                JObject payload = JObject.Parse(contents);

                //JObject messageFormattedINTER_IoT_GraphSrtucture = AddINTER_IoT_GraphSrtucture(payload);
                
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                List<Observation> list1 = ComputeSemanticTranslationsOpt1SPARQL(payload);

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);

                stopWatch.Start();

                JObject IPSMoutput = ComputeSemanticTranslationsOpt2IPSM(payload);
                List<Observation> list2 = ExecuteSyntaxTranslationFromIPSMoutputToPOCO(IPSMoutput);

                stopWatch.Stop();
                ts = stopWatch.Elapsed;
                elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                
                
                // Save metrics (delta T and accuracy) in file


            }
        }
        
        private List<Observation> ComputeSemanticTranslationsFromRDFtoPOCO(JToken value)
        {
            SemanticTranslations translations = new SemanticTranslations(value);
            List<Observation> observations = translations.ExecuteMappings();
            return observations;
        }

        private List<Observation> ComputeSemanticTranslationsOpt1SPARQL(JToken value)
        {
            return ComputeSemanticTranslationsFromRDFtoPOCO(value);
        }

        private JObject ComputeSemanticTranslationsOpt2IPSM(JToken value)
        {
            //List<Observation> result = new List<Observation>();

            JObject messageFormattedINTER_IoT_GraphSrtucture = AddINTER_IoT_GraphSrtucture((JObject)value);

            string url = "http://168.63.44.177:8888/translation";

            // Prepare IPSM input message
            JObject align = new JObject();
            align.Add("name", "SAREF_CO");
            align.Add("version", "0.57");

            JArray aligns = new JArray();
            aligns.Add(align);

            JObject message = new JObject();
            message.Add("alignIDs", aligns);
            message.Add("graphStr", messageFormattedINTER_IoT_GraphSrtucture.ToString(Formatting.None));

            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(message);
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var responseText = streamReader.ReadToEnd();
                return JObject.Parse(responseText);
                // Data formatted according to iiot ontology (INTER-IoT): map to POCO Observations
                //AddObservationOfDataTranslatedWithIPSM(responseText, result);

            }
            
            return null;
        }

        private List<Observation> ExecuteSyntaxTranslationFromIPSMoutputToPOCO(JObject response)
        {
            List<Observation> observations = new List<Observation>();
            JObject resultTranslation = new JObject();
            if (response["message"].ToString() == "Message translation successful")
            {
                resultTranslation = JObject.Parse(response["graphStr"].ToString());

                SaveFile("GOIoTP", resultTranslation);

                // TODO: test with UC01
                SemanticTranslations translations = new SemanticTranslations(resultTranslation);
                observations = translations.ExecuteMappings();
                
            }
            else
            {
                SaveFile("IPSM_error", response);
            }
            return observations;
        }

        private void button11_Click(object sender, EventArgs e)
        {

        }

        private void button12_Click(object sender, EventArgs e)
        {
            TestINTERMWPostToContextManager();
        }

        private void TestINTERMWPostToContextManager()
        {
            // Call ContextManager to start listening (subscribe) IoT Hub 
            string baseUrlCM = "https://inter-iot-ews-contextmanagerrest-v0.azurewebsites.net/api/trip/";

            if (RunningLocal)
                baseUrlCM = "http://localhost:53268/api/trip/";


            JObject payload = new JObject();

            string folderPath = textBox2.Text;
            DirectoryInfo info = new DirectoryInfo(folderPath);
            FileInfo[] files = info.GetFiles().OrderBy(p => p.Name).ToArray();
            textBox1.Text = "files count:" + files.Count() + Environment.NewLine;
            CreateOutputFolder();

            foreach (FileInfo file in files)
            {
                string contents = File.ReadAllText(file.FullName);
                payload = JObject.Parse(contents);
                JObject messageFormattedINTER_IoT_GraphSrtucture = AddINTER_IoT_GraphSrtucture(payload);
                
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(baseUrlCM);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(messageFormattedINTER_IoT_GraphSrtucture);
                }

                SetText("[" + DateTime.Now.ToString("o") + "] Called " + baseUrlCM + Environment.NewLine, true);
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var responseText = streamReader.ReadToEnd();

                    SetText("[" + DateTime.Now.ToString("o") + "] Received " + responseText + Environment.NewLine, true);

                }
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            SaveFile("logfile", textBox1.Text, ".log");
            MessageBox.Show("Log file saved!");
        }
    }
}

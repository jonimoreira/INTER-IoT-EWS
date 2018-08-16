using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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


        private void LoadMap()
        {
            //gmap.MapProvider = GMap.NET.MapProviders.BingMapProvider.Instance;
            gmap.MapProvider = GMap.NET.MapProviders.GoogleMapProvider.Instance;
            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerOnly;
            gmap.SetPositionByKeywords("Enschede");
            //gmap.Position = new GMap.NET.PointLatLng(48.8589507, 2.2775175);
            gmap.Position = new GMap.NET.PointLatLng(52.2390134, 6.857026);
            gmap.ShowCenter = false;
            gmap.CanDragMap = true;
            gmap.DragButton = MouseButtons.Left;
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

            foreach (FileInfo file in files)
            {
                /*
                JObject jObject = JObject.Parse(file.OpenText().ToString());
                textBox1.Text += jObject["label"] + Environment.NewLine;
                */
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

                    }
                }

                sparqlQuery = sparqlQuery03;
                results = tStore.ExecuteQuery(sparqlQuery);

                if (results is SparqlResultSet)
                {
                    SparqlResultSet rset = (SparqlResultSet)results;
                    textBox1.Text += "query3 count:" + rset.Count + Environment.NewLine;

                    foreach (SparqlResult spqlResult in rset)
                    {
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
                        point.HeartRate = heartRate;

                        LiteralNode batteryLevelLiteral = (LiteralNode)spqlResult.Value("measBatteryECGValue");
                        double batteryLevel = batteryLevelLiteral.AsValuedNode().AsDouble();
                        point.BatteryLevelECG = batteryLevel;

                    }
                }

                sparqlQuery = sparqlQuery04;
                results = tStore.ExecuteQuery(sparqlQuery);

                if (results is SparqlResultSet)
                {
                    SparqlResultSet rset = (SparqlResultSet)results;
                    textBox1.Text += "query4 count:" + rset.Count + Environment.NewLine;

                    foreach (SparqlResult spqlResult in rset)
                    {
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
            }
            
            return TripPointHelper;

        }
        
        private void PlotTripRoute(TripPointHelper tripPointHelper)
        {
            GMapOverlay routes = new GMapOverlay("routes");
            List<PointLatLng> points = new List<PointLatLng>();

            //textBox1.Text = string.Empty;
            Point lastPoint = null;
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
                    //textBox1.Text += "[" + point.DateTimeMessageSent.ToString("o") + "] Lat:" + point.Latitude + " |Lon:" + point.Longitude + Environment.NewLine;

                    if (lastPoint == null)
                        AddMarker(point, GMarkerGoogleType.green_big_go, "Start");

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
                double threshold = 0.0004;
                if ((Math.Abs(pointCurrent.Latitude - pointLast.Latitude) > threshold)
                    || (Math.Abs(pointCurrent.Longitude - pointLast.Longitude) > threshold))
                    return false;
            }
            
            return true;
        }

        private void AddMarker(Point point, GMarkerGoogleType markerType, string tooltipText)
        {
            GMapOverlay markers = new GMapOverlay("markers");
            GMapMarker marker = new GMarkerGoogle(new PointLatLng(point.Latitude, point.Longitude), markerType);
            marker.ToolTipMode = MarkerTooltipMode.Always;
            marker.ToolTipText = tooltipText;

            markers.Markers.Add(marker);
            gmap.Overlays.Add(markers);
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

                chart1.Series[0].Points[i].AxisLabel = point.DateTimeMessageSent.ToLongTimeString();

                // Heart rate
                chart2.Series[0].Points.AddXY(i + 1, Math.Abs(point.HeartRate));
                chart2.Series[0].Points[i].AxisLabel = point.DateTimeMessageSent.ToLongTimeString();

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
            DisableOverlapingSituationTypes();

            Form2 f = new Form2();
            f.comboBox1.SelectedItem = comboBox1.SelectedItem;
            f.button1.Enabled = false;
            f.button2.Enabled = false;
            f.ExecuteTestCase(this.TripPointHelper, int.Parse(textBox3.Text));
            f.ShowDialog();
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

        static string iotHubUri = "XXXXXXXXXXXXXXXX";
        static string deviceKey = "XXXXXXXXXXXXXXXX";
        static string deviceId = "XXXXXXXXXXXXXXXX";

        public void SendDataToAzureIoTHub(int threadSleep)
        {
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

        }

        // To listen directly from Azure IoT Hub and call PUT /api/deviceobservations/{deviceId}
        static string connectionString = "XXXXXXXXXXXXXXXX";
        static string iotHubD2cEndpoint = "messages/events";
        static EventHubClient eventHubClient;

        public void SimulateINTERIoT_MW()
        {
            eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, iotHubD2cEndpoint);

            var d2cPartitions = eventHubClient.GetRuntimeInformation().PartitionIds;

            CancellationTokenSource cts = new CancellationTokenSource();

            var tasks = new List<Task>();
            foreach (string partition in d2cPartitions)
            {
                tasks.Add(ReceiveMessagesFromIoTHub(partition, cts.Token));
            }
            Task.WaitAll(tasks.ToArray());
        }

        private async Task ReceiveMessagesFromIoTHub(string partition, CancellationToken ct)
        {
            var eventHubReceiver = eventHubClient.GetDefaultConsumerGroup().CreateReceiver(partition, DateTime.UtcNow);
            while (true)
            {
                if (ct.IsCancellationRequested) break;
                EventData eventData = await eventHubReceiver.ReceiveAsync();
                if (eventData == null) continue;

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

                    SaveFile(domain, data);

                }

            }
        }

        private void PlotMarkerFromEmergencyMessage(string data)
        {
            GMapOverlay routes = null;
            GMapRoute route = null;
            List<PointLatLng> points = null;
            PointLatLng lastPoint = new PointLatLng();

            if (gmap.Overlays.Count > 0)
            {
                routes = gmap.Overlays[0];
                route = routes.Routes[0];
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

                    GMapOverlay markers = new GMapOverlay("markers");
                    GMapMarker marker = new GMarkerGoogle(new PointLatLng(lat, lon), GMarkerGoogleType.red_big_stop);
                    marker.ToolTipMode = MarkerTooltipMode.Always;
                    marker.ToolTipText = toolTip;
                    markers.Markers.Add(marker);
                    gmap.Overlays.Add(markers);
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
                ORDER BY ?measTime 

            ";

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

        private void PlotRoutePointFromMessage(string data)
        {
            GMapOverlay routes = null;
            GMapRoute route = null;
            List<PointLatLng> points = null;
            PointLatLng lastPoint = new PointLatLng();

            if (gmap.Overlays.Count > 0)
            {
                routes = gmap.Overlays[0];
                route = routes.Routes[0];
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

            string sparqlQuery = sparqlQuery01;

            Object results = tStore.ExecuteQuery(sparqlQuery);

            //SparqlQuery query = sparqlparser.ParseFromString(sparqlQuery);
            //Object results = processor.ProcessQuery(query);

            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                SetText("query count:" + rset.Count + Environment.NewLine, true);

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
                    SetText("sensor: " + sesorId + ", measTime: " + measDateTime.ToString("o") + ", lat: " + lat + ", lon: " + lon + ", accelX: " + acceleration_Average_AxisX + ", accelY: " + acceleration_Average_AxisY + ", accelZ: " + acceleration_Average_AxisZ + Environment.NewLine, true);

                    string latlon = lat + "_" + lon;
                    if (repeatedPointsFromMessage.ContainsKey(latlon))
                        repeatedPointsFromMessage[latlon]++;
                    else
                        repeatedPointsFromMessage.Add(latlon, 1);

                    PointLatLng pointLatLng = new PointLatLng(lat, lon);

                    // check if the same lat/lon was already plotted (device not in movment)
                    // check if point is out of range from the prior point
                    if (!ContainsPoint(pointLatLng, repeatedPointsFromMessage) && IsInTheRangeOfPosition(pointLatLng, lastPoint))
                    {
                        points.Add(pointLatLng);

                        if (route == null)
                            route = new GMapRoute(points, GetComboSelectedItem());
                        route.Stroke = new Pen(Color.Green, 3);
                        if (routes.Routes.Count == 0)
                        {
                            routes.Routes.Add(route);
                            gmap.Overlays.Add(routes);
                        }
                        else
                        {
                            routes.Routes[0] = route;
                            gmap.Overlays[0] = routes;
                        }
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
                if (concat)
                    this.textBox1.Text += text;
                else
                    this.textBox1.Text = text;

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

        private void DisableOverlapingSituationTypes()
        {
            try
            {
                // Call ContextManager to start listening (subscribe) IoT Hub 
                string baseUrlCM = "https://inter-iot-ews-contextmanagerrest-v0.azurewebsites.net/api/deviceobservations/";
                HttpWebRequest requestCM = (HttpWebRequest)WebRequest.Create(baseUrlCM + "subscribe");
                requestCM.AutomaticDecompression = DecompressionMethods.GZip;
                using (HttpWebResponse response = (HttpWebResponse)requestCM.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    string html = reader.ReadToEnd();
                }

                //string baseUrl = "http://localhost:53269/api/deviceobservations/";
                string baseUrl = "http://inter-iot-ews-situationidentificationmanagerrest-v0.azurewebsites.net/api/deviceobservations/";

                // First reset all STs
                string url = baseUrl + "reset";
                HttpWebRequest requestReset = (HttpWebRequest)WebRequest.Create(url);
                requestReset.AutomaticDecompression = DecompressionMethods.GZip;
                using (HttpWebResponse response = (HttpWebResponse)requestReset.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    string html = reader.ReadToEnd();
                }

                string situationTypesToDisable = "UC01_VehicleCollisionDetected_ST02,UC01_VehicleCollisionDetected_ST03,UC01_VehicleCollisionDetected_ST04";

                if (comboBox1.SelectedItem.ToString().StartsWith("UC01_ST01"))
                    situationTypesToDisable = "UC01_VehicleCollisionDetected_ST02,UC01_VehicleCollisionDetected_ST03,UC01_VehicleCollisionDetected_ST04";
                else if (comboBox1.SelectedItem.ToString().StartsWith("UC01_ST02"))
                    situationTypesToDisable = "UC01_VehicleCollisionDetected_ST01,UC01_VehicleCollisionDetected_ST03,UC01_VehicleCollisionDetected_ST04";
                
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
    }
}

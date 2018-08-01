using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Parsing;
using VDS.RDF.Query;

namespace INTERIoTEWS.UIprototype
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            UpdateChart();
        }

        private void UpdateChart()
        {
            // read file from path
            string dataStr = GetFileContentFromPath();

            SetupChart();

            // get hasValues from leads and plot data
            ExecuteSPARQL(dataStr);
                                    
        }

        private void SetupChart()
        {
            //chart1.Series[0].XValueType = ChartValueType.DateTime;
            //chart1.ChartAreas[0].AxisX.Interval = 1;
            //chart1.ChartAreas[0].AxisX.IntervalType = DateTimeIntervalType.Milliseconds;
            //chart1.ChartAreas[0].AxisX.IntervalOffset = 1;

            chart1.Series[0].Points.Clear();
            chart1.Series[1].Points.Clear();
            chart1.Series[2].Points.Clear();
            chart1.Series[3].Points.Clear();


        }

        public string GetFileContentFromPath()
        {
            string filePath = textBox1.Text; // + @"\Projects\InterIOT\Workplan\WP2-INTER-IoT-EWS\data\tests\1525876445_1fd06e81-2684-44a8-a49a-3d89e7ab792f.json";
            string json = string.Empty;
            using (StreamReader r = new StreamReader(filePath))
            {
                json = r.ReadToEnd();
            }
            return json;
        }

        public void ExecuteSPARQL(string data)
        {
            DateTime firstTimestamp = DateTime.Now;
            DateTime lastTimestamp = DateTime.Now;
            int numValues1 = 0;
            int numValues2 = 0;
            int numValues3 = 0;
            int numValues4 = 0;
            int counter = 0;
            Lead lead2 = null;
            double deltaTimeFrequencyBasedInSeconds = 0.004;

            var jsonLdParser = new JsonLdParser();
            TripleStore tStore = new TripleStore();
            using (var reader = new System.IO.StringReader(data))
            {
                jsonLdParser.Load(tStore, reader);
            }

            // Query for all saref4health:hasValues
            string sparqlQuery = @"
                PREFIX saref: <https://w3id.org/saref#>
                PREFIX saref4health: <https://w3id.org/def/saref4health#>

                SELECT ?sensor ?measurement ?timestamp ?value 
                WHERE  
	                {
					?measurement saref4health:hasValues ?value.
					?sensor saref:makesMeasurement ?measurement.
					?measurement saref:hasTimestamp ?timestamp
					}
				ORDER BY ?sensor ?timestamp

            ";
            Object results = tStore.ExecuteQuery(sparqlQuery);

            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;

                foreach (SparqlResult spqlResult in rset)
                {   
                    
                    string sensorId = spqlResult["sensor"].ToString();

                    LiteralNode measurementTimeNode = (LiteralNode)spqlResult.Value("timestamp");
                    DateTime measDateTime = DateTime.Parse(measurementTimeNode.Value);
                    
                    // update time
                    double intervalToAddInSeconds = deltaTimeFrequencyBasedInSeconds * numValues2;

                    measDateTime = measDateTime.AddSeconds(intervalToAddInSeconds);

                    LiteralNode measurementValueNode = (LiteralNode)spqlResult.Value("value");
                    double measurementValue = measurementValueNode.AsValuedNode().AsDouble();

                    // Undo multiplication from MyDriving
                    measurementValue = measurementValue / 100.0;

                    double measurementValueRounded = measurementValue; // Math.Round(measurementValue);
                    
                    double[] point = { measurementValue };

                    if (counter == 0)
                        firstTimestamp = measDateTime;

                    if (counter == rset.Count - 1)
                        lastTimestamp = measDateTime;
                    
                    counter++;
                    
                    switch (sensorId)
                    {
                        case "https://w3id.org/saref/instances#ECGLead_I_code131329":
                            chart1.Series[0].Points.AddY(measurementValueRounded);
                            numValues1++;
                            break;
                        case "https://w3id.org/saref/instances#ECGLead_II_code131330":

                            if (lead2 == null)
                                lead2 = new Lead(sensorId);

                            lead2.Points.Add(new DataPoint(measDateTime, measurementValueRounded));
                            
                            numValues2++;
                            break;
                        case "https://w3id.org/saref/instances#ECGLead_III_code131389":
                            //chart1.Series[0].Points.Add(point);
                            //chart1.Series[0].Points.AddXY(measDateTime.Millisecond, measurementValue);
                            chart1.Series[2].Points.AddY(measurementValueRounded);
                            numValues3++;
                            break;
                        default:
                            chart1.Series[3].Points.AddY(measurementValueRounded);
                            numValues4++;

                            break;
                    }

                }
            }

            chart1.Series[0].LegendText += " - count: " + numValues1;
            chart1.Series[1].LegendText += " - count: " + numValues2;
            chart1.Series[2].LegendText += " - count: " + numValues3;
            chart1.Series[3].LegendText += " - count: " + numValues4;

            chart1.Update();

            firstTimestamp = lead2.Points[0].Xval;
            lastTimestamp = lead2.Points[lead2.Points.Count - 1].Xval;

            label1.Text += lead2.Name;
            label1.Text += "\n";
            label1.Text = firstTimestamp.ToString("o") + " ---TO--- " + lastTimestamp.ToString("o");
            label1.Text += "\n";
            label1.Text += "Delta time (ms): " + (lastTimestamp - firstTimestamp).TotalMilliseconds ;

            DrawLead(lead2);

            CalculateHeartRate(lead2);

        }

        private void DrawLead(Lead lead2)
        {
            chart1.Series[1].Points.Clear();
            for (int i = 0; i < lead2.Points.Count; i++)
            {
                DataPoint point = lead2.Points[i];
                chart1.Series[1].Points.AddXY(point.Xval, point.Yval);

            }

            
        }

        private void CalculateHeartRate(Lead lead2)
        {
            double sumValues = 0;
            double sumOfDerivation = 0;
            double sumOfVariance = 0;
            foreach (DataPoint point in lead2.Points)
            {
                sumValues += point.Yval;
                sumOfDerivation += (point.Yval * point.Yval);
            }

            double meanValue = sumValues / lead2.Points.Count;

            foreach (DataPoint point in lead2.Points)
            {
                point.ComputeVariance(meanValue);
                sumOfVariance += point.Variance;
            }

            double sumOfDerivationAverage = sumOfDerivation / lead2.Points.Count;
            double standardDeviation = Math.Sqrt(sumOfDerivationAverage - (meanValue * meanValue));

            label1.Text += "\n";
            label1.Text += "meanValue = " + meanValue + "\n";
            label1.Text += "sumOfVariance = " + sumOfVariance + "\n";
            label1.Text += "sumOfDerivationAverage = " + sumOfDerivationAverage + "\n";
            label1.Text += "standardDeviation = " + standardDeviation + "\n";

            textBox2.Text = string.Empty;
            int count = 0;
            foreach (DataPoint point in lead2.Points)
            {
                if (point.Yval > meanValue + standardDeviation)
                {
                    textBox2.Text += "[" + count + "]: " + point.Xval.ToString("o") + " ## " + point.Yval + " ## " + point.Variance + Environment.NewLine;
                }
                count++;
            }


        }

        private void button2_Click(object sender, EventArgs e)
        {
            int[] x = new int[] { 9811, 9535, 9634, 10005, 10157, 10024, 9537, 9531, 9809, 10147, 10023, 9633, 9528, 9813, 10143, 10018, 9621, 9528, 9812, 10144, 10016, 9634, 9534, 9819, 10145, 10021, 9810, 9532, 9647, 10014, 10160, 9808, 9533, 9633, 10012, 10161, 9808, 9539, 9647, 10019, 10160, 9806, 9535, 9649, 10021, 10159, 10015, 9628, 9653, 9826, 10140, 10005, 9630, 9538, 9836, 10160, 10011, 9629, 9537, 9829, 10159, 10012, 9627, 9535, 9837, 10161, 10000, 9784, 9533, 9663, 10031, 10166, 9801, 9545, 9670, 10040, 10163, 9803, 9544, 9677, 10042, 10169, 9804, 9540, 9667, 10045, 10170, 9807, 9640, 9563, 9861, 10182, 10020, 9644, 9568, 9865, 10185, 10023, 9643, 9576, 9871, 10174, 10020, 9645, 9565, 9870, 10189, 10180, 9647, 9557, 9695, 10070, 10180, 9814, 9563, 9707, 10072, 10181, 9798, 9556, 9711, 10074, 10181, 9812, 9568, 9710, 10087, 10199, 10019, 9569, 9582, 9892, 10196, 10019, 9641, 9568, 9886, 10199, 10012, 9645, 9584, 9898, 10201, 10011, 9640, 9583, 9897, 10202, 10178, 9796, 9585, 9715, 10075, 10165, 9796, 9563, 9718, 10086, 10174, 9791, 9559, 9718, 10082, 10167, 9792, 9559, 9721, 10087, 10157, 9980, 9623, 9581, 9900, 10198, 9995, 9629, 9580, 9900, 10194, 9988, 9779, 9583, 9904, 10194, 9978, 9767, 9565, 9888, 10189, 9981, 9767, 9544, 9716, 9904, 10153, 9769, 9541, 9719, 10084, 10148, 9763, 9611, 9711, 10073, 10139, 9759, 9544, 9715, 10090, 10188, 9759, 9608, 9573, 9892, 10182, 9960, 9604, 9571, 9894, 10176, 9946, 9599, 9576, 9905, 10183, 9955, 9598, 9571, 9900, 10085, 10126, 9594, 9530, 9716, 10081, 10127, 9731, 9513, 9702, 10073, 10122, 9732, 9523, 9711, 10079, 10118, 9728, 9520, 9709, 10072, 10159, 9921, 9567, 9556, 9873, 10153, 9916, 9570, 9552, 9887, 10156, 9912, 9564, 9551, 9886, 10152, 9905, 9558, 9547, 9885, 10141, 10077, 9689, 9494, 9695, 10062, 10082, 9692, 9493, 9689, 10059, 10080, 9687, 9491, 9692, 10056, 10074, 9671, 9476, 9681, 9870, 10068, 9877, 9535, 9536, 9878, 10134, 9876, 9530, 9528, 9875, 10130, 9870, 9529, 9525, 9863, 10119, 9860, 9524, 9531, 9684, 10048, 9859, 9654, 9469, 9678, 10046, 10044, 9649, 9464, 9678, 10039, 10031, 9640, 9463, 9678, 10040, 10037, 9643, 9462, 9680, 9870, 10116, 9842, 9509, 9517, 9868, 10107, 9833, 9491, 9514, 9867, 10106, 9831, 9499, 9520, 9865, 10106, 9827, 9501, 9520, 9870, 10040, 10023, 9622, 9451, 9665, 10034, 10014, 9623, 9450, 9681, 10041, 10017, 9621, 9454, 9683, 10044, 10016, 9619, 9451, 9682, 10034, 10091, 9811, 9491, 9526, 9880, 10109, 9815, 9490, 9525, 9884, 10109, 9815, 9493, 9534, 9888, 10109, 9808, 9489, 9533, 9694, 10113, 10016, 9624, 9465, 9708, 10060, 10020, 9624, 9467, 9709, 10064, 10021, 9622, 9467, 9702, 10064, 10019, 9627, 9476, 9558, 9914, 10020, 9821, 9508, 9560, 9916, 10127, 9824, 9512, 9565, 9914, 10118, 9819, 9514, 9567, 9928, 10134, 9826, 9519, 9573, 9739, 10094, 10033, 9638, 9491, 9744, 10099, 10031, 9627, 9493, 9751, 10104, 10037, 9633, 9499, 9754, 10107, 10037, 9638, 9502, 9760, 9953, 10149, 9835, 9531, 9583, 9953, 10149, 9835, 9536, 9603, 9963, 10157, 9836, 9540, 9609, 9967, 10161, 9839, 9540, 9521, 9966, 10116, 10042, 9651, 9523, 9786, 10135, 10051, 9659, 9531, 9792, 10140, 10048, 9662, 9534, 9799, 10148, 10047, 9654, 9533, 9633, 10150, 10178, 9850, 9558, 9640, 10001, 10170, 9846, 9554, 9638, 9997, 10175, 9842, 9549, 9628, 10000, 10168, 9838, 9551, 9538, 9807, 10149, 10044, 9651, 9531, 9808, 10148, 10040, 9649, 9534, 9802, 10135, 10028, 9645, 9529, 9807, 10142, 10031, 9637, 9525, 9628, 9993, 10148, 9810, 9526, 9624, 9993, 10144, 9791, 9518, 9621, 9988, 10141, 9797, 9516, 9618, 9986, 10137, 9787, 9510, 9613, 9787, 10118, 9991, 9600, 9488, 9777, 10112, 9984, 9595, 9495, 9779, 10108, 9975, 9588, 9490, 9775, 10105, 9967, 9580, 9484, 9761, 9953, 10100, 9749, 9481, 9597, 9968, 10104, 9747, 9478, 9593, 9961, 10095, 9736, 9470, 9588, 9955, 10081, 9722, 9553, 9465, 9953, 10083, 9937, 9552, 9464, 9760, 10081, 9932, 9548, 9465, 9760, 10081, 9926, 9541, 9449, 9748, 10074, 9917, 9535, 9451, 9577, 9947, 10068, 9706, 9446, 9574, 9944, 10064, 9700, 9445, 9570, 9934, 10057, 9696, 9443, 9575, 9947, 10059, 9694, 9438, 9449, 9753, 10067, 9897, 9521, 9449, 9754, 10065, 9881, 9513, 9450, 9756, 10065, 9892, 9512, 9447, 9752, 10062, 9886, 9679, 9445, 9575, 9945, 10046, 9673, 9417, 9566, 9943, 10042, 9673, 9424, 9575, 9947, 10042, 9668, 9426, 9576, 9948, 10039, 9667, 9503, 9566, 9742, 10055, 9869, 9501, 9444, 9757, 10058, 9867, 9497, 9444, 9759, 10058, 9861, 9494, 9441, 9760, 10051, 9854, 9650, 9417, 9579, 9952, 10031, 9651, 9419, 9579, 9951, 10029, 9648, 9417, 9580, 9953, 10025, 9642, 9406, 9577, 9953, 10025, 9644, 9488, 9444, 9768, 10060, 9854, 9490, 9450, 9770, 10061, 9848, 9488, 9440, 9759, 10054, 9846, 9486, 9449, 9772, 10059, 9848, 9486, 9418, 9591, 9963, 10017, 9630, 9416, 9592, 9956, 10008, 9625, 9409, 9590, 9958, 10011, 9627, 9410, 9591, 9966, 10015, 9832, 9415, 9455, 9785, 10063, 9830, 9462, 9448, 9782, 10064 };

            MessageBox.Show("Number of samples: " + x.Length);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SendDataToAzureIoTHub();
            MessageBox.Show("Message sento to: " + iotHubUri);
        }

        static Microsoft.Azure.Devices.Client.DeviceClient deviceClient;
        
        static string iotHubUri = "XXXXXXXXXXXXXXXX";
        static string deviceKey = "XXXXXXXXXXXXXXXX";
        static string deviceId = "XXXXXXXXXXXXXXXX";

        public void SendDataToAzureIoTHub()
        {
            if (deviceClient == null)
                deviceClient = Microsoft.Azure.Devices.Client.DeviceClient.Create(iotHubUri, new Microsoft.Azure.Devices.Client.DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey), Microsoft.Azure.Devices.Client.TransportType.Http1);

            var messageString = GetFileContentFromPath();
            var message = new Microsoft.Azure.Devices.Client.Message(Encoding.ASCII.GetBytes(messageString));
            
            deviceClient.SendEventAsync(message);
        }

    }
}

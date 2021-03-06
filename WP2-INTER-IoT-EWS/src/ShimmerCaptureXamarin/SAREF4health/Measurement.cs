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
using Newtonsoft.Json.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace ShimmerCaptureXamarin.SAREF4health
{
    
    public class Measurement
    {
        public Measurement()
        {
        }

        private string isMeasuredIn;
        private string relatesToProperty;
        private string type;
        private string id;
        private string label;
        private double hasTimestamp;
        private double hasValue;
        private JObject jSONLDobject;

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



        public JObject IsMeasuredIn_JsonLD
        {
            get
            {
                JObject result = new JObject();
                result.Add("@id", isMeasuredIn);
                result.Add("@type", "saref:UnitOfMeasure");

                return result;
            }
        }


        public JObject RelatesToProperty_JsonLD
        {
            get
            {
                JObject result = new JObject();
                result.Add("@id", relatesToProperty);

                if (relatesToProperty.Contains("sarefInst:Acceleration"))
                {
                    string types = @"
                        [ 'dim:Acceleration', 'saref:Property' ]
                    ";
                    JArray array = JArray.Parse(types);
                    result.Add("@type", array);
                }
                else
                    result.Add("@type", "saref:Property");
                    
                return result;
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
                UpdateJSONLD();
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
                UpdateJSONLD();
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
                UpdateJSONLD();
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
                UpdateJSONLD();
            }
        }

        public JObject JSONLDobject
        {
            get
            {
                return jSONLDobject;
            }

            set
            {
                jSONLDobject = value;
            }
        }

        public double HasValue
        {
            get
            {
                return hasValue;
            }

            set
            {
                hasValue = value;
                UpdateJSONLD();
            }
        }


        private void UpdateJSONLD()
        {
            if (jSONLDobject != null)
            {
                jSONLDobject["label"] = this.label;
                jSONLDobject["saref:hasTimestamp"] = ConvertTimestampXSDdateTime(this.hasTimestamp);
                jSONLDobject["saref:hasValue"] = this.hasValue;

                // TODO: implement the rest...
            }
        }

        private string ConvertTimestampXSDdateTime(double timestamp)
        {
            string result = string.Empty;

            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dateTime = dateTime.AddMilliseconds(timestamp).ToLocalTime();
            result = SoapDateTime.ToString(dateTime);

            return result;
        }

    }

    public class ECGSampleSequence
    {
        public ECGSampleSequence()
        {
        }

        // saref:isMeasuredIn only saref4health:ElectricPotential
        private string isMeasuredIn;
        // saref:relatesToProperty only saref4health:HeartElectricalActivity
        private string relatesToProperty;
        private string type;
        private string id;
        private string label;
        private double hasTimestamp;
        private List<double> hasValues;
        private JObject jSONLDobject;

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

        public List<double> HasValues
        {
            get
            {
                return hasValues;
            }

            set
            {
                hasValues = value;
            }
        }

        public JObject JSONLDobject
        {
            get
            {
                return jSONLDobject;
            }

            set
            {
                jSONLDobject = value;
            }
        }
    }

        /*
        public class ECGtimeSeries
        {
            public ECGtimeSeries()
            {
                values = new Dictionary<double, double>();
            }

            private Dictionary<double, double> values;

            /// <summary>
            /// timestamp, ECG lead value
            /// </summary>
            public Dictionary<double, double> Values
            {
                get
                {
                    return values;
                }

                set
                {
                    values = value;
                }
            }
        }

        public class ECGdata
        {
            public ECGdata()
            {
                values = new Dictionary<string, SAREF4health.ECGtimeSeries>();
            }

            private Dictionary<string, ECGtimeSeries> values;

            public Dictionary<string, ECGtimeSeries> Values
            {
                get
                {
                    return values;
                }

                set
                {
                    values = value;
                }
            }
        }
        */
    }
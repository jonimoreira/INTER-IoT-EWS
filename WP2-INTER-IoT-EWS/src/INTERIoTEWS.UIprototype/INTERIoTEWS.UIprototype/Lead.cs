using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INTERIoTEWS.UIprototype
{
    public class Lead
    {

        private List<DataPoint> points;
        public string Name { get; set; }

        public List<DataPoint> Points
        {
            get
            {
                return points;
            }

            set
            {
                points = value;
            }
        }

        public Lead(string name)
        {
            this.Name = name;
            points = new List<UIprototype.DataPoint>();
        }

    }

    public class DataPoint
    {
        public DateTime Xval { get; set; }
        public double Yval { get; set; }
        public double Variance { get; set; }

        public DataPoint(DateTime xval, double yval)
        {
            this.Xval = xval;
            this.Yval = yval;
        }

        public void ComputeVariance(double meanValue)
        {
            this.Variance = Math.Pow(Yval - meanValue, 2);
        }

    }



}

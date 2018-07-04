using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INTERIoTEWS.Context.DataObjects.SOSA.GEO
{
    public class Point: IEsperEvent
    {
        public object Identifier { get; private set; }
        public double Lat { get; private set; }
        public double Long { get; private set; }

        public Point(object identifier, double lat, double lon)
        {
            this.Identifier = identifier;
            this.Lat = lat;
            this.Long = lon;
        }
    }
}

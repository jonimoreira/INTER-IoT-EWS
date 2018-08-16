using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INTERIoTEWS.Context.DataObjects.SOSA
{
    /// <summary>
    /// http://www.w3.org/ns/sosa/Sensor
    /// Accelerometers, gyroscopes, barometers, magnetometers, and so forth are Sensors that are typically mounted on a modern smart phone (which acts as Platform).
    /// </summary>
    public class Sensor : IEsperEvent
    {
        public object Identifier { get; private set; }

        public Sensor(object identifier, Platform platformHost, string _label)
        {
            this.Identifier = identifier;
            this.isHostedBy = platformHost;
            this.label = _label;
        }

        public Platform isHostedBy { get; set; }
        public string label { get; set; }
        
    }
}

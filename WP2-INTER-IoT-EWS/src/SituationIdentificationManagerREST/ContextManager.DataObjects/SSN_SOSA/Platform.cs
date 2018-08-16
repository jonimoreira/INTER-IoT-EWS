using INTERIoTEWS.Context.DataObjects.SOSA.GEO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INTERIoTEWS.Context.DataObjects.SOSA
{
    /// <summary>
    /// http://www.w3.org/ns/sosa/Platform
    /// A Platform is an entity that hosts other entities, particularly Sensors, Actuators, Samplers, and other Platforms. 
    /// "Sensors that are typically mounted on a modern smart phone (which acts as Platform)."
    /// </summary>
    public class Platform: IEsperEvent
    {
        public object Identifier { get; private set; }

        public Platform(object identifier, Point _location, string _label)
        {
            this.Identifier = identifier;
            this.location = _location;
            this.label = _label;

            if (_location == null)
                throw new Exception("GeoPoint (location) data is null, input error.");
        }

        public Point location { get; private set; }
        public string label { get; private set; }
        
        // workaround: a property to store the tripId (check afterwards how to map from SAREF)
        public string tripId { get; set; }

    }
}

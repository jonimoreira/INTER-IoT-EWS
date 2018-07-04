using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Threading;

namespace INTERIoTEWS.Context.DataObjects.SOSA
{
    [DataContract]
    [KnownType(typeof(SEMIoTICS.AccelerometerSensorObservation))]
    [KnownType(typeof(SEMIoTICS.VehicleCollisionDetectedObservation))]
    public class Observation: IEsperEvent
    {
        public delegate void SensorUpdatedHandler(Observation sensor);
        public event SensorUpdatedHandler SensorUpdated;
        
        [DataMember]
        public object Identifier { get; private set; }

        public Observation(object identifier, Sensor sensor, string messageId)
        {
            this.Identifier = identifier;
            this.madeBySensor = sensor;
            this.MessageId = messageId;
        }

        public override string ToString()
        {
            return base.ToString() + "[" + this.Identifier + "]";
        }

        public DateTime resultTime { get; set; }

        public int hasSimpleResult { get; set; }
        
        public Result hasResult { get; set; }

        public Sensor madeBySensor { get; set; }
        
        public ObservableProperty observedProperty { get; set; }

        public string MessageId { get; private set; }

        /*
        protected virtual void update()
        {
            if (SensorUpdated != null)
            {
                SensorUpdated(this);
            }
        }
        */
    }
}

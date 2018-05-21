using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Threading;

namespace ContextManagerREST.Domain
{
    [DataContract]
    [KnownType(typeof(AccelerometerSensor))]
    //[KnownType(typeof(LocationSensor))]
    //[KnownType(typeof(TireSensor))]
    public abstract class Sensor
    {
        public delegate void SensorUpdatedHandler(Sensor sensor);
        public event SensorUpdatedHandler SensorUpdated;

        [DataMember]
        public object Identifier { get; private set; }

        public Sensor(object identifier)
        {
            this.Identifier = identifier;
        }

        /*
        public void Start()
        {
            var threadStart = new ThreadStart(loopUpdate);
            var thread = new Thread(threadStart);
            thread.Start();
        }
        */

        public override string ToString()
        {
            return base.ToString() + "[" + this.Identifier + "]";
        }

        /*
        private void loopUpdate()
        {
            while (true)
            {
                Thread.Sleep(2000);
                this.update();
            }
        }
        */

        protected virtual void update()
        {
            if (SensorUpdated != null)
            {
                SensorUpdated(this);
            }
        }
    }
}

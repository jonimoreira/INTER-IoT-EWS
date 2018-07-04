using INTERIoTEWS.Context.DataObjects.SOSA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace INTERIoTEWS.Context.DataObjects.SEMIoTICS
{
    [DataContract]
    public class AccelerometerSensorObservation : Observation, IEsperEvent
    {
        [DataMember]
        public double AccelerationX { get; private set; }
        public double AccelerationY { get; private set; }
        public double AccelerationZ{ get; private set; }

        public double TriAxialAccelerationEnergy { get; private set; }
        

        public AccelerometerSensorObservation(object identifier, Sensor sensor, double accelX, double accelY, double accelZ, double triAxial, bool collision, string messageId) : base (identifier, sensor, messageId)
        {
            this.AccelerationX = accelX;
            this.AccelerationY = accelY;
            this.AccelerationZ = accelZ;
            this.TriAxialAccelerationEnergy = triAxial;
        }

        /*
        protected override void update()
        {
            
            double change = 4;
            this.Acceleration -= change;

            if (this.Acceleration < 0)
            {
                this.Acceleration = 0;
            }
            
            base.update();
        }
        */
    }
}

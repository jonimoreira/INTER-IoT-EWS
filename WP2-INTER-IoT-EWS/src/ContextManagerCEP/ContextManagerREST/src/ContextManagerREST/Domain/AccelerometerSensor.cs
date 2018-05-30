using ContextManagerREST.CEP;
using ContextManagerREST.SituationIdentification.CEP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace ContextManagerREST.Domain
{
    [DataContract]
    public class AccelerometerSensor : Sensor, IEsperEvent
    {
        [DataMember]
        public double AccelerationX { get; private set; }
        public double AccelerationY { get; private set; }
        public double AccelerationZ{ get; private set; }

        public double TriAxialAccelerationEnergy { get; private set; }
        public bool VehicleCollisionDetectedFromMobileDevice { get; private set; }


        public AccelerometerSensor(object identifier, double accelX, double accelY, double accelZ, double triAxial, bool collision) : base (identifier)
        {
            this.AccelerationX = accelX;
            this.AccelerationY = accelY;
            this.AccelerationZ = accelZ;
            this.TriAxialAccelerationEnergy = triAxial;
            this.VehicleCollisionDetectedFromMobileDevice = collision;
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

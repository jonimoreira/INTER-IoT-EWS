using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyDriving.Helpers
{
    public class SmartphoneAccelerationManager
    {
        private Dictionary<DateTime, SmartphoneAcceleration> accelerations;
        public SmartphoneAccelerationManager()
        {
            Accelerations = new Dictionary<DateTime, SmartphoneAcceleration>();
        }

        public Dictionary<DateTime, SmartphoneAcceleration> Accelerations
        {
            get
            {
                return accelerations;
            }

            set
            {
                accelerations = value;
            }
        }
        
    }

    public class SmartphoneAcceleration
    {
        public SmartphoneAcceleration(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }
    
}
using MyDriving.EWS.SAREF4health;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyDriving.Helpers
{
    public class SmartphoneAccelerationManager
    {
        
        public SmartphoneAccelerationManager()
        {
            Accelerations = new Dictionary<DateTime, SmartphoneAcceleration>();
        }

        private Dictionary<DateTime, SmartphoneAcceleration> accelerations;
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

        public VehicleCollisionDetection VehicleCollisionDetectionFromSmartphone
        {
            get
            {
                return vehicleCollisionDetectionFromSmartphone;
            }

            set
            {
                vehicleCollisionDetectionFromSmartphone = value;
            }
        }

        private VehicleCollisionDetection vehicleCollisionDetectionFromSmartphone;

        public bool collisionDetected = false;
        public double valueDetectedX = 0.0;
        public double valueDetectedY = 0.0;
        public double valueDetectedZ = 0.0;
        public DateTime timestampAsDateTime;
        public double timestampCollisionDetected;

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
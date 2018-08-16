using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INTERIoTEWS.UIprototype
{
    public class TripPointHelper
    {
        Dictionary<int, Point> tripPoint;

        public Dictionary<int, Point> TripPoint
        {
            get
            {
                if (tripPoint == null)
                    tripPoint = new Dictionary<int, Point>();
                return tripPoint;
            }

            set
            {
                tripPoint = value;
            }
        }
    }

    public class Point
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime DateTimeMessageSent { get; set; }
        public double AccelerationX { get; set; }
        public double AccelerationY { get; set; }
        public double AccelerationZ { get; set; }
        public double AccelerationX_ECG { get; set; }
        public double AccelerationY_ECG { get; set; }
        public double AccelerationZ_ECG { get; set; }
        public double HeartRate { get; set; }
        public string OriginalFileContent { get; set; }
        public string OriginalFileName { get; set; }
        public bool VehicleCollisionDetected { get; set; }
        public DateTime VehicleCollisionDateTime { get; set; }
        public double VehicleCollisionCrossAxial { get; set; }
        public double BatteryLevelECG { get; set; }

        public Point(double lat, double lon, DateTime dt, double accelX, double accelY, double accelZ, string fileContent)
        {
            Latitude = lat;
            Longitude = lon;
            DateTimeMessageSent = dt;
            AccelerationX = accelX;
            AccelerationY = accelY;
            AccelerationZ = accelZ;
            OriginalFileContent = fileContent;
            AccelerationX_ECG = double.MinValue;
            AccelerationY_ECG = double.MinValue;
            AccelerationZ_ECG = double.MinValue;
            VehicleCollisionDetected = false;
            BatteryLevelECG = double.MinValue;
        }
    }
}

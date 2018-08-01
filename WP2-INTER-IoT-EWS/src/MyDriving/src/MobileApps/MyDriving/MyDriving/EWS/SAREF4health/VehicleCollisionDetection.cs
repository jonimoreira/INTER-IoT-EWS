using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyDriving.EWS.SAREF4health
{
    public class VehicleCollisionDetection
    {
        public double sumCrossAxialValues = 0.0;
        public int countCrossAxialValues = 0;
        public const double InitialVariance = 0.0; //7.8; // 61.1
        public double varianceCrossAxialValues = InitialVariance; // if acceleration is measured in m/s^2 => equivalent to a change of 10km/h in 1 second. 
        public double timestampCollisionDetected = double.MinValue;

        public bool collisionDetected = false;

        public double sumOfDerivation = 0;
        public double sumOfDerivationAverage = 0;
        public double standardDeviation = 0;
        public double meanCrossAxialValues = 0;
        public double crossAxialEnergyWhenCollisionDetected = 0;
        private double threshold = 30; // (unit of measure: m/s2.  1 G = 9.806 m/s2 (common threshold = 4G)

        public double Threshold
        {
            get
            {
                return threshold;
            }

            set
            {
                threshold = value;
            }
        }

        public VehicleCollisionDetection(double _threshold)
        {
            this.threshold = _threshold;
        }

        public void ClearDetectCollisionVariables(List<Measurement> accelerationCrossAxialList)
        {
            sumCrossAxialValues = 0.0;
            countCrossAxialValues = 0;
            varianceCrossAxialValues = InitialVariance; // if acceleration is measured in m/s^2 => equivalent to a change of 10km/h in 1 second.
            collisionDetected = false;
            timestampCollisionDetected = double.MinValue;
            lock (accelerationCrossAxialList)
                accelerationCrossAxialList.Clear();
            sumOfDerivation = 0;
            sumOfDerivationAverage = 0;
            standardDeviation = 100;
            meanCrossAxialValues = 0;
        }

    }
}
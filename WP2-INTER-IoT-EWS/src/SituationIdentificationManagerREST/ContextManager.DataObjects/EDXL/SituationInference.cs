using EDXLSharp.CAPLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContextManager.DataObjects.EDXL.InferenceHandler
{
    public class SituationInference
    {
        private string SituationTypeIdentified;
        public Dictionary<String, object> AttributesSituationIdentified;

        public SituationInference(string situationTypeIdentified, Dictionary<String, object> attributesSituationIdentified)
        {
            this.SituationTypeIdentified = situationTypeIdentified;
            this.AttributesSituationIdentified = attributesSituationIdentified;
        }

        public EmergencyInformation GetSeverityAndUrgency()
        {
            EmergencyInformation result = new InferenceHandler.EmergencyInformation();

            switch (SituationTypeIdentified)
            {
                case "UC01_VehicleCollisionDetected_ST01":
                case "UC01_VehicleCollisionDetected_ST02":
                case "UC01_VehicleCollisionDetected_ST03":
                case "UC01_VehicleCollisionDetected_ST04":
                    result = GetSeverityAndUrgency_UC01_VehicleCollisionDetected();
                    break;
                default:
                    break;
            }

            return result;
        }

        const double Gforce = 9.806; // m/s2 

        private EmergencyInformation GetSeverityAndUrgency_UC01_VehicleCollisionDetected()
        {
            // (unit of measure: m/s2.  1 G = 9.806 m/s2 (common threshold = 4G)
            double threshold = 3 * Gforce;

            double computedCrossAxialValue = double.Parse(AttributesSituationIdentified["ComputedCrossAxialValue"].ToString());

            double acceleration = Math.Sqrt(computedCrossAxialValue);

            EmergencyInformation result = ComputeUC01Classification(acceleration, threshold);

            return result;
        }
        
        private EmergencyInformation ComputeUC01Classification(double acceleration, double threshold)
        {
            EmergencyInformation emergencyInfo = new InferenceHandler.EmergencyInformation();
            emergencyInfo.Description = "acceleration: " + acceleration + ", threshold: " + threshold;

            if (acceleration > threshold && acceleration <= threshold * 1.2)
            {
                emergencyInfo.Severity = SeverityType.Minor;
                emergencyInfo.Urgency = UrgencyType.Expected;
                emergencyInfo.Description = "Green: Light accident. Acceleration (cross-axial): " + acceleration + ", while threshold = " + threshold;
            }
            else if (acceleration > threshold * 1.2 && acceleration <= threshold * 1.4)
            {
                emergencyInfo.Severity = SeverityType.Moderate;
                emergencyInfo.Urgency = UrgencyType.Immediate;
                emergencyInfo.Description = "Yellow: Accident. Acceleration (cross-axial): " + acceleration + ", while threshold = " + threshold;
            }
            else if (acceleration > threshold * 1.4 && acceleration <= threshold * 1.6)
            {
                emergencyInfo.Severity = SeverityType.Severe;
                emergencyInfo.Urgency = UrgencyType.Immediate;
                emergencyInfo.Description = "Red: Hard accident. Acceleration (cross-axial): " + acceleration + ", while threshold = " + threshold;
            }
            else if (acceleration > threshold * 1.6)
            {
                emergencyInfo.Severity = SeverityType.Extreme;
                emergencyInfo.Urgency = UrgencyType.Immediate;
                emergencyInfo.Description = "ATENTION! Black: Extreme accident. Acceleration (cross-axial): " + acceleration + ", while threshold = " + threshold;
            }

            return emergencyInfo;
        }

    }

    public class EmergencyInformation
    {
        public SeverityType Severity { get; set; }
        public UrgencyType Urgency { get; set; }
        public string Description { get; set; }

        public EmergencyInformation()
        {
            this.Severity = SeverityType.Unknown;
            this.Urgency = UrgencyType.Unknown;
            this.Description = "Description not set";
        }
    }
}

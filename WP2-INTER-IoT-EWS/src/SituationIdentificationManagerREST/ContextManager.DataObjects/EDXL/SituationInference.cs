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
                case "UC01_VehicleCollisionDetected_ST05":
                    result = GetSeverityAndUrgency_UC01_VehicleCollisionDetected();
                    break;
                case "UC02_HealthEarlyWarningScore_ST01":
                case "UC02_HealthEarlyWarningScore_ST02":
                case "UC02_HealthEarlyWarningScore_ST03":
                case "UC02_HealthEarlyWarningScore_ST04":
                case "UC03_TemporalRelations_ST01":
                case "UC03_TemporalRelations_ST02":
                    result = GetSeverityAndUrgency_UC02_HealthEarlyWarningScore();
                    break;
                case "UC04_DangerousGoods_ST01":
                case "UC04_DangerousGoods_ST02":
                case "UC04_DangerousGoods_ST03":
                    result = GetSeverityAndUrgency_UC04_DangerousGoods();
                    break;
                default:
                    break;
            }

            return result;
        }

        private EmergencyInformation GetSeverityAndUrgency_UC04_DangerousGoods()
        {
            EmergencyInformation result = null; 

            switch (SituationTypeIdentified)
            {
                case "UC04_DangerousGoods_ST01":
                    result = GetSeverityAndUrgency_UC01_VehicleCollisionDetected();
                    break;
                case "UC04_DangerousGoods_ST02":
                    result = GetSeverityAndUrgency_UC02_HealthEarlyWarningScore();
                    break;
                case "UC04_DangerousGoods_ST03":
                    result = GetSeverityAndUrgency_UC02_HealthEarlyWarningScore();
                    break;
                default:
                    break;
            }

            result.Description = AttributesSituationIdentified["DangerousGoods"].ToString() + Environment.NewLine + result.Description;

            return result;
        }

        public const double Gforce = 9.806; // m/s2 

        public static double thresholdAcceleration = 3 * Gforce;

        public static double thresholdBradycardia = 49;
        public static double thresholdTachycardia = 101;

        private EmergencyInformation GetSeverityAndUrgency_UC02_HealthEarlyWarningScore()
        {   
            double heartRate = double.Parse(AttributesSituationIdentified["resultValue"].ToString());
            
            EmergencyInformation result = ComputeUC02Classification(heartRate);

            return result;
        }

        private EmergencyInformation ComputeUC02Classification(double heartRate)
        {
            EmergencyInformation emergencyInfo = new InferenceHandler.EmergencyInformation();
            emergencyInfo.Description = "heartRate: " + heartRate + ", thresholdBradycardia: " + thresholdBradycardia + ", thresholdTachycardia: " + thresholdTachycardia;

            // Bradycardia
            if (heartRate <= thresholdBradycardia && heartRate > thresholdBradycardia * 0.9)
            {
                emergencyInfo.Severity = SeverityType.Minor;
                emergencyInfo.Urgency = UrgencyType.Expected;
                emergencyInfo.Description = "Green: light bradycardia. Heart rate: " + heartRate + ", while thresholdBradycardia = " + thresholdBradycardia;
            }
            else if (heartRate <= thresholdBradycardia * 0.9 && heartRate > thresholdBradycardia * 0.8)
            {
                emergencyInfo.Severity = SeverityType.Moderate;
                emergencyInfo.Urgency = UrgencyType.Immediate;
                emergencyInfo.Description = "Yellow: moderate bradycardia. Heart rate: " + heartRate + ", while thresholdBradycardia = " + thresholdBradycardia;
            }
            else if (heartRate <= thresholdBradycardia * 0.8 && heartRate > thresholdBradycardia * 0.7)
            {
                emergencyInfo.Severity = SeverityType.Severe;
                emergencyInfo.Urgency = UrgencyType.Immediate;
                emergencyInfo.Description = "Red: severe bradycardia. Heart rate: " + heartRate + ", while thresholdBradycardia = " + thresholdBradycardia;
            }
            else if (heartRate <= thresholdBradycardia * 0.7)
            {
                emergencyInfo.Severity = SeverityType.Extreme;
                emergencyInfo.Urgency = UrgencyType.Immediate;
                emergencyInfo.Description = "ATENTION! Extreme bradycardia. Heart rate: " + heartRate + ", while thresholdBradycardia = " + thresholdBradycardia;
            }

            // Tachycardia
            if (heartRate > thresholdTachycardia && heartRate <= thresholdTachycardia * 1.4)
            {
                emergencyInfo.Severity = SeverityType.Minor;
                emergencyInfo.Urgency = UrgencyType.Expected;
                emergencyInfo.Description = "Green: light tachycardia. Heart rate: " + heartRate + ", while thresholdTachycardia = " + thresholdTachycardia;
            }
            else if (heartRate > thresholdTachycardia * 1.4 && heartRate <= thresholdTachycardia * 1.8)
            {
                emergencyInfo.Severity = SeverityType.Moderate;
                emergencyInfo.Urgency = UrgencyType.Immediate;
                emergencyInfo.Description = "Yellow: moderate tachycardia. Heart rate: " + heartRate + ", while thresholdTachycardia = " + thresholdTachycardia;
            }
            else if (heartRate > thresholdTachycardia * 1.8 && heartRate <= thresholdTachycardia * 2.0)
            {
                emergencyInfo.Severity = SeverityType.Severe;
                emergencyInfo.Urgency = UrgencyType.Immediate;
                emergencyInfo.Description = "Red: severe tachycardia. Heart rate: " + heartRate + ", while thresholdTachycardia = " + thresholdTachycardia;
            }
            else if (heartRate > thresholdTachycardia * 2.0)
            {
                emergencyInfo.Severity = SeverityType.Extreme;
                emergencyInfo.Urgency = UrgencyType.Immediate;
                emergencyInfo.Description = "ATENTION! Extreme tachycardia. Heart rate: " + heartRate + ", while thresholdTachycardia = " + thresholdTachycardia;
            }

            return emergencyInfo;
        }
        
        private EmergencyInformation GetSeverityAndUrgency_UC01_VehicleCollisionDetected()
        {
            // (unit of measure: m/s2.  1 G = 9.806 m/s2 (common threshold = 4G)

            double computedCrossAxialValue = double.Parse(AttributesSituationIdentified["ComputedCrossAxialValue"].ToString());

            double acceleration = Math.Sqrt(computedCrossAxialValue);

            EmergencyInformation result = ComputeUC01Classification(acceleration);

            return result;
        }
        
        private EmergencyInformation ComputeUC01Classification(double acceleration)
        {
            EmergencyInformation emergencyInfo = new InferenceHandler.EmergencyInformation();
            emergencyInfo.Description = "acceleration: " + acceleration + ", threshold: " + thresholdAcceleration;

            if (acceleration > thresholdAcceleration && acceleration <= thresholdAcceleration * 1.2)
            {
                emergencyInfo.Severity = SeverityType.Minor;
                emergencyInfo.Urgency = UrgencyType.Expected;
                emergencyInfo.Description = "Green: Light incident. Acceleration (cross-axial): " + acceleration + ", while threshold = " + thresholdAcceleration;
            }
            else if (acceleration > thresholdAcceleration * 1.2 && acceleration <= thresholdAcceleration * 1.4)
            {
                emergencyInfo.Severity = SeverityType.Moderate;
                emergencyInfo.Urgency = UrgencyType.Immediate;
                emergencyInfo.Description = "Yellow: Accident. Acceleration (cross-axial): " + acceleration + ", while threshold = " + thresholdAcceleration;
            }
            else if (acceleration > thresholdAcceleration * 1.4 && acceleration <= thresholdAcceleration * 1.6)
            {
                emergencyInfo.Severity = SeverityType.Severe;
                emergencyInfo.Urgency = UrgencyType.Immediate;
                emergencyInfo.Description = "Red: Hard accident. Acceleration (cross-axial): " + acceleration + ", while threshold = " + thresholdAcceleration;
            }
            else if (acceleration > thresholdAcceleration * 1.6)
            {
                emergencyInfo.Severity = SeverityType.Extreme;
                emergencyInfo.Urgency = UrgencyType.Immediate;
                emergencyInfo.Description = "ATENTION! Extreme accident. Acceleration (cross-axial): " + acceleration + ", while threshold = " + thresholdAcceleration;
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

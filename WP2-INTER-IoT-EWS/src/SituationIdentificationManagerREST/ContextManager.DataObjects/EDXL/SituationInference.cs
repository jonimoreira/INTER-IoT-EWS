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

        public KeyValuePair<SeverityType, UrgencyType> GetSeverityAndUrgency()
        {
            KeyValuePair<SeverityType, UrgencyType> result = new KeyValuePair<SeverityType, UrgencyType>(SeverityType.Unknown, UrgencyType.Unknown);

            switch (SituationTypeIdentified)
            {
                case "UC01_VehicleCollisionDetected_ST01":
                case "UC01_VehicleCollisionDetected_ST02":
                    result = GetSeverityAndUrgency_UC01_VehicleCollisionDetected();
                    break;
                default:
                    break;
            }

            return result;
        }
        
        private KeyValuePair<SeverityType, UrgencyType> GetSeverityAndUrgency_UC01_VehicleCollisionDetected()
        {
            KeyValuePair<SeverityType, UrgencyType> result = new KeyValuePair<SeverityType, UrgencyType>(SeverityType.Unknown, UrgencyType.Unknown);

            // (unit of measure: m/s2.  1 G = 9.806 m/s2 (common threshold = 4G)
            double threshold = 30; 
            double computedCrossAxialValue = double.Parse(AttributesSituationIdentified["ComputedCrossAxialValue"].ToString());

            double acceleration = Math.Sqrt(computedCrossAxialValue);

            result = ComputeUC01Classification(acceleration, threshold);

            return result;
        }
        
        private KeyValuePair<SeverityType, UrgencyType> ComputeUC01Classification(double acceleration, double threshold)
        {
            KeyValuePair<SeverityType, UrgencyType> result = new KeyValuePair<SeverityType, UrgencyType>(SeverityType.Unknown, UrgencyType.Unknown);

            if (acceleration > threshold && acceleration <= threshold * 1.2)
                result = new KeyValuePair<SeverityType, UrgencyType>(SeverityType.Minor, UrgencyType.Expected);
            else if (acceleration > threshold * 1.2 && acceleration <= threshold * 1.4)
                result = new KeyValuePair<SeverityType, UrgencyType>(SeverityType.Moderate, UrgencyType.Immediate);
            else if (acceleration > threshold * 1.4 && acceleration <= threshold * 1.6)
                result = new KeyValuePair<SeverityType, UrgencyType>(SeverityType.Severe, UrgencyType.Immediate);
            else if (acceleration > threshold * 1.6)
                result = new KeyValuePair<SeverityType, UrgencyType>(SeverityType.Extreme, UrgencyType.Immediate);

            return result;
        }

    }
}

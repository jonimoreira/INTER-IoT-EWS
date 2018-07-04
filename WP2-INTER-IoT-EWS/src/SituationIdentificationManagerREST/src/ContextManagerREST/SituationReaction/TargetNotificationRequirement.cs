using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace INTERIoTEWS.SituationIdentificationManager.SituationIdentificationREST.SituationReaction
{
    public class TargetNotificationRequirement
    {
        public string Name { get; set; }
        public TargetNotificationRequirementTypeEnum TargetNotificationRequirementType { get; set; }
        public string TargetName { get; set; }


        public TargetNotificationRequirement(string name, TargetNotificationRequirementTypeEnum targetNotificationRequirementType, string targetName)
        {
            this.Name = name;
            this.TargetNotificationRequirementType = targetNotificationRequirementType;
            this.TargetName = targetName;
        }


        public enum TargetNotificationRequirementTypeEnum
        {
            AllTripData = 0,
            AllHealthDataFromTrip = 1,
            AllPositionDataFromTrip = 2,
            OnlyLocationSituationTypeDetected = 3,
            // Think properly some examples => should be configurable for end user.
        }

    }
}

using INTERIoTEWS.SituationIdentificationManager.SituationIdentificationREST.SituationReaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace INTERIoTEWS.SituationIdentificationManager.SituationIdentificationREST.SituationIdentification
{
    public class SituationType
    {
        public string Name { get; set; }
        public SituationReaction.SituationReactionProcess SituationReactionProcess { get; set; }

        public SituationType(string name)
        {
            this.Name = name;

            // Check whether this ST is linked to a react process
            // Idea: DB with tblSituationType (id, name), tblResponseProcess (<<follow common approaches of process-activity>>), tblSituationTypeResponseProcess 

            this.SituationReactionProcess = new SituationReaction.SituationReactionProcess("Default");
            NotificationActivity defaultNotification = new SituationReaction.NotificationActivity("Azure IoT Hub");
            TargetNotificationRequirement dataElement = new SituationReaction.TargetNotificationRequirement("Vehicle data", TargetNotificationRequirement.TargetNotificationRequirementTypeEnum.AllTripData, "Port authority (Pablo)");
            defaultNotification.TargetNotificationRequirements.Add(dataElement);
            this.SituationReactionProcess.NotificationActivities.Add(defaultNotification);

        }
        
    }
    
}

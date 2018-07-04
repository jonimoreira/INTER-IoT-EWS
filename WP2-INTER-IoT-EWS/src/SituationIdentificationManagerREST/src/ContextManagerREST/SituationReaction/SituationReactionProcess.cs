using INTERIoTEWS.SituationIdentificationManager.SituationIdentificationREST.SituationIdentification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace INTERIoTEWS.SituationIdentificationManager.SituationIdentificationREST.SituationReaction
{
    public class SituationReactionProcess
    {
        public string Name { get; set; }
        public List<NotificationActivity> NotificationActivities { get; set; }

        public SituationReactionProcess(string name)
        {
            this.Name = name;
            NotificationActivities = new List<SituationReaction.NotificationActivity>();

            
        }

        public void Execute(Situation situationIdentified)
        {
            foreach (NotificationActivity activity in NotificationActivities)
            {
                activity.Execute(situationIdentified);
            }
        }
    }
}

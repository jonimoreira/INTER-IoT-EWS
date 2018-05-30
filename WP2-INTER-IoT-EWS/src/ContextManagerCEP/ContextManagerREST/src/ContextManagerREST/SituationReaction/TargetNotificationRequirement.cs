using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ContextManagerREST.SituationReaction
{
    public class TargetNotificationRequirement
    {
        public string Name { get; set; }
        
        public TargetNotificationRequirement(string name)
        {
            this.Name = name;
        }
    }
}

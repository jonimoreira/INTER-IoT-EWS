using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ContextManagerREST.SituationIdentification
{
    public class Situation
    {
        public SituationType Type { get; }
        public bool Active { get; }

        public Situation(SituationType type, bool active)
        {
            Type = type;
            Active = active;
        }

    }
}

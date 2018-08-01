using ContextManager.DataObjects.EDXL.InferenceHandler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace INTERIoTEWS.SituationIdentificationManager.SituationIdentificationREST.SituationIdentification
{
    public class Situation
    {
        public SituationType Type { get; }
        public bool Active { get; }
        public Dictionary<String, object> AttributesFromEPL { get; }
        public SituationInference SituationInference { get; }
        
        public Situation(SituationType type, bool active, Dictionary<String, object> attributes)
        {
            Type = type;
            Active = active;
            AttributesFromEPL = attributes;

            AttributesFromEPL.Add("SituationBegin", DateTime.Now);

            SituationInference = new SituationInference(Type.Name, AttributesFromEPL);
            
        }
        
    }
}

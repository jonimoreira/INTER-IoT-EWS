using INTERIoTEWS.Context.DataObjects.SOSA;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace INTERIoTEWS.Context.DataObjects.SEMIoTICS
{
    [DataContract]
    public class DangerousGoodsObservation : Observation
    {
        public DangerousGoodsObservation(Observation observationBase) : base(observationBase.Identifier, observationBase.madeBySensor, observationBase.MessageId)
        {
            this.hasResult = observationBase.hasResult;
            this.hasSimpleResult = observationBase.hasSimpleResult;
            this.observedProperty = observationBase.observedProperty;
            this.resultTime = observationBase.resultTime;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ReadDeviceToCloudMessages
{
    [DataContract]
    internal class ECGDevice
    {
        [DataMember]
        internal string id;
        [DataMember]
        internal string type;
        [DataMember]
        internal string comment;
        [DataMember]
        internal string label;
        [DataMember]
        internal string seeAlso;
        [DataMember]
        internal List<ECGLeadBipolarLimb> consistsOf;

    }

    [DataContract]
    internal class ECGLeadBipolarLimb
    {
        [DataMember]
        internal string label;
    }

}

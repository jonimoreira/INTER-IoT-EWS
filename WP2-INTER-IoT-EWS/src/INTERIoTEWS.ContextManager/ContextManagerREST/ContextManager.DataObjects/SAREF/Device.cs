using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ContextManager.DataObjects.SAREF
{
    [Table("INTERIoTEWS.DeviceObservation")]
    public class DeviceObservation : BaseDataObject
    {
        public DeviceObservation()
        {

        }

        public string DeviceIdURI { get; set; }
        public string DeviceTypeURI { get; set; }

        public string Label { get; set; }
        public string Comment { get; set; }
        public string SeeAlso { get; set; }

    }
}

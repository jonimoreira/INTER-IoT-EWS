using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDriving.EWS.Logistics.LogiTrans
{
    /// <summary>
    /// http://ontology.tno.nl/transport#TransportEvent
    /// rdfs:subClassOf
    ///     LogiCO:hasLocation exactly 1 LogiTrans:Transport Location
    ///     LogiServ:hasTime min 1
    ///     LogiTrans:has transport event type code max 1
    ///     LogiServ:has status max 1
    ///     LogiCO:hasDescription max 1
    ///     LogiServ:Event
    /// </summary>
    public class TransportEvent
    {
        public DateTime hasTime { get; set; }
        public LogiCO.Location hasLocation { get; set; }

        public string hasDescription { get; set; }
        public string hasTransportEventTypeCode { get; set; }
        public string hasStatus { get; set; }

        // Added by me
        
    }

    /// <summary>
    /// http://ontology.tno.nl/transport#GoodsItem
    /// A Goods Item specifies goods or products to be transported.
    /// </summary>
    public class GoodsItem
    {
        public string hasDescription { get; set; }
    }
}

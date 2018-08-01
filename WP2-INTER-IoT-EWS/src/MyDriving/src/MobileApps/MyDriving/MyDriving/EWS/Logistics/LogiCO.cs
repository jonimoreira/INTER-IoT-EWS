using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDriving.EWS.Logistics.LogiCO
{
    /// <summary>
    /// It represents the relative location used to define the place(s) relevant for logistics activities.
    /// Equivalent to the concept of Place in:
    ///     - DOLCE UltraLite(DUL) ontology:  'A location, in a very generic sense: a political geographic entity (Roma, Lesotho), 
    ///     a non-material location determined by the presence of other entities ('the area close to Roma'), pivot events or signs 
    ///     ('the area where the helicopter fell'), complements of other entities ('the area under the table'), etc. 
    /// In this generic sense, a Place is a relative location.For an absolute location, see the class SpaceRegion.
    /// </summary>
    public class Location
    {
        public Address hasAddress { get; set; }

    }

    public class Address
    {

    }

    public class MoveableEquipment
    {
        public List<LogiTrans.GoodsItem> contains { get; set; }
        public TransportMeans isContainedIn { get; set; }
        public TransportMeans isMovedBy { get; set; }

    }

    public class TransportMeans
    {

    }

    public class Truck : TransportMeans
    {

    }

}

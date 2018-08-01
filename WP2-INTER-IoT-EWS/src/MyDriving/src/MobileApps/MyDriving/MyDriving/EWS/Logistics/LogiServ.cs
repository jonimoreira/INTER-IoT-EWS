using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// http://ontology.tno.nl/logiserv/
/// </summary>
namespace MyDriving.EWS.Logistics.LogiServ
{
    /// <summary>
    /// http://ontology.tno.nl/logiserv/LogiServ_Transport.html
    /// Transport denotes the activity of moving resources (namely, products and/or packages and/or pieces of equipment, but also empty containers) 
    /// from an origin to a destination, in a certain time, using suitable transport mode and transport means
    /// rdfs:subClassOf
    ///     LogiServ:hasOrigin some LogiCO:Location
    ///     time:hasBeginning some time:Instant
    ///     LogiServ:Activity
    ///     LogiServ:hasDestination some LogiCO:Location
    ///     time:hasEnd some time:Instant
    ///     
    /// Alignment with MyDriving: MyDriving.DataObjects.Trip
    /// 
    /// </summary>
    public class Transport: Activity
    {
        /// <summary>
        /// Trip.RecordedTimeStamp
        /// </summary>
        public DateTime hasBeginning { get; set; }

        /// <summary>
        /// Trip.EndTimeStamp
        /// </summary>
        public DateTime hasEnd { get; set; }

        public LogiCO.Location hasOrigin { get; set; }
        public LogiCO.Location hasDestination { get; set; }

        private DataObjects.Trip MyDrivingTrip { get; set; }


        public Transport()
        {

        }

        public Transport(DataObjects.Trip myDrivingTrip)
        {
            this.Identifier = myDrivingTrip.Id;
            hasBeginning = myDrivingTrip.RecordedTimeStamp;
            hasEnd = myDrivingTrip.EndTimeStamp;

            MyDrivingTrip = myDrivingTrip;
            // Idea: map from Trip.Points to origin (first) and destination (last)

        }

    }


    public class Activity: BaseClass
    {
        public DateTime hasTime { get; set; }
        public LogiCO.Location hasLocation { get; set; }

    }

    public class Cargo
    {
        public LogiCO.MoveableEquipment contains { get; set; }
    }


}

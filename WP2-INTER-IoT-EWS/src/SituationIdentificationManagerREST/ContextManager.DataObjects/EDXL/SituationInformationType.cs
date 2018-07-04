using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace INTERIoTEWS.Context.DataObjects.EDXL
{
    /// <summary>
    /// Identifies and describes the incident with which the message is concerned
    /// </summary>
    public class SituationInformationType: IReport
    {
        /// <summary>
        /// The primaryIncidentInformation identifies and describes the initial incident.
        /// OPTIONAL [0..1]
        /// </summary>
        private IncidentInformationType primaryIncidentInformation;

        //·         subIncidentInformation[0..*]: SubIncidentKInformationType
    }
}

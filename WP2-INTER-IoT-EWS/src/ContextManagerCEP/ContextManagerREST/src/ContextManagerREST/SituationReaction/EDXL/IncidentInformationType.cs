using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ContextManagerREST.CEP.SituationReaction.EDXL
{
    public class IncidentInformationType
    {
        /// <summary>
        /// Situation Information MUST carry one or multiple incident names.  A formally declared incident may have a name which can change during the incident lifespan.  Previous names MUST be carried. In addition, the same incident is sometimes assigned different names by different jurisdictions, organizations or agencies.  These multiple incident names MUST be carried.
        /// REQUIRED; MAY be used more than once [1..*]
        /// </summary>
        private List<string> incidentNames;

        /// <summary>
        /// The physical location of the incident applying reusable <EDXLLocationType> components to express location information using a variety of options including geopolitical (e.g. addresses) and geospatial (e.g. lat/long).
        /// REQUIRED [1..1]
        /// </summary>
        private string incidentLocation; //[1..1]: ct:EDXLLocationType

        /*
·         
·         incidentKind [0..*]: IncidentKindDefaultValues

·         incidentComplexity [0..1]: IncidentComplexityDefaultValues

·         incidentStartDateTime [0..1]: ct:EDXLDateTimeType

·         geographicSize [1..1]: GeographicSizeType

·         disasterInformation [0..*]: DisasterInformationType

·         jurisdictionInformation [0..*]: JurisdictionInformationType

·         incidentStaging [0..*]: IncidentStagingType
         
         */
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ContextManagerREST.CEP.SituationReaction.EDXL
{
    public class SitRepType
    {
        /// <summary>
        /// Each EDXL-SitRep contains an identifier that uniquely identifies the EDXL-SitRep message / Report.
        /// REQUIRED[1..1]
        /// </summary>
        private string messageID;

        /// <summary>
        /// The person name and/or PositionTitle (ICSPositionTitle when an Incident Management Organization is in place) of the person preparing the information that makes up the message / report and the associated DateTime that this report was prepared  
        /// REQUIRED  [1..1]
        /// </summary>
        private string preparedBy; //[1..1]: ct:PersonTimePairType

        /// <summary>
        /// The person name and/or PositionTitle (ICSPositionTitle when an Incident Management Organziation is in place) of the person formally authorizing the information that makes up the message / report and the associated DateTime that this report was prepared 
        /// REQUIRED  [1..1]
        /// </summary>
        private string authorizedBy; //[1..1]: ct:PersonTimePairType

        /// <summary>
        /// States the purpose of this Situation Report. May contain description information regarding why the report is being sent and required response or action, if any.
        /// REQUIRED  [1..1]
        /// </summary>
        private string reportPurpose;

        /// <summary>
        /// A unique number for reporting an incident or event, used to identify each new or updated report instance. Used to support report tracking.
        /// REQUIRED  [1..1]
        /// </summary>
        private int reportNumber;

        /// <summary>
        /// This indicates the current version of the specific SitRep MessageReportType report being submitted from the same source (“authorizedBy”) for the same incident or event.  If only one SitRep will be submitted, indicate BOTH “Initial” and “Final”. 
        /// Default value list: ReportVersionDefaultValues
        /// REQUIRED  [1..1]
        /// </summary>
        private string reportVersion; //[1..1]: ReportVersionDefaultValues

        /// <summary>
        /// orTimePeriod designates the period of time between the <fromDateTime> and the <toDateTime> elements whose definitions immediately follow this element defninition.
        /// forTimePeriod is used by the<reportNumber> and<reportVersion> elements whose definitions immediate precede this element definition..
        /// forTimePeriod SHOULD include all of the time since the last <sitRep> <reportNumber>/<reportVersion> of this type was submitted.
        /// However, if this report is the originating EDXL-SitRep message for an incident, it should cover the time lapsed since the incident or event started.
        /// The forTimePeriod element MUST include one operational period, but MAY also include more than one Operational Period based on agency/organizational reporting requirements.
        /// All elements of information contained in a given EDXL-SitRep message report type always apply only to the forTimePeriod specified by the<fromDateTime> and the<toDateTime>.
        /// REQUIRED  [1..1]
        /// </summary>
        private string forTimePeriod; //[1..1]: ct:TimePeriodType

        /// <summary>
        /// reportTitle is the designation of a more specific title for the SitRep report other than or in addition to the title given as the value of the <sitRep> element.
        /// OPTIONAL [0..1]
        /// </summary>
        private string reportTitle;

        /// <summary>
        /// The name or other identifier of the incident to which the current message refers, that has been assigned to the incident by an authorized agency based on current guidance.The incident number may vary by jurisdiction and profession (e.g. law enforcement vs. Fire).  The incident number may be a computer aided dispatch number, an accounting number, a disaster declaration number, or a combination of the state, unit/agency, and dispatch system number.  “Unknown” is an acceptable value.
        /// REQUIRED; MAY be used more than once [1..*]
        /// </summary>
        private List<string> incidentIDs; //[1..*]: ct:EDXLStringType

        //incidentLifecyclePhase[0..*]: IncidentLifecycleDefaultValues

        private string originatingMessageID;

        //private string precedingMessageID[0..*]: ct:EDXLStringType

        /// <summary>
        /// The code denoting the importance and necessity of the SitRep message
        /// OPTIONAL [0..1]
        /// </summary>
        private UrgencyDefaultValue urgency;

        /// <summary>
        /// The code denoting the severity of the subject incident or event.
        /// REQUIRED [1..1]
        /// </summary>
        private SeverityDefaultValue severity;

        /// <summary>
        /// report is the element used to create an instance of the IReport abstract type and through it, to specify the EDXL-SitRep Report Type of the message in which it is used.
        /// report MUST declare one of the five specific EDXL-SitRep Report Types:
        /// ·         fieldObservation[0..1]: FieldObservationType
        /// ·         situationInformation[0..1]: SituationInformationType
        /// ·         responseResourcesTotals[0..1]: ResponseResourcesTotalsType
        /// ·         casualtyAndIllnessSummary[0..1]: CasualtyAndIllnessSummaryType
        /// ·         managementReportingSummary[0..1]: ManagementReportingSummaryType
        /// REQUIRED; MUST be used once and only once [1..1]
        /// </summary>
        private IReport report; 

        /*
·         reportingLocation[0..1]: ct:EDXLLocationType
        
·         reportConfidence[1..1]: ConfidenceDefaultValues

·         actionPlan[0..1]: ct:EDXLStringType

·         nextContact[0..1]: ct:EDXLDateTimeType

*/


        public UrgencyDefaultValue Urgency
        {
            get
            {
                return urgency;
            }

            set
            {
                urgency = value;
            }
        }

        public SeverityDefaultValue Severity
        {
            get
            {
                return severity;
            }

            set
            {
                severity = value;
            }
        }

        public SitRepType(string _messageID, string _preparedBy, string _authorizedBy, string _reportPurpose, int _reportNumber, string _reportVersion, string _forTimePeriod)
        {
            this.messageID = _messageID;
            this.preparedBy = _preparedBy;
            this.authorizedBy = _authorizedBy;
            this.reportPurpose = _reportPurpose;
            this.reportNumber = _reportNumber;
            this.reportVersion = _reportVersion;
            this.forTimePeriod = _forTimePeriod;
        }

    }

    public enum UrgencyDefaultValue
    {
        Immediate = 1, // Responsive action SHOULD be taken immediately.   
        Expected = 2,  // Responsive action SHOULD be taken soon (within next hour). 
        Future = 3,    // Responsive action SHOULD be taken in the near future.
        Past = 4,      // Responsive action is no longer required.
        Unknown = 5    // Urgency not known.
    }

    public enum SeverityDefaultValue
    {
        Extreme = 1,   // Extraordinary threat to life or property.
        Severe = 2,    // Significant threat to life or property.
        Moderate = 3,  // Possible threat to life or property.
        Minor = 4,     // Minimal threat to life or property.
        Unknown = 5    // Severity unknown
    }
}

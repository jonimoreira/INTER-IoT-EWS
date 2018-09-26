using INTERIoTEWS.Context.DataObjects.SOSA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using VDS.RDF.Parsing;
using VDS.RDF;
using VDS.RDF.Query;

namespace INTERIoTEWS.UIprototype.SemanticTranslationsValidation
{
    public class SemanticTranslationsValidator
    {
        
        internal AccuracyResult ComputeAccuracyOutputTypeOpt1SPARQL(JObject payloadSAREF, List<Observation> resultOpt1SPARQL_SSN_SOSA)
        {
            AccuracyResult result = new SemanticTranslationsValidation.AccuracyResult();
            result.OutputType = OutputType.SemanticallyMeaningfulMessage;

            var jsonLdParser = new JsonLdParser();
            TripleStore tStoreSAREF = new TripleStore();
            string contentSAREF = payloadSAREF.ToString(Newtonsoft.Json.Formatting.Indented);
            using (var reader = new System.IO.StringReader(contentSAREF))
            {
                jsonLdParser.Load(tStoreSAREF, reader);
            }
            
            // Verify counts: on error -> OutputType.SyntacticalInvalidMessage 
            // 1. Measurement->Observation
            int countMeasurements = GetCountMeasurements(tStoreSAREF);
            int countObservations = resultOpt1SPARQL_SSN_SOSA.Count;
            if (countMeasurements != countObservations)
            {
                result.MissingInformation.AddCountInconsistency("Measurement->Observation", countMeasurements, countObservations);
                result.OutputType = OutputType.SyntacticalInvalidMessage;
            }

            /*
            // 2. Sensor->Sensor
            int countSensorsSAREF = GetCountSensorsSAREF(tStoreSAREF);
            int countSensorsSSN_SOSA = GetCountSensorsSSN_SOSA(resultOpt1SPARQL_SSN_SOSA);
            if (countSensorsSAREF != countSensorsSSN_SOSA)
            {
                result.MissingInformation.AddCountInconsistency("Sensor->Sensor", countSensorsSAREF, countSensorsSSN_SOSA);
                result.OutputType = OutputType.SyntacticalInvalidMessage;
                return result;
            }
            */
            // Verify content: on error -> OutputType.SemanticallyMeaninglessMessage
            result = CompareMeasurementsFields(tStoreSAREF, resultOpt1SPARQL_SSN_SOSA);

            return result;
        }

        private AccuracyResult CompareMeasurementsFields(TripleStore tStoreSAREF, List<Observation> observationsSSN_SOSA)
        {
            AccuracyResult result = new SemanticTranslationsValidation.AccuracyResult();

            Dictionary<string, Sensor> sensorsSAREF = GetSensorsSAREF(tStoreSAREF);
            Dictionary<string, Sensor> sensorsSSN_SOSA = GetSensorsSSN_SOSA(observationsSSN_SOSA);
            foreach (string sensorId in sensorsSAREF.Keys)
            {
                if (!sensorsSSN_SOSA.ContainsKey(sensorId))
                {
                    result.MissingInformation.AddContenetInconsistency("Sensor.Identifier->Sensor.Identifier", sensorId);
                    break;
                }
            }
            
            return result;
        }

        private Dictionary<string, Sensor> GetSensorsSAREF(TripleStore tStoreSAREF)
        {
            Dictionary<string, Sensor> result = new Dictionary<string, Sensor>();

            string sparqlQuery = @"
                PREFIX saref: <https://w3id.org/saref#>
                
                SELECT ?sensor 
                WHERE  
	                {
					?sensor a saref:Sensor.
	                } ";

            SparqlQueryParser sparqlparser = new SparqlQueryParser();
            SparqlQuery query = sparqlparser.ParseFromString(sparqlQuery);
            query.Timeout = 999999;

            Object results = tStoreSAREF.ExecuteQuery(query);

            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                foreach (SparqlResult spqlResult in rset)
                {
                    string sensorId = spqlResult["sensor"].ToString();
                    Sensor sensor = new Sensor(sensorId, null, sensorId);
                    result.Add(sensorId, sensor);
                }
            }

            return result;
        }

        private int GetCountSensorsSSN_SOSA(List<Observation> observationsSSN_SOSA)
        {
            return GetSensorsSSN_SOSA(observationsSSN_SOSA).Count;
        }

        private Dictionary<string, Sensor> GetSensorsSSN_SOSA(List<Observation> observationsSSN_SOSA)
        {
            Dictionary<string, Sensor> dicSensors = new Dictionary<string, Sensor>();
            foreach (Observation observation in observationsSSN_SOSA)
            {
                if (!dicSensors.ContainsKey(observation.madeBySensor.Identifier.ToString()))
                    dicSensors.Add(observation.madeBySensor.Identifier.ToString(), observation.madeBySensor);
            }
            return dicSensors;
        }

        private int GetCountSensorsSAREF(TripleStore tStoreSAREF)
        {
            int result = 0;

            string sparqlQuery = @"
                PREFIX saref: <https://w3id.org/saref#>
                
                SELECT * 
                WHERE  
	                {
					?sensor a saref:Sensor.
	                } ";

            SparqlQueryParser sparqlparser = new SparqlQueryParser();
            SparqlQuery query = sparqlparser.ParseFromString(sparqlQuery);
            query.Timeout = 999999;

            Object results = tStoreSAREF.ExecuteQuery(query);

            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                result = rset.Count;
            }

            return result;
        }

        private int GetCountMeasurements(TripleStore tStoreSAREF)
        {
            int result = 0;
            
            string sparqlQuery = @"
                PREFIX saref: <https://w3id.org/saref#>
                
                SELECT * 
                WHERE  
	                {
					?measurement a saref:Measurement.
	                } ";
            
            SparqlQueryParser sparqlparser = new SparqlQueryParser();
            SparqlQuery query = sparqlparser.ParseFromString(sparqlQuery);
            query.Timeout = 999999;

            Object results = tStoreSAREF.ExecuteQuery(query);

            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                result = rset.Count;
            }

            return result;
        }

        internal OutputType ComputeAccuracyOutputTypeOpt2IPSM(JObject payload, JObject resultOpt2IPSM)
        {
            throw new NotImplementedException();
        }
        

        internal void AddResultOpt2IPSM(string messageId, JObject payloadSAREF, JObject resultOpt2IPSM, OutputType resultAccuracyOpt2IPSM)
        {
            throw new NotImplementedException();
        }

        internal void CompareAccuracy()
        {
            //throw new NotImplementedException();
        }

        internal void AddResultOpt1SPARQL(string messageId, JObject payloadSAREF, List<Observation> resultSSN_SOSA, AccuracyResult resultAccuracyOpt1SPARQL)
        {
            ResultOpt1SPARQL resultOpt1SPARQL = new SemanticTranslationsValidation.ResultOpt1SPARQL(messageId, payloadSAREF, resultSSN_SOSA, resultAccuracyOpt1SPARQL);
            listResultOpt1SPARQL.Add(resultOpt1SPARQL);
        }

        List<ResultOpt1SPARQL> listResultOpt1SPARQL = new List<ResultOpt1SPARQL>();

    }

    public class MissingInformation
    {
        private List<Inconsistency> inconsistencies = new List<Inconsistency>();

        public List<Inconsistency> Inconsistencies
        {
            get
            {
                return inconsistencies;
            }

            set
            {
                inconsistencies = value;
            }
        }

        internal void AddContenetInconsistency(string translationPart, string missingInfoAsSAREF)
        {
            Inconsistency inconsistency = new SemanticTranslationsValidation.Inconsistency(translationPart, missingInfoAsSAREF);
            Inconsistencies.Add(inconsistency);
        }

        internal void AddCountInconsistency(string translationPart, int countSAREF, int countSSN_SOSA)
        {
            Inconsistency inconsistency = new SemanticTranslationsValidation.Inconsistency(translationPart, countSAREF, countSSN_SOSA);
            Inconsistencies.Add(inconsistency);
        }

        
    }

    public enum InconsistencyType
    {
        Syntax = 1,
        Semantic = 2,
    }

    public class Inconsistency
    {
        private int countSAREF;
        private int countSSN_SOSA;
        private string translationPart;
        private string missingAsSAREF;
        InconsistencyType type = InconsistencyType.Syntax;

        public Inconsistency(string translationPart, int countSAREF, int countSSN_SOSA)
        {
            this.TranslationPart = translationPart;
            this.CountSAREF = countSAREF;
            this.CountSSN_SOSA = countSSN_SOSA;
        }

        public Inconsistency(string translationPart, string missingAsSAREF)
        {
            this.TranslationPart = translationPart;
            this.MissingAsSAREF = missingAsSAREF;
            Type = InconsistencyType.Semantic;
        }

        public int CountSAREF
        {
            get
            {
                return countSAREF;
            }

            set
            {
                countSAREF = value;
            }
        }

        public int CountSSN_SOSA
        {
            get
            {
                return countSSN_SOSA;
            }

            set
            {
                countSSN_SOSA = value;
            }
        }

        public string TranslationPart
        {
            get
            {
                return translationPart;
            }

            set
            {
                translationPart = value;
            }
        }

        public string MissingAsSAREF
        {
            get
            {
                return missingAsSAREF;
            }

            set
            {
                missingAsSAREF = value;
            }
        }

        public InconsistencyType Type
        {
            get
            {
                return type;
            }

            set
            {
                type = value;
            }
        }
    }

    public enum OutputType
    {
        UntranslatableSourceMessage = 1,

        SyntacticalInvalidMessage = 2,

        /// <summary>
        /// Semantically meaningless message indicates an output in which a translated target message from 
        /// a gateway system does not comply with the semantic rules of a target message. 
        /// For example, source field data A and B have no semantic rule in an inputted message. 
        /// However, converted field data A? and B? have a semantic rule. These field data violate the 
        /// semantic rule in a target message, and it describes meaningless semantic information in the 
        /// target message.
        /// 
        /// Metrics (counts):
        /// - count measurements/observations
        /// - count sensor/sensor
        /// - count device/device
        /// </summary>
        SemanticallyMeaninglessMessage = 3,

        /// <summary>
        /// Semantically meaningful message indicates an output in which a translated target message from a 
        /// gateway sys- tem complies with syntax and semantic rules of a target message. For example, 
        /// converted field data are successfully composed as a target message, and it provides complete 
        /// semantic information.
        /// 
        /// Metrics (content):
        /// - sensor.measurements - sensor.observations
        /// - units/values
        /// - instance transformations (e.g. BatteryLevel, ElectricPotential_MilliVolts)
        /// 
        /// </summary>
        SemanticallyMeaningfulMessage = 4
    }



    public class AccuracyResult
    {
        private MissingInformation missingInformation;
        private OutputType outputType = OutputType.SyntacticalInvalidMessage;

        public MissingInformation MissingInformation
        {
            get
            {
                if (missingInformation == null) missingInformation = new MissingInformation();
                return missingInformation;
            }

            set
            {
                missingInformation = value;
            }
        }

        public OutputType OutputType
        {
            get
            {
                return outputType;
            }

            set
            {
                outputType = value;
            }
        }
    }

    public enum TranslationResultCase
    {
        Untranslatable = 1,
        SyntaxError = 2,
        SemanticError = 3,
        SemanticDistorted = 4,
        SemanticallyEquivalent = 5
    }


    public class ResultOpt1SPARQL
    {
        private string messageId;
        private JObject payloadSAREF;
        private List<Observation> resultSSN_SOSA;
        private AccuracyResult resultAccuracyOpt1SPARQL;

        public ResultOpt1SPARQL(string messageId, JObject payloadSAREF, List<Observation> resultSSN_SOSA, AccuracyResult resultAccuracyOpt1SPARQL)
        {
            this.messageId = messageId;
            this.payloadSAREF = payloadSAREF;
            this.resultSSN_SOSA = resultSSN_SOSA;
            this.resultAccuracyOpt1SPARQL = resultAccuracyOpt1SPARQL;
        }
    }
}

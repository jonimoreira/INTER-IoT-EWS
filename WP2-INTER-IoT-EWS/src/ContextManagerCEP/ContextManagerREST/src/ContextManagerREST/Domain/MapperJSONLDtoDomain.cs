using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ContextManagerREST.Domain
{
    public class MapperJSONLDtoDomain
    {
        private JToken inputData;

        public MapperJSONLDtoDomain(JToken input)
        {
            inputData = input;
        }

        public List<Sensor> ExecuteMappings()
        {
            // Check if the message is related to e-Health (from Shimmer3 app: @context contains SAREF4health (FHIR? UFO ECG?)) or logistics (from MyDriving app: @context contains LogiCO)
            JToken context = inputData["@context"];
            if (context == null)
                throw new Exception("@context not provided");

            bool isEHealthMessage = false;
            bool isLogisticsMessage = false;

            foreach (JToken token in context.Children().Values())
            {
                if (token.ToString() == "https://w3id.org/def/saref4health#")
                    isEHealthMessage = true;
                if (token.ToString() == "http://ontology.tno.nl/logico#")
                    isLogisticsMessage = true;
            }

            List<Sensor> result = new List<Domain.Sensor>();

            if (isEHealthMessage)
                result.AddRange(ExecuteMappingsEhealth());

            if (isLogisticsMessage)
                result.AddRange(ExecuteMappingsLogistics());

            return null;
        }

        private List<Sensor> ExecuteMappingsLogistics()
        {
            List<Sensor> result = new List<Domain.Sensor>();

            return result;
        }

        private List<Sensor> ExecuteMappingsEhealth()
        {
            List<Sensor> result = new List<Domain.Sensor>();
            // Load Accelerometer
            JToken ecgDeviceConsistsOf = inputData["saref:consistsOf"];
            if (ecgDeviceConsistsOf != null)
            {
                foreach (JToken sensor in ecgDeviceConsistsOf.Children())
                {
                    if (sensor["@type"] != null)
                    {
                        string sensorId = sensor["@id"].ToString();
                        string sensorType = sensor["@type"].ToString();
                        switch (sensorType)
                        {
                            case "saref4health:AccelerometerSensor":

                                double triAxialAccel = 0;
                                bool collisionDetected = false;
                                foreach (JToken measurement in sensor["makesMeasurement"].Children())
                                {
                                    string isMeasuredIn = measurement["isMeasuredIn"].ToString();
                                    string measurementRelatesToProperty = measurement["relatesToProperty"].ToString();
                                    string measurementId = measurement["@id"].ToString();

                                    switch (measurementRelatesToProperty)
                                    {
                                        case "sarefInst:Acceleration_Vehicle":
                                            triAxialAccel = double.Parse(measurement["hasValue"].ToString(), CultureInfo.InvariantCulture);
                                            break;
                                        case "sarefInst:VehicleCollisionDetectedFromMobileDevice":
                                            collisionDetected = measurement["hasValue"].ToString() != "0";
                                            break;
                                        default:
                                            break;
                                    }
                                }

                                AccelerometerSensor accelSensor = new AccelerometerSensor(sensorId, 0, 0, 0, triAxialAccel, collisionDetected);
                                result.Add(accelSensor);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            return result;
        }
    }
}

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
            List<Sensor> result = new List<Domain.Sensor>();

            // Load Accelerometer
            JToken ecgDeviceConsistsOf = inputData["consistsOf"];
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

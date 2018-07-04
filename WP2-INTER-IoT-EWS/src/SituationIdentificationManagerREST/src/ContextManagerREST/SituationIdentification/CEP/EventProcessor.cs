using com.espertech.esper.client;
using com.espertech.esper.core.service;
using INTERIoTEWS.Context.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace INTERIoTEWS.SituationIdentificationManager.SituationIdentificationREST.SituationIdentification.CEP
{
    class EventProcessor
    {
        private static EPServiceProvider epService = EPServiceProviderManager.GetDefaultProvider();

        public EventProcessor()
        {
            this.advertiseEventTypes();
            
        }


        private void advertiseEventTypes()
        {
            var configuration = epService.EPAdministrator.Configuration;

            //HACK: make EventTypes known manually, because NEsper does not seem to recognize classes in linked libraries
            var type = typeof(IEsperEvent);
            var types = AppDomain.CurrentDomain.GetAssemblies().ToList()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && p != type);

            foreach (var eventType in types)
            {
                configuration.AddEventType(eventType.Name, eventType);
            }
        }

        private EPStatement createStatement(string name, string expression)
        {
            var statement = epService.EPAdministrator.CreateEPL(expression, name);
            statement.Events += defaultUpdateEventHandler;

            Console.WriteLine("Statement created: " + name);
            Console.WriteLine(expression);

            return statement;
        }

        public void CreateStatements()
        {
            
            {
                // Get observation regarding the detected collision
                //     AND the corss axial function processed by the smartphone to detect the collision: join by 
                string statementName = "UC01_VehicleCollisionDetected_ST01";
                var expr = @"
                    SELECT o.madeBySensor.Identifier AS sensorId, o.Identifier AS observationId, o.observedProperty, 
                           o.resultTime AS TriggerSituationEventTimeStamp, o.hasResult.hasValue AS resultValue, o.hasResult.hasUnit AS resultUnit,
                           o.madeBySensor.isHostedBy.location.Lat AS latitude, o.madeBySensor.isHostedBy.location.Long AS longitude,
                           CrossAxialUsedToDetectCollision.hasResult.hasValue AS ComputedCrossAxialValue 
                    FROM  VehicleCollisionDetectedObservation.win:time(1 second) AS o 
                            INNER JOIN 
                          Observation.win:time(1 second) AS CrossAxialUsedToDetectCollision ON o.MessageId = CrossAxialUsedToDetectCollision.MessageId
                    WHERE o.Value = true
                        AND CrossAxialUsedToDetectCollision.observedProperty.Label = 'https://w3id.org/saref/instances#CrossAxialFunction'
                ";

                createStatement(statementName, expr);

            }
            
            {
                string statementName = "UC01_VehicleCollisionDetected_ST02";
                var expr = @"
                    SELECT AccelerationAxisX.madeBySensor.Identifier AS sensorId, AccelerationAxisX.Identifier AS observationId, AccelerationAxisX.observedProperty, 
                           AccelerationAxisX.resultTime AS TriggerSituationEventTimeStamp, AccelerationAxisX.hasResult.hasValue AS resultValue, AccelerationAxisX.hasResult.hasUnit AS resultUnit,
                           AccelerationAxisX.madeBySensor.isHostedBy.location.Lat AS latitude, AccelerationAxisX.madeBySensor.isHostedBy.location.Long AS longitude
                           , (Math.Pow(AccelerationAxisX.hasResult.hasValue, 2) + Math.Pow(AccelerationAxisY.hasResult.hasValue, 2)) AS ComputedCrossAxialValue
                           , AccelerationAxisX.observedProperty.Label, AccelerationAxisY.observedProperty.Label, AccelerationAxisZ.observedProperty.Label
                           , AccelerationAxisX.hasResult.hasValue, AccelerationAxisY.hasResult.hasValue, AccelerationAxisZ.hasResult.hasValue
                    FROM  Observation.win:time(1 second) AS AccelerationAxisX, Observation.win:time(1 second) AS AccelerationAxisY, Observation.win:time(1 second) AS AccelerationAxisZ
                    WHERE AccelerationAxisX.MessageId = AccelerationAxisY.MessageId
                          AND AccelerationAxisY.MessageId = AccelerationAxisZ.MessageId
                          AND AccelerationAxisX.observedProperty.Label = 'https://w3id.org/saref/instances#Acceleration_Average_AxisX'
                          AND AccelerationAxisY.observedProperty.Label = 'https://w3id.org/saref/instances#Acceleration_Average_AxisY'
                          AND Math.Sqrt(Math.Pow(AccelerationAxisX.hasResult.hasValue, 2) + Math.Pow(AccelerationAxisY.hasResult.hasValue, 2)) > 3
                ";

                // TODO: check the bug when using Z axis 

                /*
                 * ,
                           (Math.Pow(AccelerationAxisX.hasResult.hasValue, 2) + Math.Pow(AccelerationAxisY.hasResult.hasValue, 2) + Math.Pow(AccelerationAxisZ.hasResult.hasValue, 2)) AS ComputedCrossAxialValue
                 * 
                 WHERE AccelerationAxisX.observedProperty.Label = 'https://w3id.org/saref/instances#Acceleration_Average_AxisX'
                        AND AccelerationAxisY.observedProperty.Label = 'https://w3id.org/saref/instances#Acceleration_Average_AxisY'
                        AND AccelerationAxisZ.observedProperty.Label = 'https://w3id.org/saref/instances#Acceleration_Average_AxisZ'
                        AND Math.Sqrt(Math.Pow(AccelerationAxisX.hasResult.hasValue, 2) + Math.Pow(AccelerationAxisY.hasResult.hasValue, 2) + Math.Pow(AccelerationAxisZ.hasResult.hasValue, 2)) > 0
                 */
                createStatement(statementName, expr);

            }

            /*
            {
                string statementName = "UC01_VehicleCollisionDetected_ST03";
                var expr = @"
                    SELECT AccelerationAxisX.madeBySensor.Identifier AS sensorId, AccelerationAxisX.Identifier AS observationId, AccelerationAxisX.observedProperty, 
                           AccelerationAxisX.resultTime AS TriggerSituationEventTimeStamp, AccelerationAxisX.hasResult.hasValue AS resultValue, AccelerationAxisX.hasResult.hasUnit AS resultUnit,
                           AccelerationAxisX.madeBySensor.isHostedBy.location.Lat AS latitude, AccelerationAxisX.madeBySensor.isHostedBy.location.Long AS longitude
                           , AccelerationAxisX.observedProperty.Label, AccelerationAxisY.observedProperty.Label, AccelerationAxisZ.observedProperty.Label
                           , AccelerationAxisX.hasResult.hasValue, AccelerationAxisY.hasResult.hasValue, AccelerationAxisZ.hasResult.hasValue
                    FROM  Observation.win:time(2 second) AS AccelerationAxisX 
                            INNER JOIN 
                          Observation.win:time(2 second) AS AccelerationAxisY ON AccelerationAxisX.MessageId = AccelerationAxisY.MessageId
                            INNER JOIN 
                          Observation.win:time(2 second) AS AccelerationAxisZ ON AccelerationAxisY.MessageId = AccelerationAxisZ.MessageId
                    WHERE AccelerationAxisX.observedProperty.Label = 'https://w3id.org/saref/instances#Acceleration_Average_AxisZ'
                ";
                
                createStatement(statementName, expr);

            }

            
            {
                string statementName = "UC01_VehicleCollisionDetected_ST02";
                var expr = @"
                    SELECT o.madeBySensor.Identifier AS sensorId, o.Identifier AS observationId, o.observedProperty, 
                           o.resultTime AS TriggerSituationEventTimeStamp, o.hasResult.hasValue AS resultValue, o.hasResult.hasUnit AS resultUnit,
                           o.madeBySensor.isHostedBy.location.Lat AS latitude, o.madeBySensor.isHostedBy.location.Long AS longitude 
                    FROM  Observation AS o 
                    WHERE o.hasResult.hasValue > 1000
                ";

                createStatement(statementName, expr);

            }
          
            {
                string statementName = "OverallAverageAcceleration";
                var expr =
                    "SELECT avg(AccelerationX) \n" +
                    " FROM  AccelerometerSensor.win:time(15 sec) \n" +
                    " WHERE Identifier  =  \"sarefInst:Shimmer3AccelerometerSensor_T9JRN42\" \n";
                
                createStatement(statementName, expr);
                
            }

            {
                string statementName = "VehicleCollisionDetected";
                var expr =
                    "SELECT Identifier \n" +
                    " FROM  AccelerometerSensor \n" +
                    " WHERE VehicleCollisionDetectedFromMobileDevice  =  true \n";

                createStatement(statementName, expr);

            }

            
            {
                string statementName = "IndividualAverageAcceleration";
                var expr =
                    "SELECT    Identifier, \n" +
                    "          avg(Acceleration)  \n" +
                    " FROM     AccelerometerSensor.win:time(30 sec) \n" +
                    " GROUP BY Identifier";

                createStatement(statementName, expr);
            }
            */
            //EPServiceProvider epService = EPServiceProviderManager.GetDefaultProvider();
            //epService.EPAdministrator.GetStatement("OverallAverageAcceleration").Events += OnOverallAverageAcceleration;

        }


        private void defaultUpdateEventHandler(object sender, UpdateEventArgs e)
        {
            var attributes = (e.NewEvents.FirstOrDefault().Underlying as Dictionary<String, object>);
            try
            {
                if (sender is StatementResultServiceImpl)
                {
                    StatementResultServiceImpl result = (StatementResultServiceImpl)sender;
                    // TODO: multi-thread calls
                    SituationType situationTypeIdentified = new SituationType(result.StatementName);
                    Situation situationIdentified = new SituationIdentification.Situation(situationTypeIdentified, true, attributes);
                    situationIdentified.Type.SituationReactionProcess.Execute(situationIdentified);


                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error on [defaultUpdateEventHandler]: " + ex.Message);
            }

        }
    }
}

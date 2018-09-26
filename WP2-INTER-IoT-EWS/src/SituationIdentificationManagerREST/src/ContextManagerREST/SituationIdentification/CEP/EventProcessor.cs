using com.espertech.esper.client;
using com.espertech.esper.core.service;
using ContextManager.DataObjects.EDXL.InferenceHandler;
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

        public void StopStatement(string statementName)
        {
            try
            {
                EPStatement statement = epService.EPAdministrator.GetStatement(statementName);

                if (statement != null)
                    statement.Stop();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceInformation("[SituationIdentificationManager] StopStatement: " + ex.Message);
            }
        }

        public void RecreateAllStatements()
        {
            epService.EPAdministrator.DestroyAllStatements();
            CreateStatements();
        }

        public void RestartAllStatements()
        {
            epService.EPAdministrator.StartAllStatements();
        }

        public void CreateStatements()
        {

            //string uc01_threshold_query = SituationInference.thresholdAcceleration.ToString().Replace(",", ".");
            string uc01_threshold_query = ((int)SituationInference.thresholdAcceleration).ToString();

            {
                // 7.1.1.1	Detected with ECG device accelerometer, computed by smartphone
                // Get observation regarding the detected collision processed by the smartphone with high-frequency data from ECG device
                //     AND the corss axial function processed by the smartphone to detect the collision: join by 
                string statementName = "UC01_VehicleCollisionDetected_ST01";
                var expr = @"
                    SELECT o.madeBySensor.Identifier AS sensorId, o.Identifier AS observationId, o.observedProperty, 
                           o.resultTime AS TriggerEventBegin, o.hasResult.hasValue AS resultValue, o.hasResult.hasUnit AS resultUnit,
                           o.madeBySensor.isHostedBy.location.Lat AS latitude, o.madeBySensor.isHostedBy.location.Long AS longitude,
                           CrossAxialUsedToDetectCollision.hasResult.hasValue AS ComputedCrossAxialValue,
                           o.madeBySensor.isHostedBy.tripId AS TripId
                    FROM  VehicleCollisionDetectedObservation.win:time(1 second) AS o 
                            INNER JOIN 
                          Observation.win:time(1 second) AS CrossAxialUsedToDetectCollision ON o.MessageId = CrossAxialUsedToDetectCollision.MessageId
                    WHERE o.Value = true
                        AND CrossAxialUsedToDetectCollision.observedProperty.Label = 'https://w3id.org/saref/instances#CrossAxialFunction'
                ";

                createStatement(statementName, expr);

            }
            
            {
                // 7.1.1.2	Detected with ECG device accelerometer, computed by EWS (cloud)
                // Get acceleration data and copmpute the cross axial function (Threshold in the EPL)
                string statementName = "UC01_VehicleCollisionDetected_ST02";
                var expr = @"
                    SELECT AccelerationAxisX.madeBySensor.Identifier AS sensorId, AccelerationAxisX.Identifier AS observationId, AccelerationAxisX.observedProperty, 
                           AccelerationAxisX.resultTime AS TriggerEventBegin, AccelerationAxisX.hasResult.hasValue AS resultValue, AccelerationAxisX.hasResult.hasUnit AS resultUnit,
                           AccelerationAxisX.madeBySensor.isHostedBy.location.Lat AS latitude, AccelerationAxisX.madeBySensor.isHostedBy.location.Long AS longitude
                           , (Math.Pow(AccelerationAxisX.hasResult.hasValue, 2) + Math.Pow(AccelerationAxisY.hasResult.hasValue, 2) + Math.Pow(AccelerationAxisY.hasResult.hasValue, 2)) AS ComputedCrossAxialValue
                           , Math.Sqrt(Math.Pow(AccelerationAxisX.hasResult.hasValue, 2) + Math.Pow(AccelerationAxisY.hasResult.hasValue, 2) + Math.Pow(AccelerationAxisY.hasResult.hasValue, 2)) AS SqrtFromComputedCrossAxialValue
                           , " + uc01_threshold_query + @"  AS Threshold_From_EWS
                           , AccelerationAxisX.observedProperty.Label, AccelerationAxisY.observedProperty.Label, AccelerationAxisZ.observedProperty.Label
                           , AccelerationAxisX.hasResult.hasValue, AccelerationAxisY.hasResult.hasValue, AccelerationAxisZ.hasResult.hasValue
                           , AccelerationAxisX.madeBySensor.isHostedBy.tripId AS TripId
                    FROM  Observation.win:time(3 second) AS AccelerationAxisX, Observation.win:time(3 second) AS AccelerationAxisY, Observation.win:time(3 second) AS AccelerationAxisZ
                    WHERE AccelerationAxisX.MessageId = AccelerationAxisY.MessageId
                          AND AccelerationAxisY.MessageId = AccelerationAxisZ.MessageId
                          AND AccelerationAxisX.madeBySensor.label.StartsWith('Accelerometer ECG device')
                          AND AccelerationAxisX.observedProperty.Label = 'https://w3id.org/saref/instances#Acceleration_Average_AxisX'
                          AND AccelerationAxisY.observedProperty.Label = 'https://w3id.org/saref/instances#Acceleration_Average_AxisY'
                          AND Math.Sqrt(Math.Pow(AccelerationAxisX.hasResult.hasValue, 2) + Math.Pow(AccelerationAxisY.hasResult.hasValue, 2) + Math.Pow(AccelerationAxisY.hasResult.hasValue, 2)) > " + uc01_threshold_query;

                // TODO: AND AccelerationAxisX.madeBySensor.isHostedBy.label = 'Shimmer3ECG'
                // instead of 'Accelerometer ECG device: average acceleration within device-cloud frequency (N x ECG unit sampling rate)'
                // TODO: check bug when using Z axis in where clause
                /*
                 * TODO: check use of identifier or @type:
                 *        AND AccelerationAxisX.observedProperty.Identifier.ToString() = 'https://w3id.org/saref/instances#Acceleration_Average_AxisX'
                          AND AccelerationAxisY.observedProperty.Identifier.ToString() = 'https://w3id.org/saref/instances#Acceleration_Average_AxisY'
                 * 
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

            {
                // 7.1.1.3	Detected with smartphone accelerometer, computed by smartphone
                // Get observation regarding the detected collision processed by the smartphone 
                string statementName = "UC01_VehicleCollisionDetected_ST03";
                var expr = @"
                    SELECT o.madeBySensor.Identifier AS sensorId, o.Identifier AS observationId, o.observedProperty, 
                           o.resultTime AS TriggerEventBegin, o.hasResult.hasValue AS resultValue, o.hasResult.hasUnit AS resultUnit,
                           o.madeBySensor.isHostedBy.location.Lat AS latitude, o.madeBySensor.isHostedBy.location.Long AS longitude,
                           CrossAxialUsedToDetectCollision.hasResult.hasValue AS ComputedCrossAxialValue,
                           o.madeBySensor.isHostedBy.tripId AS TripId
                    FROM  VehicleCollisionDetectedObservation.win:time(1 second) AS o 
                            INNER JOIN 
                          Observation.win:time(1 second) AS CrossAxialUsedToDetectCollision ON o.MessageId = CrossAxialUsedToDetectCollision.MessageId
                    WHERE o.Value = false
                        AND o.AccelerationFromSmartphone = true
                        AND CrossAxialUsedToDetectCollision.observedProperty.Label = 'https://w3id.org/saref/instances#CrossAxialFunction'
                ";

                createStatement(statementName, expr);

            }

            string queryForUC01_VehicleCollisionDetected_ST04 = string.Empty;

            {
                // 7.1.1.4	Detected with smartphone accelerometer, computed by EWS (cloud)
                // Get acceleration data and copmpute the cross axial function (Threshold in the EPL)
                string statementName = "UC01_VehicleCollisionDetected_ST04";
                var expr = @"
                    SELECT AccelerationAxisX.madeBySensor.Identifier AS sensorId, AccelerationAxisX.Identifier AS observationId, AccelerationAxisX.observedProperty, 
                           AccelerationAxisX.resultTime AS TriggerEventBegin, AccelerationAxisX.hasResult.hasValue AS resultValue, AccelerationAxisX.hasResult.hasUnit AS resultUnit,
                           AccelerationAxisX.madeBySensor.isHostedBy.location.Lat AS latitude, AccelerationAxisX.madeBySensor.isHostedBy.location.Long AS longitude
                           , (Math.Pow(AccelerationAxisX.hasResult.hasValue, 2) + Math.Pow(AccelerationAxisY.hasResult.hasValue, 2) + Math.Pow(AccelerationAxisY.hasResult.hasValue, 2)) AS ComputedCrossAxialValue
                           , Math.Sqrt(Math.Pow(AccelerationAxisX.hasResult.hasValue, 2) + Math.Pow(AccelerationAxisY.hasResult.hasValue, 2) + Math.Pow(AccelerationAxisY.hasResult.hasValue, 2)) AS SqrtFromComputedCrossAxialValue
                           , " + uc01_threshold_query + @"  AS Threshold_From_EWS
                           , AccelerationAxisX.observedProperty.Label, AccelerationAxisY.observedProperty.Label, AccelerationAxisZ.observedProperty.Label
                           , AccelerationAxisX.hasResult.hasValue, AccelerationAxisY.hasResult.hasValue, AccelerationAxisZ.hasResult.hasValue
                           , AccelerationAxisX.madeBySensor.isHostedBy.tripId AS TripId
                    FROM  Observation.win:time(3 second) AS AccelerationAxisX, Observation.win:time(3 second) AS AccelerationAxisY, Observation.win:time(3 second) AS AccelerationAxisZ
                    WHERE AccelerationAxisX.MessageId = AccelerationAxisY.MessageId
                          AND AccelerationAxisY.MessageId = AccelerationAxisZ.MessageId
                          AND AccelerationAxisX.madeBySensor.label.StartsWith('Accelerometer smartphone')
                          AND AccelerationAxisX.observedProperty.Label = 'https://w3id.org/saref/instances#Acceleration_Average_AxisX'
                          AND AccelerationAxisY.observedProperty.Label = 'https://w3id.org/saref/instances#Acceleration_Average_AxisY'
                          AND Math.Sqrt(Math.Pow(AccelerationAxisX.hasResult.hasValue, 2) + Math.Pow(AccelerationAxisY.hasResult.hasValue, 2) + Math.Pow(AccelerationAxisY.hasResult.hasValue, 2)) > " + uc01_threshold_query;

                createStatement(statementName, expr);

                queryForUC01_VehicleCollisionDetected_ST04 = expr;

            }

            string queryForUC01_VehicleCollisionDetected_ST05 = string.Empty;

            {
                // 7.1.1.5	Detected with ECG device and smartphone accelerometers, computed by EWS (cloud)
                // Vehicle collision with two accelerometers (from Shimmer3 ECG unit and smartphone) computed by the mobile application (MyDriving-LD): test  ST_UC01_05

                string statementName = "UC01_VehicleCollisionDetected_ST05";
                var expr = @"
                    SELECT AccelerationAxisX.madeBySensor.Identifier AS sensorId, AccelerationAxisX.Identifier AS observationId, AccelerationAxisX.observedProperty, 
                           AccelerationAxisX.resultTime AS TriggerEventBegin, AccelerationAxisX.hasResult.hasValue AS resultValue, AccelerationAxisX.hasResult.hasUnit AS resultUnit,
                           AccelerationAxisX.madeBySensor.isHostedBy.location.Lat AS latitude, AccelerationAxisX.madeBySensor.isHostedBy.location.Long AS longitude
                           , (Math.Pow(AccelerationAxisX.hasResult.hasValue, 2) + Math.Pow(AccelerationAxisY.hasResult.hasValue, 2) + Math.Pow(AccelerationAxisY.hasResult.hasValue, 2)) AS ComputedCrossAxialValue
                           , Math.Sqrt(Math.Pow(AccelerationAxisX.hasResult.hasValue, 2) + Math.Pow(AccelerationAxisY.hasResult.hasValue, 2) + Math.Pow(AccelerationAxisY.hasResult.hasValue, 2)) AS SqrtFromComputedCrossAxialValue
                           , " + uc01_threshold_query + @"  AS Threshold_From_EWS
                           , AccelerationAxisX.observedProperty.Label, AccelerationAxisY.observedProperty.Label, AccelerationAxisZ.observedProperty.Label
                           , AccelerationAxisX.hasResult.hasValue, AccelerationAxisY.hasResult.hasValue, AccelerationAxisZ.hasResult.hasValue
                           , AccelerationAxisX.madeBySensor.isHostedBy.tripId AS TripId
                    FROM  Observation.win:time(3 second) AS AccelerationAxisX, Observation.win:time(3 second) AS AccelerationAxisY, Observation.win:time(3 second) AS AccelerationAxisZ, 
                          Observation.win:time(3 second) AS AccelerationAxisXsphone, Observation.win:time(3 second) AS AccelerationAxisYsphone, Observation.win:time(3 second) AS AccelerationAxisZsphone
                    WHERE AccelerationAxisX.MessageId = AccelerationAxisY.MessageId
                          AND AccelerationAxisY.MessageId = AccelerationAxisZ.MessageId
			              AND AccelerationAxisX.MessageId = AccelerationAxisY.MessageId
                          AND AccelerationAxisX.madeBySensor.label.StartsWith('Accelerometer ECG device')
                          AND AccelerationAxisX.observedProperty.Label = 'https://w3id.org/saref/instances#Acceleration_Average_AxisX'
                          AND AccelerationAxisY.observedProperty.Label = 'https://w3id.org/saref/instances#Acceleration_Average_AxisY'
                          AND Math.Sqrt(Math.Pow(AccelerationAxisX.hasResult.hasValue, 2) + Math.Pow(AccelerationAxisY.hasResult.hasValue, 2) + Math.Pow(AccelerationAxisY.hasResult.hasValue, 2)) > " + uc01_threshold_query + @"


                          AND AccelerationAxisYsphone.MessageId = AccelerationAxisZsphone.MessageId
                          AND AccelerationAxisXsphone.MessageId = AccelerationAxisYsphone.MessageId
                          AND AccelerationAxisXsphone.madeBySensor.label.StartsWith('Accelerometer smartphone')
                          AND AccelerationAxisXsphone.observedProperty.Label = 'https://w3id.org/saref/instances#Acceleration_Average_AxisX'
                          AND AccelerationAxisYsphone.observedProperty.Label = 'https://w3id.org/saref/instances#Acceleration_Average_AxisY'
                          AND Math.Sqrt(Math.Pow(AccelerationAxisXsphone.hasResult.hasValue, 2) + Math.Pow(AccelerationAxisYsphone.hasResult.hasValue, 2) + Math.Pow(AccelerationAxisYsphone.hasResult.hasValue, 2)) > " + uc01_threshold_query;

                createStatement(statementName, expr);

                queryForUC01_VehicleCollisionDetected_ST05 = expr;

            }

            string uc02_thresholdBradycardia_query = SituationInference.thresholdBradycardia.ToString();

            {
                // 7.1.2.1	Bradycardia detected with fixed threshold
                // Get heart rate for bradychardia detection (threshold in EPL) 
                string statementName = "UC02_HealthEarlyWarningScore_ST01";
                var expr = @"
                    SELECT o.madeBySensor.Identifier AS sensorId, o.Identifier AS observationId, o.observedProperty, 
                           o.resultTime AS TriggerEventBegin, o.hasResult.hasValue AS resultValue, o.hasResult.hasUnit AS resultUnit,
                           o.madeBySensor.isHostedBy.location.Lat AS latitude, o.madeBySensor.isHostedBy.location.Long AS longitude,
                           o.madeBySensor.isHostedBy.tripId AS TripId
                    FROM  Observation.win:time(1 second) AS o
                    WHERE o.observedProperty.Label = 'https://w3id.org/saref/instances#HeartRate'
                        AND o.hasResult.hasValue > -0.1
                        AND o.hasResult.hasValue < " + uc02_thresholdBradycardia_query + @"
                ";

                createStatement(statementName, expr);

            }

            string uc02_thresholdTachycardia_query = SituationInference.thresholdTachycardia.ToString();

            {
                // 7.1.2.2	Tachycardia detected with fixed threshold
                string statementName = "UC02_HealthEarlyWarningScore_ST02";
                var expr = @"
                    SELECT o.madeBySensor.Identifier AS sensorId, o.Identifier AS observationId, o.observedProperty, 
                           o.resultTime AS TriggerEventBegin, o.hasResult.hasValue AS resultValue, o.hasResult.hasUnit AS resultUnit,
                           o.madeBySensor.isHostedBy.location.Lat AS latitude, o.madeBySensor.isHostedBy.location.Long AS longitude,
                           o.madeBySensor.isHostedBy.tripId AS TripId
                    FROM  Observation.win:time(1 second) AS o
                    WHERE o.observedProperty.Label = 'https://w3id.org/saref/instances#HeartRate'
                        AND o.hasResult.hasValue > " + uc02_thresholdTachycardia_query + @"
                        
                ";

                createStatement(statementName, expr);

            }
            
            {
                // 7.1.2.3	Multiple occurrences of bradycardia detected with fixed threshold
                // Based on http://esper.espertech.com/release-6.0.1/esper-reference/html/event_patterns.html            
                // Pattern: detects when 3 sensor events indicate a heart rate of less then THRESHOLD bpm uninterrupted within 20 seconds of the first event, considering events for the same sensor only.
                string statementName = "UC02_HealthEarlyWarningScore_ST03";
                var expr = @"
                     SELECT o.madeBySensor.Identifier AS sensorId, o.Identifier AS observationId, o.observedProperty, 
                           o.resultTime AS TriggerEventBegin, o.hasResult.hasValue AS resultValue, o.hasResult.hasUnit AS resultUnit,
                           o.madeBySensor.isHostedBy.location.Lat AS latitude, o.madeBySensor.isHostedBy.location.Long AS longitude,
                           o.madeBySensor.isHostedBy.tripId AS TripId
                    FROM pattern[every o=Observation(hasResult.hasValue < " + uc02_thresholdBradycardia_query + @", observedProperty.Label = 'https://w3id.org/saref/instances#HeartRate') 
                                    -> 
                                    (
                                    (Observation(observedProperty.Label = 'https://w3id.org/saref/instances#HeartRate', madeBySensor.isHostedBy.tripId=o.madeBySensor.isHostedBy.tripId, hasResult.hasValue < " + uc02_thresholdBradycardia_query + @")) 
                                    ->
                                    (Observation(observedProperty.Label = 'https://w3id.org/saref/instances#HeartRate', madeBySensor.isHostedBy.tripId=o.madeBySensor.isHostedBy.tripId, hasResult.hasValue < " + uc02_thresholdBradycardia_query + @")) 
                                    ) 
                                    where timer:within(20 seconds)
                                ]
                ";
                // add same sensor... if more than 1 sensor of heart rate (>1 devices...)

                createStatement(statementName, expr);
            }

            {
                // 7.1.2.4	Multiple occurrences of tachycardia detected with fixed threshold
                string statementName = "UC02_HealthEarlyWarningScore_ST04";
                var expr = @"
                     SELECT o.madeBySensor.Identifier AS sensorId, o.Identifier AS observationId, o.observedProperty, 
                           o.resultTime AS TriggerEventBegin, o.hasResult.hasValue AS resultValue, o.hasResult.hasUnit AS resultUnit,
                           o.madeBySensor.isHostedBy.location.Lat AS latitude, o.madeBySensor.isHostedBy.location.Long AS longitude,
                           o.madeBySensor.isHostedBy.tripId AS TripId
                    FROM pattern[every o=Observation(hasResult.hasValue > " + uc02_thresholdTachycardia_query + @", observedProperty.Label = 'https://w3id.org/saref/instances#HeartRate')
                                    -> 
                                    (
                                    (Observation(observedProperty.Label = 'https://w3id.org/saref/instances#HeartRate', madeBySensor.isHostedBy.tripId=o.madeBySensor.isHostedBy.tripId, hasResult.hasValue > " + uc02_thresholdTachycardia_query + @")) 
                                    ->
                                    (Observation(observedProperty.Label = 'https://w3id.org/saref/instances#HeartRate', madeBySensor.isHostedBy.tripId=o.madeBySensor.isHostedBy.tripId, hasResult.hasValue > " + uc02_thresholdTachycardia_query + @")) 
                                    ) 
                                    where timer:within(20 seconds)
                                ]
                ";

                createStatement(statementName, expr);
            }

            {
                // 7.1.3.1	Vehicle collision followed by bradycardia
                // Slow heart rate right after (within 2 minutes) a collision is detected can represent that an accident just occurred and the driver is probably injured. 
                // Pattern: detects when 3 sensor events indicate (A) vehicle collision; (B) 2x (ECG) a heart rate of less then THRESHOLD bpm uninterrupted within 20 seconds of the first event, considering events for the same sensor only.
                string statementName = "UC03_TemporalRelations_ST01";
                var expr = @"
                    SELECT o.madeBySensor.Identifier AS sensorId, o.Identifier AS observationId, o.observedProperty, 
                           o.resultTime AS TriggerEventBegin, o.hasResult.hasValue AS resultValue, o.hasResult.hasUnit AS resultUnit,
                           o.madeBySensor.isHostedBy.location.Lat AS latitude, o.madeBySensor.isHostedBy.location.Long AS longitude,
                           o.madeBySensor.isHostedBy.tripId AS TripId
                    FROM pattern[every o=VehicleCollisionDetectedObservation(Value = true) 
                                    -> 
                                    (
                                    (Observation(observedProperty.Label = 'https://w3id.org/saref/instances#HeartRate', madeBySensor.isHostedBy.tripId=o.madeBySensor.isHostedBy.tripId, hasResult.hasValue < " + uc02_thresholdBradycardia_query + @")) 
                                    ->
                                    (Observation(observedProperty.Label = 'https://w3id.org/saref/instances#HeartRate', madeBySensor.isHostedBy.tripId=o.madeBySensor.isHostedBy.tripId, hasResult.hasValue < " + uc02_thresholdBradycardia_query + @")) 
                                    ) 
                                    where timer:within(20 seconds)
                                ]
                ";


                createStatement(statementName, expr);
            }

            {
                // 7.1.3.2	Bradycardia followed by vehicle collision
                // Slow heart rate right before (within 2 minutes) a collision is detected can represent that an accident occurred because the driver had a cardiac issue. 
                // Pattern: detects when 3 sensor events indicate (A) 2x (ECG) a heart rate of less then THRESHOLD bpm; (B) vehicle collision; within 20 seconds of the first event, considering events for the same sensor only.
                string statementName = "UC03_TemporalRelations_ST02";
                var expr = @"
                    SELECT o.madeBySensor.Identifier AS sensorId, o.Identifier AS observationId, o.observedProperty, 
                           o.resultTime AS TriggerEventBegin, o.hasResult.hasValue AS resultValue, o.hasResult.hasUnit AS resultUnit,
                           o.madeBySensor.isHostedBy.location.Lat AS latitude, o.madeBySensor.isHostedBy.location.Long AS longitude,
                           o.madeBySensor.isHostedBy.tripId AS TripId
                    FROM pattern[every o=Observation(hasResult.hasValue < " + uc02_thresholdBradycardia_query + @", observedProperty.Label = 'https://w3id.org/saref/instances#HeartRate') 
                                    -> 
                                    (
                                    (Observation(observedProperty.Label = 'https://w3id.org/saref/instances#HeartRate', madeBySensor.isHostedBy.tripId=o.madeBySensor.isHostedBy.tripId, hasResult.hasValue < " + uc02_thresholdBradycardia_query + @")) 
                                    ->
                                    (VehicleCollisionDetectedObservation(Value = true, madeBySensor.isHostedBy.tripId=o.madeBySensor.isHostedBy.tripId)) 
                                    ) 
                                    where timer:within(20 seconds)
                                ]
                ";


                createStatement(statementName, expr);
            }

            {
                // 7.1.4.1	UC01 with dangerous goods
                string statementName = "UC04_DangerousGoods_ST01";
                var expr = @"
                    SELECT o.madeBySensor.Identifier AS sensorId, o.Identifier AS observationId, o.observedProperty, 
                           o.resultTime AS TriggerEventBegin, o.hasResult.hasValue AS resultValue, o.hasResult.hasUnit AS resultUnit,
                           o.madeBySensor.isHostedBy.location.Lat AS latitude, o.madeBySensor.isHostedBy.location.Long AS longitude,
                           CrossAxialUsedToDetectCollision.hasResult.hasValue AS ComputedCrossAxialValue,
                           o.madeBySensor.isHostedBy.tripId AS TripId,
                           DangerousGoodsTransport.observedProperty.Label AS DangerousGoods
                    FROM  VehicleCollisionDetectedObservation.win:time(1 second) AS o 
                            INNER JOIN 
                          Observation.win:time(1 second) AS CrossAxialUsedToDetectCollision ON o.MessageId = CrossAxialUsedToDetectCollision.MessageId
                            INNER JOIN 
                          DangerousGoodsObservation.win:time(20 second) AS DangerousGoodsTransport ON o.madeBySensor.isHostedBy.tripId = DangerousGoodsTransport.madeBySensor.isHostedBy.tripId
                    WHERE o.Value = true
                        AND CrossAxialUsedToDetectCollision.observedProperty.Label = 'https://w3id.org/saref/instances#CrossAxialFunction'

                ";


                createStatement(statementName, expr);
            }

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
                System.Diagnostics.Trace.TraceError("[SituationIdentificationManager] defaultUpdateEventHandler Error on situation reaction: " + ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine + "InnerException:" + ((ex.InnerException != null)? ex.InnerException.Message: "NULL"));
            }

        }
    }
}

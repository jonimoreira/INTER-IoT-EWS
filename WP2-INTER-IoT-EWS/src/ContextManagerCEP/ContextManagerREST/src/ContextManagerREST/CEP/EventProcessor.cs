using com.espertech.esper.client;
using com.espertech.esper.core.service;
using ContextManagerREST.CEP.SituationIdentification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ContextManagerREST.CEP
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

            /*
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
                    
                    SituationType situationTypeIdentified = new SituationType(result.StatementName);
                    Situation situationIdentified = new SituationIdentification.Situation(situationTypeIdentified, true);
                    situationIdentified.Type.SituationReactionProcess.Execute();


                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error on [defaultUpdateEventHandler]: " + ex.Message);
            }

        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using com.espertech.esper.client;
using ContextManagerREST.SituationIdentification.CEP;

namespace ContextManagerREST
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Starting Event Processor...");
            var eventProcessor = new EventProcessor();
            eventProcessor.CreateStatements();

            Console.WriteLine("Starting RESTful services...");
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(System.IO.Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
            
        }

    }
}

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using log4net;
using log4net.Config;
using NewRelic.Api.Agent;


namespace NewRelic.LogEnrichers.Log4Net.Examples
{
    static class Program
    {
        private static ILog _logger;

        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to the New Relic Logging Extentions for Log4Net");
            Console.WriteLine();

            if (args.Length == 0)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("dotnet run {OutputFolderPath}");
                return;
            }

            var folderPath = string.Join(' ', args);

            var folderPath_StandardLogs = Path.Combine(folderPath, "StandardLogs");
            var folderPath_NewRelicLogs = Path.Combine(folderPath, "NewRelicLogs");

            // Ensure that the output folders exist
            Directory.CreateDirectory(folderPath_StandardLogs);
            Directory.CreateDirectory(folderPath_NewRelicLogs);

            GlobalContext.Properties["StandardLogFileName"] = @$"{folderPath_StandardLogs}\Log4NetExample.log";
            GlobalContext.Properties["NewRelicLogFileName"] = @$"{folderPath_NewRelicLogs}\Log4NetExample.json";

            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
            _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            // This log information will be visible in New Relic Logging. Since 
            // a transaction has not been started, this log message will not be
            // associated to a specific transaction.
            _logger.Info("Hello, welcome to the Log4Net Logs In Context sample app!");

            do
            {
                Console.WriteLine("Creating Logged Transactions");

                // Call example methods that create transactions
                TestMethod(new object());
                // Pass null as an argument to trigger the method to throw argument null exception
                TestMethod(null);

                Console.WriteLine("Press <ENTER> to continue, Q to exit.");
            }
            while (Console.ReadLine() != "Q");

            // This log information will be visible in New Relic Logging. Since 
            // a transaction has not been started, this log message will not be
            // associated to a specific transaction.
            _logger.Info("Thanks for visiting, please come back soon!");
        }

        [Transaction]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void TestMethod(object obj)
        {
            _logger.Info(@$"Starting TestMethod");

            try
            {
                for (var cnt = 0; cnt < 10; cnt++)
                {
                    if (obj == null)
                    {
                        throw new ArgumentNullException("obj");
                    }

                    Console.WriteLine("writing message");
                    _logger.Info(@$"This is log message {cnt}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error has occurred in TestMethod");
                Api.Agent.NewRelic.NoticeError(ex);
                _logger.Error("Error has occurred in TestMethod", ex);
            }

            _logger.Info(@$"Ending TestMethod");
        }
    }
}

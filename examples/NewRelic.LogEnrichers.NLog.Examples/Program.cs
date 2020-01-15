using System;
using System.IO;
using System.Runtime.CompilerServices;
using NewRelic.Api.Agent;
using NLog;
using NLog.Config;
using NLog.Targets;


namespace NewRelic.LogEnrichers.NLog.Examples
{
	static class Program
	{
        private static Logger _logger;

        static void Main(string[] args)
		{
            Console.WriteLine("Welcome to the New Relic Logging Extentions for NLog");
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

            // Create Logger
            _logger = CreateLogger(
                folderPath_StandardLogs: folderPath_StandardLogs,
                folderPath_NewRelicLogs: folderPath_NewRelicLogs);

            // This log information will be visible in New Relic Logging. Since 
            // a transaction has not been started, this log message will not be
            // associated to a specific transaction.
            _logger.Info("Hello, welcome to the Nlog Logs In Context sample app!");

            do
            {
                Console.WriteLine("Creating Logged Transactions");

                // Call three example methods that create transactions
                TestMethod("First Transaction");
                TestMethod("Second Transaction");
                TestMethod("Third Transaction");

                Console.WriteLine("Press <ENTER> to continue, Q to exit.");
            }
            while (Console.ReadLine() != "Q");

            // This log information will be visible in New Relic Logging. Since 
            // a transaction has not been started, this log message will not be
            // associated to a specific transaction.
            _logger.Info("Thanks for visiting, please come back soon!");
        }


        /// <summary>
        /// This method is responsible for configuring the application's logging.
        /// </summary>
        private static Logger CreateLogger(string folderPath_StandardLogs, string folderPath_NewRelicLogs)
        {

            Console.WriteLine($"Standard Logs Folder            : {folderPath_StandardLogs}");
            Console.WriteLine($"New Relic Log Forwarder Source  : {folderPath_NewRelicLogs}");
            Console.WriteLine();

            var loggerConfig = new LoggingConfiguration();

            // CONFIGURE BASIC LOGGING TO A FILE
            // 1.  Add a file target which writes to the standard logs folder with the default layout.
            var standardFileTarget = new FileTarget("StandardFileTarget");
            standardFileTarget.FileName = Path.Combine(folderPath_StandardLogs, "NLogExtensions.log");
            loggerConfig.AddTarget(standardFileTarget);

            // CONFIGURE NEW RELIC LOGGING.
            // 1.  Add a file target which writes to a staging location for the log forwarder
            // 2.  Use the NewRelicJsonLayout which will write the output in the format required by NewRelic, as well
            //     as adding the contextual information linking transaction data (if applicable) to log events.
            var newRelicFileTarget = new FileTarget("NewRelicFileTarget");
            newRelicFileTarget.Layout = new NewRelicJsonLayout();
            newRelicFileTarget.FileName = Path.Combine(folderPath_NewRelicLogs, "NLogExtensions_NewRelicLogging.json");
            loggerConfig.AddTarget(newRelicFileTarget);

            loggerConfig.AddRuleForAllLevels("StandardFileTarget");
            loggerConfig.AddRuleForAllLevels("NewRelicFileTarget");

            LogManager.Configuration = loggerConfig;

            return LogManager.GetLogger("Example");
        }

        /// <summary>
        /// This method will be recorded as a Transaction using the .Net Agent.
        /// With New Relic Logging Configured, the log messages will be associated
        /// to the transaction.
        /// </summary>
        [Transaction]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void TestMethod(string testVal)
        {
            _logger.Info("Starting TestMethod - {testValue}", testVal);

            try
            {
                for (var cnt = 0; cnt < 10; cnt++)
                {
                    Console.WriteLine("writing message");
                    _logger.Info("This is log message #{MessageID}", cnt);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error has occurred in TestMethod - {testValue}", testVal);
            }

            _logger.Info("Ending TestMethod - {testValue}", testVal);
        }
    }
}

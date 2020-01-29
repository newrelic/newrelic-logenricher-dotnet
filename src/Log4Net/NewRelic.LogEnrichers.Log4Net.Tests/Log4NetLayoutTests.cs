using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using log4net;
using log4net.Config;
using log4net.Core;
using NewRelic.Api.Agent;
using Newtonsoft.Json;
using NUnit.Framework;
using Telerik.JustMock;

namespace NewRelic.LogEnrichers.Log4Net.Tests
{
    public class Log4NetLayoutTests
    {

        [TestCase("INFO")]
        [TestCase("WARN")]
        [TestCase("FATAL")]
        [TestCase("ERROR")]
        public void Output_Intrinsics_LogLevel(string level)
        {
            // Arrange
            LogManager.ShutdownRepository(Assembly.GetEntryAssembly());

            var testAgent = Mock.Create<IAgent>();
            Mock.Arrange(() => testAgent.GetLinkingMetadata()).Returns(new Dictionary<string, string>() { { "key1", "value1" } });

            var testAppender = new NewRelicAppender(() => testAgent);

            //Set the the NewRelicAppender at the root logger
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            BasicConfigurator.Configure(logRepository, testAppender);

            var layout = Mock.Create<NewRelicLayout>();

            var childAppender = new log4net.Appender.ConsoleAppender
            {
                Layout = layout
            };

            testAppender.AddAppender(childAppender);

            TextWriter tw = null;

            Mock.Arrange(() => layout.Format(Arg.IsAny<TextWriter>(), Arg.IsAny<LoggingEvent>())).DoInstead((TextWriter textWriter, LoggingEvent loggingEvent) =>
            {
                tw = textWriter;
            }).CallOriginal();

            var testLogger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            //// Act
            testLogger.Log(level, "This is a log message");

            var serializedMessage = tw.ToString();
            var deserializedMessage = TestHelpers.DeserializeOutputJSON(serializedMessage);

            // Assert
            Assert.That(deserializedMessage.ContainsKey("log.level"), "log.level not found.");
            var resultLevel = TestHelpers.DeserializeOutputJSON(deserializedMessage["log.level"].ToString());
            Assert.That(resultLevel["Name"].ToString() == level, "Incorrect logging level");
        }
    }

    public static class CustomLoggingExtensions
    {
        public static void Log(this ILog logger, string level, object message)
        {
            switch (level.ToLower()) 
            {
                case "info":
                    logger.Info(message);
                    break;
                case "debug":
                    logger.Debug(message);
                    break;
                case "warn":
                    logger.Warn(message);
                    break;
                case "fatal":
                    logger.Fatal(message);
                    break;
                case "error":
                    logger.Error(message);
                    break;
                default:
                    throw new Exception(@$"level:{level} not recognized logging level.");
            }
        }
    }
}


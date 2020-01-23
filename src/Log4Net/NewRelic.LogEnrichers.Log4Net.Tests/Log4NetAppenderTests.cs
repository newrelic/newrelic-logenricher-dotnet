using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using log4net;
using log4net.Config;
using log4net.Core;
using NewRelic.Api.Agent;
using NUnit.Framework;
using Telerik.JustMock;

namespace NewRelic.LogEnrichers.Log4Net.Tests
{
	public class Log4NetAppenderTests
    {
        private IAgent _testAgent;

        [SetUp]
		public void Setup()
		{
            _testAgent = Mock.Create<IAgent>();
        }

        [Test]
		public void GetLinkingMetadata_CalledOnceForEachEvent()
		{
            // Arrange
            const string countLogMessage = "This is a log message";

            var testAppender = new NewRelicAppender(() => _testAgent);
            testAppender.ActivateOptions();

            //Set the the NewRelicAppender at the root logger
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            BasicConfigurator.Configure(logRepository, testAppender);

            var testLogger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            var rnd = new Random();
            var countLogAttempts = rnd.Next(2, 25);

            //// Act
            for (var i = 0; i < countLogAttempts; i++)
            {
                testLogger.Info(countLogMessage);
            }

            // Assert
            Mock.Assert(() => _testAgent.GetLinkingMetadata(), Occurs.Exactly(countLogAttempts));
        }

        [Test]
        public void GetLinkingMetadata_IsHandled_NullAgent()
        {
            // Arrange
            var testAppender = new NewRelicAppender(() => _testAgent);
            testAppender.ActivateOptions();

            //Set the the NewRelicAppender at the root logger
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            BasicConfigurator.Configure(logRepository, testAppender);

            var testLogger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            var layout = Mock.Create<log4net.Layout.SimpleLayout>();
            layout.ActivateOptions();

            testAppender.Layout = layout;
            var childAppender = new log4net.Appender.ConsoleAppender();
            childAppender.Layout = layout;

            testAppender.AddAppender(childAppender);

            var testLoggingEvents = new List<LoggingEvent>();

            Mock.Arrange(() => layout.Format(Arg.IsAny<TextWriter>(), Arg.IsAny<LoggingEvent>())).DoInstead((TextWriter textWriter, LoggingEvent loggingEvent) => 
            {
                testLoggingEvents.Add(loggingEvent);
            });

            // Act

            var rnd = new Random();
            var countLogAttempts = rnd.Next(2, 25);

            for (var i = 0; i < countLogAttempts; i++)
            {
                testLogger.Info("This is log message " + i);
            }

            // Assert
            testLoggingEvents.ForEach(loggingEvent =>
            {
                Assert.That(!loggingEvent.Properties.Contains("newrelic.linkingmetadata"), "newrelic.linkingmetadata property found. This test does not expect newrelic.linkingmetadata property in log event");
            });
        }

    }
}

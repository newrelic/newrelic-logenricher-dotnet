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

        [Test]
        public void GetLinkingMetadata_CalledOnceForEachEvent()
        {
            // Arrange
            LogManager.ShutdownRepository(Assembly.GetEntryAssembly());

            var testAgent = Mock.Create<IAgent>();
            Mock.Arrange(() => testAgent.GetLinkingMetadata()).Returns(new Dictionary<string, string>() { { "key", "value" } });

            var testAppender = new NewRelicAppender(() => testAgent);

            //Set the the NewRelicAppender at the root logger
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            BasicConfigurator.Configure(logRepository, testAppender);

            var layout = Mock.Create<log4net.Layout.SimpleLayout>();
            layout.ActivateOptions();

            var childAppender = new log4net.Appender.ConsoleAppender
            {
                Layout = layout
            };

            testAppender.AddAppender(childAppender);

            var testLoggingEvents = new List<LoggingEvent>();

            Mock.Arrange(() => layout.Format(Arg.IsAny<TextWriter>(), Arg.IsAny<LoggingEvent>())).DoInstead((TextWriter textWriter, LoggingEvent loggingEvent) =>
            {
                testLoggingEvents.Add(loggingEvent);
            });

            var testLogger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            //// Act
            var rnd = new Random();
            var countLogAttempts = rnd.Next(2, 25);

            for (var i = 0; i < countLogAttempts; i++)
            {
                testLogger.Info("This is a log message");
            }

            // Assert
            Mock.Assert(() => testAgent.GetLinkingMetadata(), Occurs.Exactly(countLogAttempts));
            Assert.That(testLoggingEvents.Count, Is.EqualTo(countLogAttempts));
            testLoggingEvents.ForEach(loggingEvent =>
            {
                Assert.That(loggingEvent.Properties.Contains("newrelic.linkingmetadata"), "newrelic.linkingmetadata not property found. This test expects newrelic.linkingmetadata property in log event");
            });
        }

        [Test]
        public void GetLinkingMetadata_IsHandled_NullAgent()
        {
            // Arrange
            LogManager.ShutdownRepository(Assembly.GetEntryAssembly());

            var testAppender = new NewRelicAppender(() => null);

            //Set the the NewRelicAppender at the root logger
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            BasicConfigurator.Configure(logRepository, testAppender);

            var layout = Mock.Create<log4net.Layout.SimpleLayout>();
            layout.ActivateOptions();

            var childAppender = new log4net.Appender.ConsoleAppender
            {
                Layout = layout
            };

            testAppender.AddAppender(childAppender);

            var testLoggingEvents = new List<LoggingEvent>();

            Mock.Arrange(() => layout.Format(Arg.IsAny<TextWriter>(), Arg.IsAny<LoggingEvent>())).DoInstead((TextWriter textWriter, LoggingEvent loggingEvent) => 
            {
                testLoggingEvents.Add(loggingEvent);
            });

            var testLogger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            // Act

            var rnd = new Random();
            var countLogAttempts = rnd.Next(2, 25);

            for (var i = 0; i < countLogAttempts; i++)
            {
                testLogger.Info("This is log message " + i);
            }

            // Assert
            Assert.That(testLoggingEvents.Count, Is.EqualTo(countLogAttempts));
            testLoggingEvents.ForEach(loggingEvent =>
            {
                Assert.That(!loggingEvent.Properties.Contains("newrelic.linkingmetadata"), "newrelic.linkingmetadata property found. This test does not expect newrelic.linkingmetadata property in log event");
            });
        }

        [Test]
        public void GetLinkingMetadata_IsHandled_Exception()
        {
            // Arrange
            LogManager.ShutdownRepository(Assembly.GetEntryAssembly());

            var testAgent = Mock.Create<IAgent>();
            Mock.Arrange(() => testAgent.GetLinkingMetadata()).DoInstead(() =>
            {
                throw new Exception("Exception - GetLinkingMetadata");
            });

            var testAppender = new NewRelicAppender(() => testAgent);


            //Set the the NewRelicAppender at the root logger
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            BasicConfigurator.Configure(logRepository, testAppender);

            var testLogger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            var layout = Mock.Create<log4net.Layout.SimpleLayout>();
            var childAppender = new log4net.Appender.ConsoleAppender
            {
                Layout = layout
            };

            testAppender.AddAppender(childAppender);

            var testLoggingEvents = new List<LoggingEvent>();

            Mock.Arrange(() => layout.Format(Arg.IsAny<TextWriter>(), Arg.IsAny<LoggingEvent>())).DoInstead((TextWriter textWriter, LoggingEvent loggingEvent) =>
            {
                testLoggingEvents.Add(loggingEvent);
            });

            // Act

            testLogger.Info("This is log message ");

            // Assert
            Assert.That(testLoggingEvents.Count, Is.EqualTo(1));
            Assert.That(!testLoggingEvents[0].Properties.Contains("newrelic.linkingmetadata"), "newrelic.linkingmetadata property found. This test does not expect newrelic.linkingmetadata property in log event");
        }

        [Test]
        public void GetLinkingMetadata_IsHandled_NullResult()
        {
            // Arrange
            LogManager.ShutdownRepository(Assembly.GetEntryAssembly());

            var testAgent = Mock.Create<IAgent>();
            Mock.Arrange(() => testAgent.GetLinkingMetadata()).Returns<Dictionary<string, string>>(null);

            var testAppender = new NewRelicAppender(() => testAgent);

            //Set the the NewRelicAppender at the root logger
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());


            BasicConfigurator.Configure(logRepository, testAppender);

            var testLogger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            var layout = Mock.Create<log4net.Layout.SimpleLayout>();
            var childAppender = new log4net.Appender.ConsoleAppender
            {
                Layout = layout
            };

            testAppender.AddAppender(childAppender);

            var testLoggingEvents = new List<LoggingEvent>();

            Mock.Arrange(() => layout.Format(Arg.IsAny<TextWriter>(), Arg.IsAny<LoggingEvent>())).DoInstead((TextWriter textWriter, LoggingEvent loggingEvent) =>
            {
                testLoggingEvents.Add(loggingEvent);
            });

            // Act

            testLogger.Info("This is log message ");

            // Assert
            Assert.That(testLoggingEvents.Count, Is.EqualTo(1));
            Assert.That(!testLoggingEvents[0].Properties.Contains("newrelic.linkingmetadata"), "newrelic.linkingmetadata property found. This test does not expect newrelic.linkingmetadata property in log event");
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using NewRelic.Api.Agent;
using NUnit.Framework;
using Telerik.JustMock;

namespace NewRelic.LogEnrichers.Log4Net.Tests
{
    public class Log4NetLayoutTests
    {
        private const string UserPropertyKeyPrefix = "Message.Properties.";

        private IAgent _testAgent;
        private NewRelicAppender _testAppender;
        private NewRelicLayout _layout;
        private ConsoleAppender _childAppender;

        [SetUp]
        public void SetUp() 
        {
            _testAgent = Mock.Create<IAgent>();
            _testAppender = new NewRelicAppender(() => _testAgent);
            _layout = Mock.Create<NewRelicLayout>();
            _childAppender = new log4net.Appender.ConsoleAppender
            {
                Layout = _layout
            };
            _testAppender.AddAppender(_childAppender);
        }

        [TestCase("INFO")]
        [TestCase("WARN")]
        [TestCase("FATAL")]
        [TestCase("ERROR")]
        public void Output_Intrinsics_DifferentLoggingLevel(string level)
        {
            // Arrange
            LogManager.ShutdownRepository(Assembly.GetEntryAssembly());

            Mock.Arrange(() => _testAgent.GetLinkingMetadata()).Returns(new Dictionary<string, string>() { { "key1", "value1" } });


            //Set the the NewRelicAppender at the root logger
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            BasicConfigurator.Configure(logRepository, _testAppender);

            TextWriter tw = null;
            LoggingEvent le = null;

            Mock.Arrange(() => _layout.Format(Arg.IsAny<TextWriter>(), Arg.IsAny<LoggingEvent>())).DoInstead((TextWriter textWriter, LoggingEvent loggingEvent) =>
            {
                le = loggingEvent;
                tw = textWriter;
            }).CallOriginal();

            var testLogger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            var testLoggingMessage = "This is a test log message";
            //// Act
            testLogger.Log(level, testLoggingMessage, null);

            var serializedMessage = tw.ToString();
            var deserializedMessage = TestHelpers.DeserializeOutputJSON(serializedMessage);

            // Assert
            Assert.That(deserializedMessage.ContainsKey("log.level"), "log.level not found.");
            var resultLevel = TestHelpers.DeserializeOutputJSON(deserializedMessage["log.level"].ToString());
            Assert.That(resultLevel["Name"].ToString() == level, "Incorrect logging level");

            Assert.That(deserializedMessage.ContainsKey("thread.name"), "thread.name not found.");
            var threadName = deserializedMessage["thread.name"].ToString();
            Assert.That(threadName.ToString() != string.Empty, "thread.name is empty");

            Assert.That(deserializedMessage.ContainsKey("message"), "message not found.");
            var message = deserializedMessage["message"].ToString();
            Assert.That(message, Is.EqualTo(testLoggingMessage));

            Assert.That(deserializedMessage.ContainsKey("timestamp"), "timestamp not found.");
            var timestamp = deserializedMessage["timestamp"].GetInt64();
            Assert.That(le.TimeStamp.ToUnixTimeMilliseconds(), Is.EqualTo(timestamp));
        }

        [Test]
        public void Output_UserProperties()
        {
            // Arrange
            LogManager.ShutdownRepository(Assembly.GetEntryAssembly());

            log4net.ThreadContext.Properties["customerPropertyString"] = "propertyValueString";
            log4net.ThreadContext.Properties["customerPropertyNull"] = null;
            log4net.ThreadContext.Properties["customerPropertyBoolean"] = true;
            log4net.ThreadContext.Properties["customerPropertyInteger"] = 100;

            Mock.Arrange(() => _testAgent.GetLinkingMetadata()).Returns(new Dictionary<string, string>() { { "NewRelicFakeMetaDataKey", "NewRelicFakeMetatDaValue" } });


            //Set the the NewRelicAppender at the root logger
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            BasicConfigurator.Configure(logRepository, _testAppender);

            TextWriter tw = null;

            Mock.Arrange(() => _layout.Format(Arg.IsAny<TextWriter>(), Arg.IsAny<LoggingEvent>())).DoInstead((TextWriter textWriter, LoggingEvent loggingEvent) =>
            {
                tw = textWriter;
            }).CallOriginal();

            var testLogger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            var testLoggingMessage = "This is a test log message";
            //// Act
            testLogger.Info(testLoggingMessage);

            var serializedMessage = tw.ToString();
            var deserializedMessage = TestHelpers.DeserializeOutputJSON(serializedMessage);

            // Assert
            Asserts.KeyAndValueMatch(deserializedMessage, UserPropertyKeyPrefix + "customerPropertyString", "propertyValueString");
            Asserts.KeyAndValueMatch(deserializedMessage, UserPropertyKeyPrefix + "customerPropertyBoolean", true);
            Asserts.KeyAndValueMatch(deserializedMessage, UserPropertyKeyPrefix + "customerPropertyNull", JsonValueKind.Null);
            Asserts.KeyAndValueMatch(deserializedMessage, UserPropertyKeyPrefix + "customerPropertyInteger", 100);
        }

        [Test]
        public void Output_Exception()
        {
            // Arrange
            LogManager.ShutdownRepository(Assembly.GetEntryAssembly());
            Mock.Arrange(() => _testAgent.GetLinkingMetadata()).Returns(new Dictionary<string, string>() { { "NewRelicFakeMetaDataKey", "NewRelicFakeMetatDaValue" } });

            //Set the the NewRelicAppender at the root logger
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            BasicConfigurator.Configure(logRepository, _testAppender);

            TextWriter tw = null;
            Mock.Arrange(() => _layout.Format(Arg.IsAny<TextWriter>(), Arg.IsAny<LoggingEvent>())).DoInstead((TextWriter textWriter, LoggingEvent loggingEvent) =>
            {
                tw = textWriter;
            }).CallOriginal();

            var testLogger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            var testExceptionMessage = "This is an exception.";
            var testException = new InvalidOperationException(testExceptionMessage);

            // Act
            try
            {
                TestHelpers.CreateStackTracedError(0, testException, 3);
            }
            catch (Exception ex)
            {
                testLogger.Error("Something has occurred!!!", ex);
            }

            //// Act
            var serializedMessage = tw.ToString();
            var deserializedMessage = TestHelpers.DeserializeOutputJSON(serializedMessage);

            // Assert
            Asserts.KeyAndValueMatch(deserializedMessage, "error.message", testExceptionMessage);
            Asserts.KeyAndValueMatch(deserializedMessage, "error.class", testException.GetType().FullName);
            Asserts.KeyAndValueMatch(deserializedMessage, "error.stack", testException.StackTrace);
        }

        [Test]
        public void Output_Exception_NoMessage()
        {
            // Arrange
            LogManager.ShutdownRepository(Assembly.GetEntryAssembly());
            Mock.Arrange(() => _testAgent.GetLinkingMetadata()).Returns(new Dictionary<string, string>() { { "NewRelicFakeMetaDataKey", "NewRelicFakeMetatDaValue" } });

            //Set the the NewRelicAppender at the root logger
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            BasicConfigurator.Configure(logRepository, _testAppender);

            TextWriter tw = null;
            Mock.Arrange(() => _layout.Format(Arg.IsAny<TextWriter>(), Arg.IsAny<LoggingEvent>())).DoInstead((TextWriter textWriter, LoggingEvent loggingEvent) =>
            {
                tw = textWriter;
            }).CallOriginal();

            var testLogger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            var testException = new Exception(string.Empty);

            // Act
            try
            {
                TestHelpers.CreateStackTracedError(0, testException, 3);
            }
            catch (Exception ex)
            {
                testLogger.Error("Something has occurred!!!", ex);
            }

            //// Act
            var serializedMessage = tw.ToString();
            var deserializedMessage = TestHelpers.DeserializeOutputJSON(serializedMessage);

            // Assert
            Assert.That(deserializedMessage, Does.Not.ContainKey("error.message"));
            Asserts.KeyAndValueMatch(deserializedMessage, "error.class", testException.GetType().FullName);
            Asserts.KeyAndValueMatch(deserializedMessage, "error.stack", testException.StackTrace);
        }

        [Test]
        public void Output_Exception_NoStackTrace()
        {
            // Arrange
            LogManager.ShutdownRepository(Assembly.GetEntryAssembly());
            Mock.Arrange(() => _testAgent.GetLinkingMetadata()).Returns(new Dictionary<string, string>() { { "NewRelicFakeMetaDataKey", "NewRelicFakeMetatDaValue" } });

            //Set the the NewRelicAppender at the root logger
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            BasicConfigurator.Configure(logRepository, _testAppender);

            TextWriter tw = null;
            Mock.Arrange(() => _layout.Format(Arg.IsAny<TextWriter>(), Arg.IsAny<LoggingEvent>())).DoInstead((TextWriter textWriter, LoggingEvent loggingEvent) =>
            {
                tw = textWriter;
            }).CallOriginal();

            var testLogger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            var testExceptionMessage = "this is an exception";
            var testException = new Exception(testExceptionMessage);

            // Act

            testLogger.Error("Something has occurred!!!", testException);

            //// Act
            var serializedMessage = tw.ToString();
            var deserializedMessage = TestHelpers.DeserializeOutputJSON(serializedMessage);

            // Assert
            Asserts.KeyAndValueMatch(deserializedMessage, "error.message", testExceptionMessage);
            Asserts.KeyAndValueMatch(deserializedMessage, "error.class", testException.GetType().FullName);
            Assert.That(deserializedMessage, Does.Not.ContainKey("error.stack"));
        }
    }

    public static class CustomLoggingExtensions
    {
        public static void Log(this ILog logger, string level, object message, Exception exception)
        {
            switch (level.ToUpper()) 
            {
                case "INFO":
                    logger.Info(message, exception);
                    break;
                case "DEBUG":
                    logger.Debug(message, exception);
                    break;
                case "WARN":
                    logger.Warn(message, exception);
                    break;
                case "FATAL":
                    logger.Fatal(message, exception);
                    break;
                case "ERROR":
                    logger.Error(message, exception);
                    break;
                default:
                    throw new Exception(@$"level:{level} not recognized logging level.");
            }
        }
    }
}


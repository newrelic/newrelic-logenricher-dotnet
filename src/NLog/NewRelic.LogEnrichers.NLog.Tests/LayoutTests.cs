using NUnit.Framework;
using NLog.Targets;
using NLog.Config;
using NLog;
using System.Threading;
using System.Diagnostics;
using NewRelic.Api.Agent;
using Telerik.JustMock;
using System.Collections.Generic;
using System;
using NLog.Common;
using System.IO;
using System.Text.Json;

namespace NewRelic.LogEnrichers.NLog.Tests
{
    public class LayoutTests
    {
        Logger _logger;
        DebugTarget _target;
        IAgent _testAgent;

        private const string TestErrMsg = "This is a test exception";
        private const string LogMessage = "This is a log message";

        private static Dictionary<string,string> linkingMetadataDict = new Dictionary<string, string>
            {
                { "trace.id", "trace-id" },
                { "span.id", "span-id" },
                { "entity.name", "entity-name" },
                { "entity.type", "entity-type" },
                { "entity.guid", "entity-guid" },
                { "hostname", "host-name" }
            };


        [SetUp]
        public void Setup()
        {
            _testAgent = Mock.Create<IAgent>();
            _target = new DebugTarget("testTarget");
            _target.Layout = new NewRelicJsonLayout(() => _testAgent);

            var config = new LoggingConfiguration();
            config.AddTarget(_target);
            config.AddRuleForAllLevels(_target);

            LogManager.Configuration = config;

            _logger = LogManager.GetLogger("testLogger");
        }

        [TearDown]
        public void TearDown()
        {
            _logger = null;
            _target = null;
            _testAgent = null;
        }

        [Test]
        public void GetLinkingMetadata_CalledOncePerLogMessage()
        {
            //Arrange
            var rnd = new Random();
            var countLogAttempts = rnd.Next(2, 25);

            //Act
            for (var i = 0; i < countLogAttempts; i++)
            {
                _logger.Info(LogMessage);
            }

            //Assert
            Mock.Assert(() => _testAgent.GetLinkingMetadata(), Occurs.Exactly(countLogAttempts));
            Assert.That(_target.Counter, Is.EqualTo(countLogAttempts));
        }

        [Test]
        public void GetLinkingMetadata_IsHandled_NullAgent()
        {
            // Arrange
            _target.Layout = new NewRelicJsonLayout(() => null);

            // Act
            _logger.Info(LogMessage);
            var loggedMessage = _target.LastMessage;
            var resultsDictionary = TestHelpers.DeserializeOutputJSON(loggedMessage);

            // Assert
            Assert.That(_target.Counter, Is.EqualTo(1));
            Asserts.KeyAndValueMatch(resultsDictionary, "message", LogMessage);
        }

        [Test]
        public void ExceptionInGetLinkingMetadata_IsHandled()
        {
            // Arrange
            var wasRun = false;
            var exceptionMessage = "Exception - GetLinkingMetadata";
            Mock.Arrange(() => _testAgent.GetLinkingMetadata())
                .DoInstead(() =>
                {
                    wasRun = true;
                    throw new Exception(exceptionMessage);
                });

            var internalLogStringWriter = new StringWriter();
            InternalLogger.LogLevel = LogLevel.Trace;
            InternalLogger.LogWriter = internalLogStringWriter;

            // Act
            _logger.Info(LogMessage);
            var loggedMessage = _target.LastMessage;
            var resultsDictionary = TestHelpers.DeserializeOutputJSON(loggedMessage);

            // Assert
            Assert.That(wasRun, Is.True);
            Assert.That(internalLogStringWriter.ToString(), Does.Contain(exceptionMessage));
            Asserts.KeyAndValueMatch(resultsDictionary, "message", LogMessage);
        }

        [Test]
        public void GetLinkingMetadata_IsHandled_NullResult()
        {
            // Arrange
            var wasRun = false;
            Mock.Arrange(() => _testAgent.GetLinkingMetadata())
                .DoInstead(() => { wasRun = true; })
                .Returns<Dictionary<string, string>>(null);

            var internalLogStringWriter = new StringWriter();
            InternalLogger.LogLevel = LogLevel.Trace;
            InternalLogger.LogWriter = internalLogStringWriter;

            // Act
            _logger.Info(LogMessage);
            var loggedMessage = _target.LastMessage;
            var resultsDictionary = TestHelpers.DeserializeOutputJSON(loggedMessage);

            // Assert
            Assert.That(wasRun, Is.True);
            Assert.That(internalLogStringWriter.ToString(), Is.EqualTo("")); // I.e. no exception was caught and logged in this case
            Asserts.KeyAndValueMatch(resultsDictionary, "message", LogMessage);
            foreach (var key in linkingMetadataDict.Keys)
            {
                Assert.That(resultsDictionary, Does.Not.ContainKey(key));
            }
        }

        [Test]
        public void GetLinkingMetadata_IsHandled_EmptyDictionaryResult()
        {
            // Arrange
            var wasRun = false;
            Mock.Arrange(() => _testAgent.GetLinkingMetadata())
                .DoInstead(() => { wasRun = true; })
                .Returns(new Dictionary<string, string>());

            // Act
            _logger.Info(LogMessage);
            var loggedMessage = _target.LastMessage;
            var resultsDictionary = TestHelpers.DeserializeOutputJSON(loggedMessage);

            // Assert
            Assert.That(wasRun, Is.True);
            Asserts.KeyAndValueMatch(resultsDictionary, "message", LogMessage);
            foreach (var key in linkingMetadataDict.Keys)
            {
                Assert.That(resultsDictionary, Does.Not.ContainKey(key));
            }
        }

        [Test]
        public void LogMessage_NoAgent_VerifyAttributes()
        {
            //Arrange

            //Act
            _logger.Info(LogMessage);
            var loggedMessage = _target.LastMessage;
            var resultsDictionary = TestHelpers.DeserializeOutputJSON(loggedMessage);

            //Assert
            Asserts.KeyAndValueMatch(resultsDictionary, "message", LogMessage);
            Asserts.KeyAndValueMatch(resultsDictionary, "log.level", "INFO");
            Asserts.KeyAndValueMatch(resultsDictionary, "thread.id", Thread.CurrentThread.ManagedThreadId.ToString());
            Asserts.KeyAndValueMatch(resultsDictionary, "process.id", Process.GetCurrentProcess().Id.ToString());
            Assert.IsTrue(resultsDictionary.ContainsKey("timestamp"));
            foreach (var key in linkingMetadataDict.Keys)
            {
                Assert.That(resultsDictionary, Does.Not.ContainKey(key), "The agent was running and instrumented the test process.");
            }
        }

        [Test]
        public void LogMessage_WithAgent_VerifyAttributes()
        {
            //Arrange
            var wasRun = false;
            Mock.Arrange(() => _testAgent.GetLinkingMetadata())
                .DoInstead(() => { wasRun = true; })
                .Returns(linkingMetadataDict);

            //Act
            _logger.Info(LogMessage);
            var loggedMessage = _target.LastMessage;
            var resultsDictionary = TestHelpers.DeserializeOutputJSON(loggedMessage);

            //Assert
            Asserts.KeyAndValueMatch(resultsDictionary, "message", LogMessage);
            Asserts.KeyAndValueMatch(resultsDictionary, "log.level", "INFO");
            Asserts.KeyAndValueMatch(resultsDictionary, "thread.id", Thread.CurrentThread.ManagedThreadId.ToString());
            Asserts.KeyAndValueMatch(resultsDictionary, "process.id", Process.GetCurrentProcess().Id.ToString());
            Assert.IsTrue(resultsDictionary.ContainsKey("timestamp"));
            Assert.IsTrue(wasRun);
            foreach (var key in linkingMetadataDict.Keys)
            {
                Asserts.KeyAndValueMatch(resultsDictionary, key, linkingMetadataDict[key]);
            }

        }
        [Test]
        public void LogMessageWithUserAttribute_WithAgent_VerifyAttributes()
        {
            //Arrange
            var wasRun = false;
            Mock.Arrange(() => _testAgent.GetLinkingMetadata())
                .DoInstead(() => { wasRun = true; })
                .Returns(linkingMetadataDict);

            var userPropertyName = "UserPropertyName";
            var userPropertyValue = "UserPropertyValue";
            var messageTemplate = "Message with custom attribute: {"+userPropertyName+"}";
            var formattedMessage = $"Message with custom attribute: \"{userPropertyValue}\"";

            //Act
            _logger.Info(messageTemplate, userPropertyValue);
            var loggedMessage = _target.LastMessage;
            var resultsDictionary = TestHelpers.DeserializeOutputJSON(loggedMessage);

            //Assert
            Asserts.KeyAndValueMatch(resultsDictionary, "message", formattedMessage);
            Asserts.KeyAndValueMatch(resultsDictionary, "message.template", messageTemplate);
            Asserts.KeyAndValueMatch(resultsDictionary, "log.level", "INFO");
            Asserts.KeyAndValueMatch(resultsDictionary, "thread.id", Thread.CurrentThread.ManagedThreadId.ToString());
            Asserts.KeyAndValueMatch(resultsDictionary, "process.id", Process.GetCurrentProcess().Id.ToString());
            Assert.IsTrue(resultsDictionary.ContainsKey("timestamp"));
            Assert.IsTrue(resultsDictionary.ContainsKey("Message Properties"));
            Asserts.KeyAndValueMatch(resultsDictionary, "Message Properties", JsonValueKind.Object);
            Assert.IsTrue(wasRun);
            foreach (var key in linkingMetadataDict.Keys)
            {
                Asserts.KeyAndValueMatch(resultsDictionary, key, linkingMetadataDict[key]);
            }
        }

        [Test]
        public void LogErrorWithException_WithAgent_VerifyAttributes()
        {
            //Arrange
            var wasRun = false;
            Mock.Arrange(() => _testAgent.GetLinkingMetadata())
                .DoInstead(() => { wasRun = true; })
                .Returns(linkingMetadataDict);

            var testException = new InvalidOperationException(TestErrMsg);

            //Act
            try
            {
                TestHelpers.CreateStackTracedError(0, testException, 3);

            }
            catch (Exception caughtException)
            {
                _logger.Error(caughtException, LogMessage);
            }
            var loggedMessage = _target.LastMessage;
            var resultsDictionary = TestHelpers.DeserializeOutputJSON(loggedMessage);

            //Assert
            Asserts.KeyAndValueMatch(resultsDictionary, "message", LogMessage);
            Asserts.KeyAndValueMatch(resultsDictionary, "log.level", "ERROR");
            Asserts.KeyAndValueMatch(resultsDictionary, "thread.id", Thread.CurrentThread.ManagedThreadId.ToString());
            Asserts.KeyAndValueMatch(resultsDictionary, "process.id", Process.GetCurrentProcess().Id.ToString());
            Assert.IsTrue(resultsDictionary.ContainsKey("timestamp"));
            Assert.IsTrue(wasRun);
            foreach (var key in linkingMetadataDict.Keys)
            {
                Asserts.KeyAndValueMatch(resultsDictionary, key, linkingMetadataDict[key]);
            }
            Asserts.KeyAndValueMatch(resultsDictionary, "error.class", testException.GetType().FullName);
            Asserts.KeyAndValueMatch(resultsDictionary, "error.message", TestErrMsg);
            Asserts.KeyAndValueMatch(resultsDictionary, "error.stack", testException.StackTrace);
        }
    }
}

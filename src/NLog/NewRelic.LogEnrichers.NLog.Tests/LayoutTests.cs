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
        public void LogError_WithAgent_VerifyAttributes()
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

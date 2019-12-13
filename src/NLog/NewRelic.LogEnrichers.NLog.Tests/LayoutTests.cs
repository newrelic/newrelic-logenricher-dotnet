using NUnit.Framework;
using NLog.Targets;
using NLog.Config;
using NLog;
using System.Threading;
using System.Diagnostics;
using NewRelic.Api.Agent;

namespace NewRelic.LogEnrichers.NLog.Tests
{
    public class LayoutTests
    {
        Logger _logger;
        DebugTarget _target;
        IAgent _agent;

        [SetUp]
        public void Setup()
        {
            _target = new DebugTarget("testTarget");
            _target.Layout = new NewRelicJsonLayout(() => _agent);

            var config = new LoggingConfiguration();
            config.AddTarget(_target);
            config.AddRuleForAllLevels(_target);

            LogManager.Configuration = config;

            _logger = LogManager.GetLogger("testLogger");
        }

        [Test]
        public void LogMessage_VerifyAttributes()
        {
            var message = "lorem ipsum";
            _logger.Info(message);

            var loggedMessage = _target.LastMessage;
            var resultsDictionary = TestHelpers.DeserializeOutputJSON(loggedMessage);

            Asserts.KeyAndValueMatch(resultsDictionary, "message", message);
            Asserts.KeyAndValueMatch(resultsDictionary, "log.level", "INFO");
            Asserts.KeyAndValueMatch(resultsDictionary, "thread.id", Thread.CurrentThread.ManagedThreadId.ToString());
            Asserts.KeyAndValueMatch(resultsDictionary, "process.id", Process.GetCurrentProcess().Id.ToString());
            Assert.IsTrue(resultsDictionary.ContainsKey("timestamp"));
        }
    }
}

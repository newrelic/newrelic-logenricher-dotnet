using System;
using System.Collections.Generic;
using System.IO;
using NewRelic.Api.Agent;
using NUnit.Framework;
using Serilog.Debugging;
using Telerik.JustMock;

namespace NewRelic.LogEnrichers.Serilog.Tests
{
    [TestFixture]
    public class EnricherTests
    {
        private const string ExceptionLogMessage = "This is log message that happened with an exception in the API.";
        private const string WarningLogMessage = "This is a warning.";
        private const string LinkingMetadataKey = "newrelic.linkingmetadata";

        private List<string> _testRunDebugLogs;
        private TestSink _outputSink;
        private IAgent _testAgent;

        [SetUp]
        public void Setup()
        {
            _testRunDebugLogs = new List<string>();
            _outputSink = new TestSink();
            _testAgent = Mock.Create<IAgent>();
            SelfLog.Enable(msg => _testRunDebugLogs.Add(msg));
        }

        [TearDown]
        public void TearDown()
        {
            _testRunDebugLogs = null;
            _outputSink = null;
            _testAgent = null;
            SelfLog.Disable();
        }

        [Test]
        public void GetLinkingMetadata_CalledOnceForEachEvent()
        {
            // Arrange
            const string warningCountLogMessageExpected = "This is warning #";
            const string warningCountLogMessage = "This is warning #{Count}";

            var testEnricher = new NewRelicEnricher(() => _testAgent);
            var testLogger = TestHelpers.GetLogger(_outputSink, testEnricher);
            var rnd = new Random();
            var countLogAttempts = rnd.Next(2, 25);
            var expectedMessages = new List<string>();

            // Act
            for(var i = 0; i < countLogAttempts; i++)
            {
                expectedMessages.Add(warningCountLogMessageExpected + i.ToString());
                testLogger.Warning(warningCountLogMessage, i);
            }

            // Assert
            Assert.That(_testRunDebugLogs.Count, Is.EqualTo(0));
            Mock.Assert(() => _testAgent.GetLinkingMetadata(), Occurs.Exactly(countLogAttempts));
            Assert.That(_outputSink.LogEvents.Count, Is.EqualTo(countLogAttempts));
            Assert.That(_outputSink.LogEvents[0].Properties, Does.Not.ContainKey(LinkingMetadataKey));

            //The actual messages are what we expected.
            for (var i = 0; i < _outputSink.LogEvents.Count; i++)
            {
                var expectedMessage = expectedMessages[i];
                var sw = new StringWriter();
                var logEvent = _outputSink.LogEvents[i];
                logEvent.RenderMessage(sw);
                var actualMessage = sw.ToString();

                Assert.That(actualMessage, Is.EqualTo(expectedMessage));
            }
        }

        [Test]
        public void GetLinkingMetadata_IsHandled_NullAgent()
        {
            // Arrange
            var testEnricher = new NewRelicEnricher(() => null);
            var testLogger = TestHelpers.GetLogger(_outputSink, testEnricher);

            // Act
            testLogger.Warning(WarningLogMessage);

            // Assert
            Assert.That(_outputSink.LogEvents.Count, Is.EqualTo(1));
            Assert.That(_outputSink.LogEvents[0].MessageTemplate.Text, Is.EqualTo(WarningLogMessage));
            Assert.That(_outputSink.LogEvents[0].Properties, Does.Not.ContainKey(LinkingMetadataKey));
            Assert.That(_testRunDebugLogs.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetLinkingMetadata_IsHandled_NotAttachedAgent()
        {
            // Arrange
            var testEnricher = new NewRelicEnricher();
            var testLogger = TestHelpers.GetLogger(_outputSink, testEnricher);

            // Act
            testLogger.Warning(WarningLogMessage);

            // Assert
            Assert.That(_outputSink.LogEvents.Count, Is.EqualTo(1));
            Assert.That(_outputSink.LogEvents[0].MessageTemplate.Text, Is.EqualTo(WarningLogMessage));
            Assert.That(_outputSink.LogEvents[0].Properties, Does.Not.ContainKey(LinkingMetadataKey), "The agent was running and instrumented the test process.");
            Assert.That(_testRunDebugLogs.Count, Is.EqualTo(0));
        }

        [Test]
        public void Enricher_IsHandled_Exception()
        {
            // Arrange
            var wasRun = false;
            Mock.Arrange(() => _testAgent.GetLinkingMetadata())
                .DoInstead(() =>
                {
                    wasRun = true;
                    throw new Exception("Exception - GetLinkingMetadata");
                });
            var testEnricher = new NewRelicEnricher(() => _testAgent);
            var testLogger = TestHelpers.GetLogger(_outputSink, testEnricher);

            // Act
            testLogger.Warning(ExceptionLogMessage);

            // Assert
            Assert.That(wasRun, Is.True);
            Assert.That(_outputSink.LogEvents.Count, Is.EqualTo(1));
            Assert.That(_outputSink.LogEvents[0].MessageTemplate.Text, Is.EqualTo(ExceptionLogMessage));
            Assert.That(_outputSink.LogEvents[0].Properties, Does.Not.ContainKey(LinkingMetadataKey));
            Assert.That(_testRunDebugLogs.Count, Is.EqualTo(1));
            Assert.That(_testRunDebugLogs[0], Does.Contain("System.Exception"));
        }

        [Test]
        public void GetLinkingMetadata_IsHandled_NullResult()
        {
            // Arrange
            var wasRun = false;
            Mock.Arrange(() => _testAgent.GetLinkingMetadata())
                .DoInstead(() => { wasRun = true; })
                .Returns<Dictionary<string,string>>(null);
            var testEnricher = new NewRelicEnricher(() => _testAgent);
            var testLogger = TestHelpers.GetLogger(_outputSink, testEnricher);

            // Act
            testLogger.Warning(ExceptionLogMessage);

            // Assert
            Assert.That(wasRun, Is.True);
            Assert.That(_outputSink.LogEvents.Count, Is.EqualTo(1));
            Assert.That(_outputSink.LogEvents[0].MessageTemplate.Text, Is.EqualTo(ExceptionLogMessage));
            Assert.That(_outputSink.LogEvents[0].Properties, Does.Not.ContainKey(LinkingMetadataKey));
            Assert.That(_testRunDebugLogs.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetLinkingMetadata_IsHandled_EmptyDictionaryResult()
        {
            // Arrange
            var wasRun = false;
            Mock.Arrange(() => _testAgent.GetLinkingMetadata())
                .DoInstead(() => { wasRun = true; })
                .Returns(new Dictionary<string, string>());
            var testEnricher = new NewRelicEnricher(() => _testAgent);
            var testLogger = TestHelpers.GetLogger(_outputSink, testEnricher);

            // Act
            testLogger.Warning(WarningLogMessage);

            // Assert
            Assert.That(wasRun, Is.True);
            Assert.That(_outputSink.LogEvents.Count, Is.EqualTo(1));
            Assert.That(_outputSink.LogEvents[0].MessageTemplate.Text, Is.EqualTo(WarningLogMessage));
            Assert.That(_outputSink.LogEvents[0].Properties, Does.Not.ContainKey(LinkingMetadataKey));
            Assert.That(_testRunDebugLogs.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetLinkingMetadata_IsHandled_MetadataPropertyAdded()
        {
            // Arrange
            var wasRun = false;
            var testDict = new Dictionary<string, string>
            {
                { "trace.id", "trace-id" },
                { "span.id", "span-id" },
                { "entity.name", "entity-name" },
                { "entity.type", "entity-type" },
                { "entity.guid", "entity-guid" },
                { "hostname", "host-name" }
            };

            Mock.Arrange(() => _testAgent.GetLinkingMetadata())
                .DoInstead(() => { wasRun = true; })
                .Returns(testDict);
            var testEnricher = new NewRelicEnricher(() => _testAgent);
            var testLogger = TestHelpers.GetLogger(_outputSink, testEnricher);

            // Act
            testLogger.Warning(WarningLogMessage);

            // Assert
            Assert.That(wasRun, Is.True);
            Assert.That(_outputSink.LogEvents.Count, Is.EqualTo(1));
            Assert.That(_outputSink.LogEvents[0].MessageTemplate.Text, Is.EqualTo(WarningLogMessage));
            Assert.That(_outputSink.LogEvents[0].Properties, Does.ContainKey(LinkingMetadataKey));
            Assert.That(_testRunDebugLogs.Count, Is.EqualTo(0));
        }

        // Act covers the chance the serilog could change their implementation to handle templates with periods in the future.
        [Test]
        public void GetLinkingMetadata_IsHandled_DuplicateKeyInMessageTemplate()
        {
            // Arrange
            const string TestValue = "TEST VALUE";
            const string TestTemplate = "TEMPLATE {newrelic.linkingmetadata}"; // matches our metadata property name

            var wasRun = false;
            var testDict = new Dictionary<string, string> { { "message", "duplicate" } }; // must not be empty
            Mock.Arrange(() => _testAgent.GetLinkingMetadata())
                .DoInstead(() => { wasRun = true; })
                .Returns(testDict);
            var testEnricher = new NewRelicEnricher(() => _testAgent);
            var testLogger = TestHelpers.GetLogger(_outputSink, testEnricher);

            // Act
            testLogger.Warning(TestTemplate, TestValue);

            // Assert
            Assert.That(wasRun, Is.True);
            Assert.That(_outputSink.LogEvents.Count, Is.EqualTo(1));
            Assert.That(_outputSink.LogEvents[0].MessageTemplate.Text, Is.EqualTo(TestTemplate));
            Assert.That(_testRunDebugLogs.Count, Is.EqualTo(0));
            Assert.That(_outputSink.LogEvents[0].Properties, Does.ContainKey(LinkingMetadataKey));
            Assert.That(_outputSink.LogEvents[0].Properties[LinkingMetadataKey].ToString(), Does.Not.Contain(TestValue));
        }
    }
}

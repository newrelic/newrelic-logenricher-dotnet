// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using NewRelic.Api.Agent;
using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using NUnit.Framework;
using Telerik.JustMock;

namespace NewRelic.LogEnrichers.NLog.Tests
{
    public class LayoutTests
    {
        private const string TestErrMsg = "This is a test exception";
        private const string LogMessage = "This is a log message";
        private const string UserPropertiesKey = "Message.Properties";
        private const string CustomLoggerName = "CustomLogger";

        private static readonly Dictionary<string, string> LinkingMetadataDict = new Dictionary<string, string>
        {
            { "trace.id", "trace-id" },
            { "span.id", "span-id" },
            { "entity.name", "entity-name" },
            { "entity.type", "entity-type" },
            { "entity.guid", "entity-guid" },
            { "hostname", "host-name" },
        };

        private Logger _logger;
        private DebugTarget _target;
        private IAgent _testAgent;

        [SetUp]
        public void Setup()
        {
            _testAgent = Mock.Create<IAgent>();
            _target = new DebugTarget("testTarget")
            {
                Layout = new NewRelicJsonLayout(() => _testAgent),
            };

            var config = new LoggingConfiguration();
            config.AddTarget(_target);
            config.AddRuleForAllLevels(_target);

            LogManager.Configuration = config;
            _logger = LogManager.GetCurrentClassLogger();
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
            // Arrange
            var rnd = new Random();
            var countLogAttempts = rnd.Next(2, 25);

            // Act
            for (var i = 0; i < countLogAttempts; i++)
            {
                _logger.Info(LogMessage);
            }

            // Assert
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
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.MessageText.GetOutputName(), LogMessage);
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
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.MessageText.GetOutputName(), LogMessage);
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
            Assert.That(internalLogStringWriter.ToString(), Is.EqualTo(string.Empty)); // I.e. no exception was caught and logged in this case
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.MessageText.GetOutputName(), LogMessage);
            foreach (var key in LinkingMetadataDict.Keys)
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
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.MessageText.GetOutputName(), LogMessage);
            foreach (var key in LinkingMetadataDict.Keys)
            {
                Assert.That(resultsDictionary, Does.Not.ContainKey(key));
            }
        }

        [Test]
        public void LogMessage_NoAgent_VerifyAttributes()
        {
            // Arrange

            // Act
            _logger.Info(LogMessage);
            var loggedMessage = _target.LastMessage;
            var resultsDictionary = TestHelpers.DeserializeOutputJSON(loggedMessage);

            // Assert
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.MessageText.GetOutputName(), LogMessage);
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.LogLevel.GetOutputName(), "INFO");
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.ThreadId.GetOutputName(), Thread.CurrentThread.ManagedThreadId.ToString());
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.ProcessId.GetOutputName(), Process.GetCurrentProcess().Id.ToString());
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.LoggerName.GetOutputName(), _logger.Name);
            Assert.IsTrue(resultsDictionary.ContainsKey(NewRelicLoggingProperty.Timestamp.GetOutputName()));
            Assert.That(resultsDictionary, Does.Not.ContainKey(NewRelicLoggingProperty.LineNumber.GetOutputName()));
            foreach (var key in LinkingMetadataDict.Keys)
            {
                Assert.That(resultsDictionary, Does.Not.ContainKey(key), "The agent was running and instrumented the test process.");
            }
        }

        [Test]
        public void LogMessage_CustomLayoutAttributes_VerifyAttributes()
        {
            // Arrange
            // For this one-off test, need to re-do the logging configuration to override what is done in setup
            var target = new DebugTarget("customAttributeTarget");
            var layout = new NewRelicJsonLayout(() => _testAgent);
            layout.Attributes.Add(new JsonAttribute(NewRelicLoggingProperty.LineNumber.GetOutputName(), "${callsite-linenumber}", true));
            target.Layout = layout;

            var config = new LoggingConfiguration();
            config.AddTarget(target);
            config.AddRuleForAllLevels(target);

            LogManager.Configuration = config;

            var logger = LogManager.GetLogger(CustomLoggerName);

            // Act
            logger.Info(LogMessage);
            var loggedMessage = target.LastMessage;
            var resultsDictionary = TestHelpers.DeserializeOutputJSON(loggedMessage);

            // Assert
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.MessageText.GetOutputName(), LogMessage);
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.LogLevel.GetOutputName(), "INFO");
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.ThreadId.GetOutputName(), Thread.CurrentThread.ManagedThreadId.ToString());
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.ProcessId.GetOutputName(), Process.GetCurrentProcess().Id.ToString());
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.LoggerName.GetOutputName(), logger.Name);
            Assert.IsTrue(resultsDictionary.ContainsKey(NewRelicLoggingProperty.Timestamp.GetOutputName()));
            Assert.That(resultsDictionary, Does.ContainKey(NewRelicLoggingProperty.LineNumber.GetOutputName()));
            foreach (var key in LinkingMetadataDict.Keys)
            {
                Assert.That(resultsDictionary, Does.Not.ContainKey(key), "The agent was running and instrumented the test process.");
            }
        }

        [Test]
        public void LogMessage_WithAgent_VerifyAttributes()
        {
            // Arrange
            var wasRun = false;
            Mock.Arrange(() => _testAgent.GetLinkingMetadata())
                .DoInstead(() => { wasRun = true; })
                .Returns(LinkingMetadataDict);

            // Act
            _logger.Info(LogMessage);
            var loggedMessage = _target.LastMessage;
            var resultsDictionary = TestHelpers.DeserializeOutputJSON(loggedMessage);

            // Assert
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.MessageText.GetOutputName(), LogMessage);
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.LogLevel.GetOutputName(), "INFO");
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.ThreadId.GetOutputName(), Thread.CurrentThread.ManagedThreadId.ToString());
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.ProcessId.GetOutputName(), Process.GetCurrentProcess().Id.ToString());
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.LoggerName.GetOutputName(), _logger.Name);
            Assert.IsTrue(resultsDictionary.ContainsKey(NewRelicLoggingProperty.Timestamp.GetOutputName()));
            Assert.IsTrue(wasRun);
            foreach (var key in LinkingMetadataDict.Keys)
            {
                Asserts.KeyAndValueMatch(resultsDictionary, key, LinkingMetadataDict[key]);
            }
        }

        [Test]
        public void LogMessageWithUserAttributes_VerifyUserAttributes()
        {
            // Arrange
            var userPropName1 = "UserPropertyName1";
            var userPropVal1 = "UserPropertyValue1";
            var userPropName2 = "UserPropertyName2";
            var userPropVal2 = "UserPropertyValue2";
            var messageTemplate = "Message with custom attributes: {" + userPropName1 + "}, {" + userPropName2 + "}";
            var formattedMessage = $"Message with custom attributes: \"{userPropVal1}\", \"{userPropVal2}\"";

            // Act
            _logger.Info(messageTemplate, userPropVal1, userPropVal2);
            var loggedMessage = _target.LastMessage;
            var resultsDictionary = TestHelpers.DeserializeOutputJSON(loggedMessage);

            // Assert
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.MessageText.GetOutputName(), formattedMessage);
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.MessageTemplate.GetOutputName(), messageTemplate);
            Assert.IsTrue(resultsDictionary.ContainsKey(UserPropertiesKey));
            Asserts.KeyAndValueMatch(resultsDictionary, UserPropertiesKey, JsonValueKind.Object);
            var userPropertiesDict = TestHelpers.DeserializeOutputJSON(resultsDictionary[UserPropertiesKey].ToString());
            Asserts.KeyAndValueMatch(userPropertiesDict, userPropName1, userPropVal1);
            Asserts.KeyAndValueMatch(userPropertiesDict, userPropName2, userPropVal2);
        }

        [Test]
        public void IsHandled_NullValue_UserProperty()
        {
            // Arrange
            var userPropName1 = "UserPropertyName1";
            string userPropVal1 = null;
            var messageTemplate = "Message with custom attribute: {" + userPropName1 + "}";
            var formattedMessage = $"Message with custom attribute: NULL";

            // Act
            _logger.Info(messageTemplate, userPropVal1);
            var loggedMessage = _target.LastMessage;
            var resultsDictionary = TestHelpers.DeserializeOutputJSON(loggedMessage);

            // Assert
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.MessageText.GetOutputName(), formattedMessage);
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.MessageTemplate.GetOutputName(), messageTemplate);
            Assert.IsTrue(resultsDictionary.ContainsKey(UserPropertiesKey));
            Asserts.KeyAndValueMatch(resultsDictionary, UserPropertiesKey, JsonValueKind.Object);
            var userPropertiesDict = TestHelpers.DeserializeOutputJSON(resultsDictionary[UserPropertiesKey].ToString());
            Asserts.KeyAndValueMatch(userPropertiesDict, userPropName1, JsonValueKind.Null);
        }

        [Test]
        public void IsHandled_JSONValue_UserProperty()
        {
            // Arrange
            var userPropName1 = "UserPropertyName1";
            var userPropVal1 = new { foo = "bar", beep = "boop" };
            var messageTemplate = "Message with custom attribute: {@" + userPropName1 + "}";
            var formattedMessage = "Message with custom attribute: {\"foo\":\"bar\", \"beep\":\"boop\"}";

            // Act
            _logger.Info(messageTemplate, userPropVal1);
            var loggedMessage = _target.LastMessage;
            var resultsDictionary = TestHelpers.DeserializeOutputJSON(loggedMessage);

            // Assert
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.MessageText.GetOutputName(), formattedMessage);
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.MessageTemplate.GetOutputName(), messageTemplate);
            Assert.IsTrue(resultsDictionary.ContainsKey(UserPropertiesKey));
            Asserts.KeyAndValueMatch(resultsDictionary, UserPropertiesKey, JsonValueKind.Object);
            var userPropertiesDict = TestHelpers.DeserializeOutputJSON(resultsDictionary[UserPropertiesKey].ToString());
            Asserts.KeyAndValueMatch(userPropertiesDict, userPropName1, JsonValueKind.Object);
            var userPropValDict = TestHelpers.DeserializeOutputJSON(userPropertiesDict[userPropName1].ToString());
            Asserts.KeyAndValueMatch(userPropValDict, "foo", "bar");
            Asserts.KeyAndValueMatch(userPropValDict, "beep", "boop");
        }

        [Test]
        public void LogMessageWithNestedObjects_DefaultRecursionLimit()
        {
            var alice = new Person { Name = "Alice", Manager = null };
            var bob = new Person { Name = "Bob", Manager = alice };
            var charlie = new Person { Name = "Charlie", Manager = bob };
            var messageTemplate = "Person = {person}";
            var formattedMessage = "Person = Charlie";

            // Act
            _logger.Info(messageTemplate, charlie);
            var loggedMessage = _target.LastMessage;
            var resultsDictionary = TestHelpers.DeserializeOutputJSON(loggedMessage);

            // Assert
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.MessageText.GetOutputName(), formattedMessage);
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.MessageTemplate.GetOutputName(), messageTemplate);
            Assert.IsTrue(resultsDictionary.ContainsKey(UserPropertiesKey));
            Asserts.KeyAndValueMatch(resultsDictionary, UserPropertiesKey, JsonValueKind.Object);
            var userPropertiesDict = TestHelpers.DeserializeOutputJSON(resultsDictionary[UserPropertiesKey].ToString());
            Asserts.KeyAndValueMatch(userPropertiesDict, "person", JsonValueKind.Object);
            var userPropValDict = TestHelpers.DeserializeOutputJSON(userPropertiesDict["person"].ToString());
            Asserts.KeyAndValueMatch(userPropValDict, "Name", "Charlie");
            Asserts.KeyAndValueMatch(userPropValDict, "Manager", "Bob");
        }

        [Test]
        public void LogMessageWithNestedObjects_UserModifiedRecursionLimit()
        {
            // Arrange
            // For this one-off test, need to re-do the logging configuration to override what is done in setup
            var target = new DebugTarget("customAttributeTarget");
            var layout = new NewRelicJsonLayout(() => _testAgent);
            layout.MaxRecursionLimit = 3;
            target.Layout = layout;

            var config = new LoggingConfiguration();
            config.AddTarget(target);
            config.AddRuleForAllLevels(target);

            LogManager.Configuration = config;

            var logger = LogManager.GetLogger(CustomLoggerName);

            var alice = new Person { Name = "Alice", Manager = null };
            var bob = new Person { Name = "Bob", Manager = alice };
            var charlie = new Person { Name = "Charlie", Manager = bob };
            var messageTemplate = "Person = {person}";
            var formattedMessage = "Person = Charlie";

            // Act
            logger.Info(messageTemplate, charlie);
            var loggedMessage = target.LastMessage;
            var resultsDictionary = TestHelpers.DeserializeOutputJSON(loggedMessage);

            // Assert
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.MessageText.GetOutputName(), formattedMessage);
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.MessageTemplate.GetOutputName(), messageTemplate);
            Assert.IsTrue(resultsDictionary.ContainsKey(UserPropertiesKey));
            Asserts.KeyAndValueMatch(resultsDictionary, UserPropertiesKey, JsonValueKind.Object);
            var userPropertiesDict = TestHelpers.DeserializeOutputJSON(resultsDictionary[UserPropertiesKey].ToString());
            Asserts.KeyAndValueMatch(userPropertiesDict, "person", JsonValueKind.Object);
            var userPropValDict = TestHelpers.DeserializeOutputJSON(userPropertiesDict["person"].ToString());
            Asserts.KeyAndValueMatch(userPropValDict, "Name", "Charlie");
            Asserts.KeyAndValueMatch(userPropValDict, "Manager", JsonValueKind.Object);
        }

        [Test]
        public void LogEvent_VerifyTimestamp()
        {
            // Arrange
            var logEvent = new LogEventInfo(LogLevel.Info, "testLogger", LogMessage);

            // Act
            _logger.Log(logEvent);
            var loggedMessage = _target.LastMessage;
            var resultsDictionary = TestHelpers.DeserializeOutputJSON(loggedMessage);

            // Assert
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.MessageText.GetOutputName(), LogMessage);
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.Timestamp.GetOutputName(), logEvent.TimeStamp.ToUnixTimeMilliseconds());
        }

        [Test]
        public void IsHandled_NullValue_InLinkingMetadata()
        {
            // Arrange
            var wasRun = false;
            var linkingMetadataWithNullValue = new Dictionary<string, string>()
            {
                { "trace.id", "12345" },
                { "hostname", null },
            };
            Mock.Arrange(() => _testAgent.GetLinkingMetadata())
                .DoInstead(() => { wasRun = true; })
                .Returns(linkingMetadataWithNullValue);

            // Act
            _logger.Info(LogMessage);
            var loggedMessage = _target.LastMessage;
            var resultsDictionary = TestHelpers.DeserializeOutputJSON(loggedMessage);

            // Assert
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.MessageText.GetOutputName(), LogMessage);
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.LogLevel.GetOutputName(), "INFO");
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.ThreadId.GetOutputName(), Thread.CurrentThread.ManagedThreadId.ToString());
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.ProcessId.GetOutputName(), Process.GetCurrentProcess().Id.ToString());
            Assert.IsTrue(resultsDictionary.ContainsKey(NewRelicLoggingProperty.Timestamp.GetOutputName()));
            Assert.IsTrue(wasRun);
            Asserts.KeyAndValueMatch(resultsDictionary, "trace.id", "12345");
            Asserts.KeyAndValueMatch(resultsDictionary, "hostname", JsonValueKind.Null);
        }

        [Test]
        public void LogErrorWithException_WithAgent_VerifyAttributes()
        {
            // Arrange
            var wasRun = false;
            Mock.Arrange(() => _testAgent.GetLinkingMetadata())
                .DoInstead(() => { wasRun = true; })
                .Returns(LinkingMetadataDict);

            var testException = new InvalidOperationException(TestErrMsg);

            // Act
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

            // Assert
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.MessageText.GetOutputName(), LogMessage);
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.LogLevel.GetOutputName(), "ERROR");
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.ThreadId.GetOutputName(), Thread.CurrentThread.ManagedThreadId.ToString());
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.ProcessId.GetOutputName(), Process.GetCurrentProcess().Id.ToString());
            Assert.IsTrue(resultsDictionary.ContainsKey(NewRelicLoggingProperty.Timestamp.GetOutputName()));
            Assert.IsTrue(wasRun);
            foreach (var key in LinkingMetadataDict.Keys)
            {
                Asserts.KeyAndValueMatch(resultsDictionary, key, LinkingMetadataDict[key]);
            }

            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.ErrorClass.GetOutputName(), testException.GetType().FullName);
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.ErrorMessage.GetOutputName(), TestErrMsg);
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.ErrorStack.GetOutputName(), testException.StackTrace);
        }

        [Test]
        public void LogErrorWithException_NoExceptionMessage()
        {
            // Arrange
            var wasRun = false;
            Mock.Arrange(() => _testAgent.GetLinkingMetadata())
                .DoInstead(() => { wasRun = true; })
                .Returns(LinkingMetadataDict);

            var testException = new InvalidOperationException(string.Empty);

            // Act
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

            // Assert
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.MessageText.GetOutputName(), LogMessage);
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.LogLevel.GetOutputName(), "ERROR");
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.ThreadId.GetOutputName(), Thread.CurrentThread.ManagedThreadId.ToString());
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.ProcessId.GetOutputName(), Process.GetCurrentProcess().Id.ToString());
            Assert.IsTrue(resultsDictionary.ContainsKey(NewRelicLoggingProperty.Timestamp.GetOutputName()));
            Assert.IsTrue(wasRun);
            foreach (var key in LinkingMetadataDict.Keys)
            {
                Asserts.KeyAndValueMatch(resultsDictionary, key, LinkingMetadataDict[key]);
            }

            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.ErrorClass.GetOutputName(), testException.GetType().FullName);
            Assert.That(resultsDictionary, Does.Not.ContainKey(NewRelicLoggingProperty.ErrorMessage.GetOutputName()));
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.ErrorStack.GetOutputName(), testException.StackTrace);
        }

        [Test]
        public void LogErrorWithException_NoStackTrace()
        {
            // Arrange
            var wasRun = false;
            Mock.Arrange(() => _testAgent.GetLinkingMetadata())
                .DoInstead(() => { wasRun = true; })
                .Returns(LinkingMetadataDict);

            var testException = new InvalidOperationException(TestErrMsg);

            // Act
            _logger.Error(testException, LogMessage);
            var loggedMessage = _target.LastMessage;
            var resultsDictionary = TestHelpers.DeserializeOutputJSON(loggedMessage);

            // Assert
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.MessageText.GetOutputName(), LogMessage);
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.LogLevel.GetOutputName(), "ERROR");
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.ThreadId.GetOutputName(), Thread.CurrentThread.ManagedThreadId.ToString());
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.ProcessId.GetOutputName(), Process.GetCurrentProcess().Id.ToString());
            Assert.IsTrue(resultsDictionary.ContainsKey(NewRelicLoggingProperty.Timestamp.GetOutputName()));
            Assert.IsTrue(wasRun);
            foreach (var key in LinkingMetadataDict.Keys)
            {
                Asserts.KeyAndValueMatch(resultsDictionary, key, LinkingMetadataDict[key]);
            }

            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.ErrorClass.GetOutputName(), testException.GetType().FullName);
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.ErrorMessage.GetOutputName(), TestErrMsg);
            Assert.That(resultsDictionary, Does.Not.ContainKey(NewRelicLoggingProperty.ErrorStack.GetOutputName()));
        }

        [TestCase(true, true, "name with space", "value with space", "name_with_underscore", "value_with_underscore")]
        [TestCase(true, true, "name-with-hyphen", "value-with-hyphen", "keyname", "valuename")]
        [TestCase(true, true, "testing integer mdc", 5, "testing integer mdlc", 10)]
        [TestCase(true, true, "testing double mdc", 12.3D, "testing double mdlc", 3.12D)]
        [TestCase(true, true, "testing bool mdc", true, "testing bool mdlc", false)]
        [TestCase(true, true, "testing empty mdc", "", "testing empty mdlc", "")]
        [TestCase(true, true, "testing null mdc", null, "testing null mdlc", null)]
        [TestCase(true, false, "onlymdc", "mdconly", "nomdlc", "nomdlc")]
        [TestCase(false, true, "nomdc", "nomdc", "onlymdlc", "mdlconly")]
        public void LogMessage_WithAgent_McdAndMdlc(bool mdcEnabled, bool mdlcEnabled, string mdcKey, object mdcValue, string mdlcKey, object mdlcValue)
        {
            // Arrange
            var target = new DebugTarget("mdcMdlcTarget");
            var layout = new NewRelicJsonLayout(() => _testAgent);
            layout.IncludeMdc = mdcEnabled;
            layout.IncludeMdlc = mdlcEnabled;
            target.Layout = layout;

            var config = new LoggingConfiguration();
            config.AddTarget(target);
            config.AddRuleForAllLevels(target);

            LogManager.Configuration = config;

            var logger = LogManager.GetLogger(CustomLoggerName);

            var wasRun = false;
            Mock.Arrange(() => _testAgent.GetLinkingMetadata())
                .DoInstead(() => { wasRun = true; })
                .Returns(LinkingMetadataDict);

            // Act
            MappedDiagnosticsContext.Set(mdcKey, mdcValue);
            MappedDiagnosticsLogicalContext.Set(mdlcKey, mdlcValue);

            logger.Info(LogMessage);
            var loggedMessage = target.LastMessage;
            var resultsDictionary = TestHelpers.DeserializeOutputJSON(loggedMessage);

            // Assert
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.MessageText.GetOutputName(), LogMessage);
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.LogLevel.GetOutputName(), "INFO");
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.ThreadId.GetOutputName(), Thread.CurrentThread.ManagedThreadId.ToString());
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.ProcessId.GetOutputName(), Process.GetCurrentProcess().Id.ToString());
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.LoggerName.GetOutputName(), logger.Name);
            Assert.IsTrue(resultsDictionary.ContainsKey(NewRelicLoggingProperty.Timestamp.GetOutputName()));

            if (mdcEnabled)
            {
                Asserts.KeyAndValueMatch(resultsDictionary, mdcKey, mdcValue);
            }

            if (mdlcEnabled)
            {
                Asserts.KeyAndValueMatch(resultsDictionary, mdlcKey, mdlcValue);
            }

            Assert.AreEqual(resultsDictionary.ContainsKey(mdcKey), mdcEnabled);
            Assert.AreEqual(resultsDictionary.ContainsKey(mdlcKey), mdlcEnabled);
            Assert.IsTrue(wasRun);
            foreach (var key in LinkingMetadataDict.Keys)
            {
                Asserts.KeyAndValueMatch(resultsDictionary, key, LinkingMetadataDict[key]);
            }
        }

        [Test]
        public void LogMessage_WithAgent_McdAndMdlc_NoKeys_NoJson()
        {
            // Arrange
            var target = new DebugTarget("mdcMdlcTarget");
            var layout = new NewRelicJsonLayout(() => _testAgent);
            layout.IncludeMdc = true;
            layout.IncludeMdlc = true;
            target.Layout = layout;

            var config = new LoggingConfiguration();
            config.AddTarget(target);
            config.AddRuleForAllLevels(target);

            LogManager.Configuration = config;

            var logger = LogManager.GetLogger(CustomLoggerName);

            var wasRun = false;
            Mock.Arrange(() => _testAgent.GetLinkingMetadata())
                .DoInstead(() => { wasRun = true; })
                .Returns(LinkingMetadataDict);

            // Act
            const string testValueA = "testValueA";
            const string testValueB = "testValueB";
            MappedDiagnosticsContext.Set(string.Empty, testValueA);
            MappedDiagnosticsLogicalContext.Set(string.Empty, testValueB);

            logger.Info(LogMessage);
            var loggedMessage = target.LastMessage;
            var resultsDictionary = TestHelpers.DeserializeOutputJSON(loggedMessage);

            var resultsDictionaryStrings = new Dictionary<string, string>();
            foreach (var result in resultsDictionary)
            {
                resultsDictionaryStrings.Add(result.Key, result.Value.ToString());
                Console.WriteLine($"'{result.Key}':'{result.Value.ToString()}'");
            }

            // Assert
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.MessageText.GetOutputName(), LogMessage);
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.LogLevel.GetOutputName(), "INFO");
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.ThreadId.GetOutputName(), Thread.CurrentThread.ManagedThreadId.ToString());
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.ProcessId.GetOutputName(), Process.GetCurrentProcess().Id.ToString());
            Asserts.KeyAndValueMatch(resultsDictionary, NewRelicLoggingProperty.LoggerName.GetOutputName(), logger.Name);
            Assert.IsTrue(resultsDictionary.ContainsKey(NewRelicLoggingProperty.Timestamp.GetOutputName()));
            Assert.IsFalse(resultsDictionaryStrings.ContainsValue(testValueA));
            Assert.IsFalse(resultsDictionaryStrings.ContainsValue(testValueB));
            Assert.IsTrue(wasRun);
            foreach (var key in LinkingMetadataDict.Keys)
            {
                Asserts.KeyAndValueMatch(resultsDictionary, key, LinkingMetadataDict[key]);
            }
        }
    }
}

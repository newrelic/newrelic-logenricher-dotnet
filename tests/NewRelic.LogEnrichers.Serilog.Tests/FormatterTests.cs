// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using Serilog.Debugging;
using Serilog.Events;

namespace NewRelic.LogEnrichers.Serilog.Tests
{
    public class FormatterTests
    {
        private const string LinkingMetadataKey = "newrelic.linkingmetadata";
        private const string UserPropertyKeyPrefix = "Message.Properties.";
        private const string SingleAtSignTestKey = "@SingleAtSignTestKey";
        private const string DoubleAtSignTestKey = "@@DoubleAtSignTestKey";
        private const string IntegerTestKey = "IntegerTestKey";
        private const string StringATestKey = "StringATestKey";
        private const string StringBTestKey = "StringBTestKey";
        private const string BooleanTestKey = "BooleanTestKey";
        private const string NullTestKey = "NullTestKey";
        private const string DictionaryTestKey = "DictionaryTestKey";
        private const int IntegerTestValue = 12;
        private const bool BooleanTestValue = true;
        private const object NullTestValue = null;
        private const string StringTestValue = "test-string";
        private const string TestErrMsg = "This is a test exception";
        private const string LogMessage = "This is a log message";
        private const int CountIntrinsicProperties = 4;

        private readonly NewRelicLoggingProperty[] _newRelicPropertiesNotReserved;
        private readonly NewRelicLoggingProperty[] _newRelicPropertiesReserved = new[]
        {
            NewRelicLoggingProperty.Timestamp,
            NewRelicLoggingProperty.ErrorMessage,
            NewRelicLoggingProperty.ErrorClass,
            NewRelicLoggingProperty.ErrorStack,
            NewRelicLoggingProperty.MessageText,
            NewRelicLoggingProperty.MessageTemplate,
            NewRelicLoggingProperty.LogLevel
        };

        public FormatterTests()
        {
            _newRelicPropertiesNotReserved = LoggingExtensions.AllNewRelicLoggingProperties.Except(_newRelicPropertiesReserved).ToArray();
        }

        private readonly Random _random = new Random();

        private List<string> _testRunDebugLogs;

        [SetUp]
        public void Setup()
        {
            _testRunDebugLogs = new List<string>();
            SelfLog.Enable(msg => _testRunDebugLogs.Add(msg));
        }

        [TearDown]
        public void TearDown()
        {
            _testRunDebugLogs = null;
            SelfLog.Disable();
        }

        /// <summary>
        /// LogEvent Property has null value
        /// </summary>
        [Test]
        public void IsHandled_NullValue_UserProperty()
        {
            // Arrange
            var testEnricher = new TestEnricher()
                .WithUserPropValue(IntegerTestKey, IntegerTestValue)
                .WithUserPropValue(NullTestKey, NullTestValue)
                .WithNewRelicMetadataValue(new Dictionary<string, string>());
            var testOutputSink = new TestSinkWithFormatter(new NewRelicFormatter());
            var testLogger = SerilogTestHelpers.GetLogger(testOutputSink, testEnricher);

            // Act
            testLogger.Warning(LogMessage);

            // Assert
            AssertNoSerilogErrorsAndCountOutputs(_testRunDebugLogs, testOutputSink.InputsAndOutputs, LogMessage);

            var resultDic = SerilogTestHelpers.DeserializeOutputJSON(testOutputSink.InputsAndOutputs[0]);

            AssertThatPropertyCountsMatch(testEnricher, CountIntrinsicProperties, resultDic);
            Asserts.KeyAndValueMatch(resultDic, UserPropertyKeyPrefix + IntegerTestKey, IntegerTestValue);
            Asserts.KeyAndValueMatch(resultDic, UserPropertyKeyPrefix + NullTestKey, JsonValueKind.Null);
            Assert.That(resultDic, Does.Not.ContainKey(UserPropertyKeyPrefix + LinkingMetadataKey));
        }

        /// <summary>
        /// Entry in NR dictionary has null value
        /// </summary>
        [Test]
        public void IsHandled_NullValue_InNewRelicDictionary()
        {
            // Arrange
            var testNRProperties = new Dictionary<string, string>()
            {
                { StringATestKey, StringTestValue },
                { NullTestKey, null }
            };

            var testEnricher = new TestEnricher()
                .WithUserPropValue(IntegerTestKey, IntegerTestValue)
                .WithNewRelicMetadataValue(testNRProperties);
            var testOutputSink = new TestSinkWithFormatter(new NewRelicFormatter());
            var testLogger = SerilogTestHelpers.GetLogger(testOutputSink, testEnricher);

            // Act
            testLogger.Warning(LogMessage);

            // Assert
            AssertNoSerilogErrorsAndCountOutputs(_testRunDebugLogs, testOutputSink.InputsAndOutputs, LogMessage);

            var resultDic = SerilogTestHelpers.DeserializeOutputJSON(testOutputSink.InputsAndOutputs[0]);

            AssertThatPropertyCountsMatch(testEnricher, CountIntrinsicProperties, resultDic);
            Asserts.KeyAndValueMatch(resultDic, UserPropertyKeyPrefix + IntegerTestKey, IntegerTestValue);
            Assert.That(resultDic, Does.Not.ContainKey(UserPropertyKeyPrefix + LinkingMetadataKey));
            Asserts.KeyAndValueMatch(resultDic, StringATestKey, StringTestValue);
            Asserts.KeyAndValueMatch(resultDic, NullTestKey, JsonValueKind.Null);
        }

        [Test]
        public void IsHandled_Exception()
        {
            // Arrange 
            // Setup formatter to fail part-way through emitting the JSON string.
            // Want to verify that a partial JSON string is not written out.
            var testEnricher = new TestEnricher()
                .WithUserPropValue("StartTestKey", "This is the start")
                .WithUserPropValue("ErrorTestKey", TestErrMsg)
                .WithUserPropValue("EndTestKey", "This is the end");

            var testFormatter = new TestFormatterThatThrowException();
            var testOutputSink = new TestSinkWithFormatter(testFormatter);
            var testLogger = SerilogTestHelpers.GetLogger(testOutputSink, testEnricher);

            // Act
            testLogger.Warning(LogMessage);

            // Assert
            Assert.That(_testRunDebugLogs.Count, Is.EqualTo(1));
            Assert.That(testOutputSink.InputsAndOutputs.Count, Is.EqualTo(1));
            Assert.That(testOutputSink.InputsAndOutputs[0].LogEvent.MessageTemplate.Text, Is.EqualTo(LogMessage));
            Assert.That(testOutputSink.InputsAndOutputs[0].FormattedOutput, Is.Null);
        }

        /// <summary>
        /// We need to escape a single @ in a key name by adding another @. Serilog considers single @ to be a special.
        /// </summary>
        [Test]
        public void Output_Escaping_AtSign()
        {
            // Arrange
            var testEnricher = new TestEnricher()
                .WithUserPropValue(IntegerTestKey, IntegerTestValue)
                .WithUserPropValue(SingleAtSignTestKey, BooleanTestValue)
                .WithUserPropValue(DoubleAtSignTestKey, NullTestValue)
                .WithNewRelicMetadataValue(new Dictionary<string, string>());
            var testOutputSink = new TestSinkWithFormatter(new NewRelicFormatter());
            var testLogger = SerilogTestHelpers.GetLogger(testOutputSink, testEnricher);

            // Act
            testLogger.Warning(LogMessage);

            // Assert
            AssertNoSerilogErrorsAndCountOutputs(_testRunDebugLogs, testOutputSink.InputsAndOutputs, LogMessage);

            var resultDic = SerilogTestHelpers.DeserializeOutputJSON(testOutputSink.InputsAndOutputs[0]);

            AssertThatPropertyCountsMatch(testEnricher, CountIntrinsicProperties, resultDic);
            Asserts.KeyAndValueMatch(resultDic, UserPropertyKeyPrefix + IntegerTestKey, IntegerTestValue);
            Asserts.KeyAndValueMatch(resultDic, UserPropertyKeyPrefix + "@" + SingleAtSignTestKey, BooleanTestValue);
            Asserts.KeyAndValueMatch(resultDic, UserPropertyKeyPrefix + DoubleAtSignTestKey, JsonValueKind.Null);
            Assert.That(resultDic, Does.Not.ContainKey(UserPropertyKeyPrefix + LinkingMetadataKey));
        }

        [Test]
        public void Output_UserProperties()
        {
            // Arrange
            var testEnricher = new TestEnricher()
                .WithUserPropValue(IntegerTestKey, IntegerTestValue)
                .WithUserPropValue(BooleanTestKey, BooleanTestValue)
                .WithUserPropValue(NullTestKey, NullTestValue)
                .WithUserPropValue(StringATestKey, StringTestValue)
                .WithUserPropValue(DictionaryTestKey, new Dictionary<string, object>() { { "DKeyA", "DValueA" }, { "DKeyB", 42 } })
                .WithNewRelicMetadataValue(new Dictionary<string, string>());
            var testOutputSink = new TestSinkWithFormatter(new NewRelicFormatter());
            var testLogger = SerilogTestHelpers.GetLogger(testOutputSink, testEnricher);

            // Act
            testLogger.Warning(LogMessage);

            // Assert
            AssertNoSerilogErrorsAndCountOutputs(_testRunDebugLogs, testOutputSink.InputsAndOutputs, LogMessage);

            var resultDic = SerilogTestHelpers.DeserializeOutputJSON(testOutputSink.InputsAndOutputs[0]);

            AssertThatPropertyCountsMatch(testEnricher, CountIntrinsicProperties, resultDic);
            Asserts.KeyAndValueMatch(resultDic, UserPropertyKeyPrefix + IntegerTestKey, IntegerTestValue);
            Asserts.KeyAndValueMatch(resultDic, UserPropertyKeyPrefix + BooleanTestKey, BooleanTestValue);
            Asserts.KeyAndValueMatch(resultDic, UserPropertyKeyPrefix + NullTestKey, JsonValueKind.Null);
            Asserts.KeyAndValueMatch(resultDic, UserPropertyKeyPrefix + StringATestKey, StringTestValue);
            Assert.That(resultDic, Contains.Key(UserPropertyKeyPrefix + DictionaryTestKey));
            Assert.That(resultDic[UserPropertyKeyPrefix + DictionaryTestKey].ValueKind, Is.EqualTo(JsonValueKind.Object));
            var innerDic = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(resultDic[UserPropertyKeyPrefix + DictionaryTestKey].ToString());
            Assert.That(innerDic, Contains.Key("DKeyA"));
            Assert.That(innerDic["DKeyA"].GetString(), Is.EqualTo("DValueA"));
            Assert.That(innerDic, Contains.Key("DKeyB"));
            Assert.That(innerDic["DKeyB"].GetUInt32(), Is.EqualTo(42));
            Assert.That(resultDic, Does.Not.ContainKey(UserPropertyKeyPrefix + LinkingMetadataKey));
        }

        [Test]
        public void Output_Intrinsics_MessageAndTemplate()
        {
            // Arrange
            var testEnricher = new TestEnricher();
            var testOutputSink = new TestSinkWithFormatter(new NewRelicFormatter());
            var testLogger = SerilogTestHelpers.GetLogger(testOutputSink, testEnricher);

            // Act
            const string template = "We have {value1}, {value2}, and {value3}.";
            testLogger.Warning(template, 1, "two", false);

            // Assert
            AssertNoSerilogErrorsAndCountOutputs(_testRunDebugLogs, testOutputSink.InputsAndOutputs, template);

            var resultDic = SerilogTestHelpers.DeserializeOutputJSON(testOutputSink.InputsAndOutputs[0]);

            AssertThatPropertyCountsMatch(testEnricher, CountIntrinsicProperties + 3, resultDic);
            Asserts.KeyAndValueMatch(resultDic, "message", "We have 1, \"two\", and False.");
        }

        [Test]
        public void Output_Intrinsics_Timestamp_AsUnixTimestamp()
        {
            // Arrange
            var testEnricher = new TestEnricher();
            var testOutputSink = new TestSinkWithFormatter(new NewRelicFormatter());
            var testLogger = SerilogTestHelpers.GetLogger(testOutputSink, testEnricher);

            // Act
            testLogger.Warning(LogMessage);

            // Assert
            AssertNoSerilogErrorsAndCountOutputs(_testRunDebugLogs, testOutputSink.InputsAndOutputs, LogMessage);

            var resultDic = SerilogTestHelpers.DeserializeOutputJSON(testOutputSink.InputsAndOutputs[0]);

            AssertThatPropertyCountsMatch(testEnricher, CountIntrinsicProperties, resultDic);
            Assert.That(resultDic, Contains.Key("timestamp"));
            Assert.That(testOutputSink.InputsAndOutputs[0].LogEvent.Timestamp.ToUnixTimeMilliseconds(),
                Is.EqualTo(resultDic["timestamp"].GetInt64()));
        }

        [TestCase(LogEventLevel.Information)]
        [TestCase(LogEventLevel.Warning)]
        [TestCase(LogEventLevel.Debug)]
        [TestCase(LogEventLevel.Fatal)]
        [TestCase(LogEventLevel.Error)]
        [TestCase(LogEventLevel.Verbose)]
        public void Output_Intrinsics_LogLevel(LogEventLevel level)
        {
            // Arrange
            var testEnricher = new TestEnricher();
            var testOutputSink = new TestSinkWithFormatter(new NewRelicFormatter());
            var testLogger = SerilogTestHelpers.GetLogger(testOutputSink, testEnricher);

            // Act
            testLogger.Write(level, LogMessage);

            // Assert
            AssertNoSerilogErrorsAndCountOutputs(_testRunDebugLogs, testOutputSink.InputsAndOutputs, LogMessage);

            var resultDic = SerilogTestHelpers.DeserializeOutputJSON(testOutputSink.InputsAndOutputs[0]);

            AssertThatPropertyCountsMatch(testEnricher, CountIntrinsicProperties, resultDic);
            Assert.That(resultDic, Contains.Key("log.level"));
            Assert.That(testOutputSink.InputsAndOutputs[0].LogEvent.Level.ToString(),Is.EqualTo(resultDic["log.level"].GetString()));
        }

        [Test]
        public void Output_Intrinsics_ThreadId()
        {
            const string ThreadIDKey = "ThreadID";
            var testThreadIDValue = _random.Next(0, int.MaxValue);
            
            // Arrange
            var testEnricher = new TestEnricher();
            var threadIdEnricher = new TestEnricher()
                .WithUserPropValue(ThreadIDKey, testThreadIDValue);
            var formatter = new NewRelicFormatter()
                .WithPropertyMapping(ThreadIDKey, NewRelicLoggingProperty.ThreadId);
            var testOutputSink = new TestSinkWithFormatter(formatter);
            var testLogger = SerilogTestHelpers.GetLogger(testOutputSink, testEnricher, threadIdEnricher);

            // Act
            testLogger.Warning(LogMessage);

            // Assert
            AssertNoSerilogErrorsAndCountOutputs(_testRunDebugLogs, testOutputSink.InputsAndOutputs, LogMessage);

            var resultDic = SerilogTestHelpers.DeserializeOutputJSON(testOutputSink.InputsAndOutputs[0]);

            //additional 1 for threadid
            AssertThatPropertyCountsMatch(testEnricher, CountIntrinsicProperties + 1, resultDic);
            Assert.That(resultDic, Contains.Key("thread.id"));
            Assert.That(testOutputSink.InputsAndOutputs[0].LogEvent.Properties[ThreadIDKey].ToString(),Is.EqualTo(resultDic["thread.id"].GetString()));
        }

        [Test]
        public void Output_ExceptionProperties()
        {
            // Arrange
            var testEnricher = new TestEnricher();
            var testOutputSink = new TestSinkWithFormatter(new NewRelicFormatter());
            var testLogger = SerilogTestHelpers.GetLogger(testOutputSink, testEnricher);
            var testException = new InvalidOperationException(TestErrMsg);

            // Act
            try
            {
                TestHelpers.CreateStackTracedError(0, testException, 3);
            }
            catch (Exception ex)
            {
                testLogger.Error(ex, LogMessage);
            }

            // Assert
            AssertNoSerilogErrorsAndCountOutputs(_testRunDebugLogs, testOutputSink.InputsAndOutputs, LogMessage);

            var resultDic = SerilogTestHelpers.DeserializeOutputJSON(testOutputSink.InputsAndOutputs[0]);

            AssertThatPropertyCountsMatch(testEnricher, CountIntrinsicProperties + 3, resultDic);
            Asserts.KeyAndValueMatch(resultDic, "error.message", TestErrMsg);
            Asserts.KeyAndValueMatch(resultDic, "error.class", testException.GetType().FullName);
            Asserts.KeyAndValueMatch(resultDic, "error.stack", testException.StackTrace);
        }

        [Test]
        public void Output_ExceptionProperties_NoMessage()
        {
            // Arrange
            var testEnricher = new TestEnricher();
            var testOutputSink = new TestSinkWithFormatter(new NewRelicFormatter());
            var testLogger = SerilogTestHelpers.GetLogger(testOutputSink, testEnricher);
            var testException = new Exception(string.Empty);

            // Act
            try
            {
                TestHelpers.CreateStackTracedError(0, testException, 3);
            }
            catch (Exception ex)
            {
                testLogger.Error(ex, LogMessage);
            }

            // Assert
            AssertNoSerilogErrorsAndCountOutputs(_testRunDebugLogs, testOutputSink.InputsAndOutputs, LogMessage);

            var resultDic = SerilogTestHelpers.DeserializeOutputJSON(testOutputSink.InputsAndOutputs[0]);

            AssertThatPropertyCountsMatch(testEnricher, CountIntrinsicProperties + 2, resultDic);
            Assert.That(resultDic, Does.Not.ContainKey("error.message"));
            Asserts.KeyAndValueMatch(resultDic, "error.class", testException.GetType().FullName);
            Asserts.KeyAndValueMatch(resultDic, "error.stack", testException.StackTrace);
        }

        [Test]
        public void Output_ExceptionProperties_NoStackTrace()
        {
            // Arrange
            var testEnricher = new TestEnricher();
            var testOutputSink = new TestSinkWithFormatter(new NewRelicFormatter());
            var testLogger = SerilogTestHelpers.GetLogger(testOutputSink, testEnricher);
            var testException = new Exception(TestErrMsg);
            
            // Act
            testLogger.Error(testException, LogMessage);

            // Assert
            AssertNoSerilogErrorsAndCountOutputs(_testRunDebugLogs, testOutputSink.InputsAndOutputs, LogMessage);

            var resultDic = SerilogTestHelpers.DeserializeOutputJSON(testOutputSink.InputsAndOutputs[0]);

            AssertThatPropertyCountsMatch(testEnricher, CountIntrinsicProperties + 2, resultDic);
            Assert.That(resultDic, Does.Not.ContainKey("error.stack"));
            Asserts.KeyAndValueMatch(resultDic, "error.message", TestErrMsg);
            Asserts.KeyAndValueMatch(resultDic, "error.class", testException.GetType().FullName);
        }

        [Test]
        public void Output_LinkingMetadataProperties()
        {
            // Arrange
            var testNRProperties = new Dictionary<string, string>()
            {
                { StringATestKey, "TestValue1" },
                { StringBTestKey, "TestValue2" }
            };
            
            var testEnricher = new TestEnricher()
                .WithUserPropValue(IntegerTestKey, IntegerTestValue)
                .WithNewRelicMetadataValue(testNRProperties);
            var testOutputSink = new TestSinkWithFormatter(new NewRelicFormatter());
            var testLogger = SerilogTestHelpers.GetLogger(testOutputSink, testEnricher);

            // Act
            testLogger.Warning(LogMessage);

            // Assert
            AssertNoSerilogErrorsAndCountOutputs(_testRunDebugLogs, testOutputSink.InputsAndOutputs, LogMessage);

            var resultDic = SerilogTestHelpers.DeserializeOutputJSON(testOutputSink.InputsAndOutputs[0]);

            AssertThatPropertyCountsMatch(testEnricher, CountIntrinsicProperties, resultDic);
            Asserts.KeyAndValueMatch(resultDic, UserPropertyKeyPrefix + IntegerTestKey, IntegerTestValue);
            Assert.That(resultDic, Does.Not.ContainKey(UserPropertyKeyPrefix + LinkingMetadataKey));
            Asserts.KeyAndValueMatch(resultDic, StringATestKey, "TestValue1");
            Asserts.KeyAndValueMatch(resultDic, StringBTestKey, "TestValue2");
        }

        [Test]
        public void Mapping_ReservedProperty_ThrowsException()
        {
            // Arrange
            var testFormatter = new NewRelicFormatter();

            // Act and Assert
            foreach (var prop in _newRelicPropertiesReserved)
            {
                Assert.That(()=>testFormatter.WithPropertyMapping(Guid.NewGuid().ToString(), prop),Throws.Exception.TypeOf<InvalidOperationException>());
            }
        }

        [Test]
        public void Mapping_NonReservedProperty_MapsProperly()
        {
            var testNRProperties = new Dictionary<string, string>()
            {
                { StringATestKey, "TestValue1" },
                { StringBTestKey, "TestValue2" }
            };

            var testEnricher = new TestEnricher()
                .WithNewRelicMetadataValue(testNRProperties);

            // Build test data and configure formatter
            var expectedOutputs = new Dictionary<string, string>();
            var inputValues = new Dictionary<string, int>();
            var testFormatter = new NewRelicFormatter();

            foreach (var prop in _newRelicPropertiesNotReserved)
            {
                var propName = Guid.NewGuid().ToString();
                var propValue = _random.Next(int.MaxValue);

                inputValues.Add(propName, propValue);
                expectedOutputs.Add(LoggingExtensions.GetOutputName(prop), propValue.ToString());

                testEnricher.WithUserPropValue(propName, propValue);
                testFormatter.WithPropertyMapping(propName, prop);
            }

            var testOutputSink = new TestSinkWithFormatter(testFormatter);

            var testLogger = SerilogTestHelpers.GetLogger(testOutputSink, testEnricher);

            // Act
            testLogger.Warning(LogMessage);

            // Assert
            AssertNoSerilogErrorsAndCountOutputs(_testRunDebugLogs, testOutputSink.InputsAndOutputs, LogMessage);

            var resultDic = SerilogTestHelpers.DeserializeOutputJSON(testOutputSink.InputsAndOutputs[0]);

            foreach(var expectedOutput in expectedOutputs)
            {
                Asserts.KeyAndValueMatch(resultDic, expectedOutput.Key, expectedOutput.Value);
            }

            foreach(var inputVal in inputValues)
            {
                Assert.That(resultDic, Does.Not.ContainKey(inputVal.Key));
                Assert.That(resultDic, Does.Not.ContainKey(UserPropertyKeyPrefix + inputVal.Key));
            }
        }
        private static void AssertThatPropertyCountsMatch(TestEnricher enricher, int countIntrinsics, Dictionary<string, JsonElement> jsonAsDic)
        {
            var expectedCount = enricher.CountUserProps + enricher.CountNewRelicProps + countIntrinsics;
            Assert.That(jsonAsDic.Count, Is.EqualTo(expectedCount), "Output Json Property Count Mismatch");
        }

        private static void AssertNoSerilogErrorsAndCountOutputs(List<string> serilogErrors, List<InputOutputPairing> inputsAndOutputs, string messageTemplate)
        {
            Assert.That(serilogErrors.Count, Is.EqualTo(0));
            Assert.That(inputsAndOutputs.Count, Is.EqualTo(1));
            Assert.That(inputsAndOutputs[0].LogEvent.MessageTemplate.Text, Is.EqualTo(messageTemplate));
        }

    }
}

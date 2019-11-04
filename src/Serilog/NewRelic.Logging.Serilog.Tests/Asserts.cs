using System.Collections.Generic;
using System.Text.Json;
using NUnit.Framework;

namespace NewRelic.Logging.Serilog.Tests
{
    public static class Asserts
    {
        public static void PropertyCountsMatch(TestEnricher enricher, int countIntrinsics, Dictionary<string, JsonElement> jsonAsDic)
        {
            var expectedCount = enricher.CountUserProps + enricher.CountNewRelicProps + countIntrinsics;
            Assert.That(jsonAsDic.Count, Is.EqualTo(expectedCount), "Output Json Property Count Mismatch");
        }

        public static void KeyAndValueMatch(Dictionary<string, JsonElement> resultDic, string key, object value)
        {
            Assert.That(resultDic, Contains.Key(key));
            var valueType = value.GetType();
            if (valueType == typeof(string))
            {
                Assert.That(resultDic[key].GetString(), Is.EqualTo(value));
            }
            else if (valueType == typeof(bool))
            {
                Assert.That(resultDic[key].GetBoolean(), Is.EqualTo(value));
            }
            else if (valueType == typeof(int))
            {
                Assert.That(resultDic[key].GetInt32(), Is.EqualTo(value));
            }
            else if (valueType == typeof(JsonValueKind))
            {
                Assert.That(resultDic[key].ValueKind, Is.EqualTo(value));
            }
            else
            {
                Assert.Fail("Unhandled value type");
            }
        }

        public static void NoSerilogErrorsCountOutputs(List<string> serilogErrors, List<InputOutputPairing> inputsAndOutputs, string messageTemplate)
        {
            Assert.That(serilogErrors.Count, Is.EqualTo(0));
            Assert.That(inputsAndOutputs.Count, Is.EqualTo(1));
            Assert.That(inputsAndOutputs[0].LogEvent.MessageTemplate.Text, Is.EqualTo(messageTemplate));
        }

    }

}

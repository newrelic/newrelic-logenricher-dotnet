// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0


using System.Collections.Generic;
using System.Text.Json;
using NUnit.Framework;

namespace NewRelic.LogEnrichers
{
    public static class Asserts
    {
        public static void KeyAndValueMatch(Dictionary<string, JsonElement> resultDic, string key, object value)
        {
            Assert.That(resultDic, Contains.Key(key));

            if (value == null)
            {
                Assert.IsTrue(resultDic[key].ValueKind == JsonValueKind.Null);
                return;
            }

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
            else if (valueType == typeof(long))
            {
                Assert.That(resultDic[key].GetInt64(), Is.EqualTo(value));
            }
            else if (valueType == typeof(double))
            {
                Assert.That(resultDic[key].GetDouble(), Is.EqualTo(value));
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
    }
}
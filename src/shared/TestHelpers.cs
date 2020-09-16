// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace NewRelic.LogEnrichers
{
    public static class TestHelpers
    {
        /// <summary>
        /// Responsible for deserializing the JSON that was built.
        /// Will ensure that the JSON is well-formed
        /// </summary>
        public static  Dictionary<string, JsonElement> DeserializeOutputJSON(string output)
        {
            var resultDic = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(output);
            return resultDic;
        }

        public static void CreateStackTracedError(int level, Exception exception, int throwAtLevel)
        {
            if (level == throwAtLevel)
            {
                throw exception;
            }

            CreateStackTracedError(level + 1, exception, throwAtLevel);
        }

    }
}

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace NewRelic.LogEnrichers.NLog.Tests
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
    }
}

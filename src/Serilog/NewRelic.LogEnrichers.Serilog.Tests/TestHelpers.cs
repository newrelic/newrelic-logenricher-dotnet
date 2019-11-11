using System;
using System.Collections.Generic;
using System.Text.Json;
using Serilog;
using Serilog.Core;

namespace NewRelic.LogEnrichers.Serilog.Tests
{
    public static class TestHelpers
    {
        public static Logger GetLogger(ILogEventSink outputSink, params ILogEventEnricher[] enrichers)
        {
            return new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.With(enrichers)
                .WriteTo.Sink(outputSink)
                .CreateLogger();
        }

        public static void CreateStackTracedError(int level, Exception exception, int throwAtLevel)
        {
            if (level == throwAtLevel)
            {
                throw exception;
            }

            CreateStackTracedError(level + 1, exception, throwAtLevel);
        }

        /// <summary>
        /// Responsible for deserializing the JSON that was built.
        /// Will ensure that the JSON is well-formed
        /// </summary>
        public static  Dictionary<string, JsonElement> SerializeOutputJSON(InputOutputPairing pairing)
        {
            var resultJSON = pairing.FormattedOutput;
            var resultDic = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(resultJSON);
            return resultDic;
        }
    }
}

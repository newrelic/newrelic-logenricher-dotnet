// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Text.Json;
using Serilog;
using Serilog.Core;

namespace NewRelic.LogEnrichers.Serilog.Tests
{
    public static class SerilogTestHelpers
    {
        public static Logger GetLogger(ILogEventSink outputSink, params ILogEventEnricher[] enrichers)
        {
            return new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.With(enrichers)
                .WriteTo.Sink(outputSink)
                .CreateLogger();
        }

        /// <summary>
        /// Responsible for deserializing the JSON that was built.
        /// Will ensure that the JSON is well-formed
        /// </summary>
        public static  Dictionary<string, JsonElement> DeserializeOutputJSON(InputOutputPairing pairing)
        {
            var resultJSON = pairing.FormattedOutput;
            return TestHelpers.DeserializeOutputJSON(resultJSON);
        }
    }
}

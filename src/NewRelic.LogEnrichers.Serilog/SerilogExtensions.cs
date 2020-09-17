// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using Serilog;
using Serilog.Configuration;

namespace NewRelic.LogEnrichers.Serilog
{
    public static class SerilogExtensions
    {
        public static LoggerConfiguration WithNewRelicLogsInContext(this LoggerEnrichmentConfiguration enricherConfig)
        {
            return enricherConfig.With<NewRelicEnricher>();
        }
    }
}

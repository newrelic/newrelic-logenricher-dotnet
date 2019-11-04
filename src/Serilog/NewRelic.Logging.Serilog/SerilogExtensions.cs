using System;
using System.Runtime.CompilerServices;
using System.Text;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace NewRelic.Logging.Serilog
{
    public static class SerilogExtensions
    {
        public static LoggerConfiguration WithNewRelicLogsInContext(this LoggerEnrichmentConfiguration enricherConfig)
        {
            return enricherConfig.With<NewRelicEnricher>();
        }
    }
}

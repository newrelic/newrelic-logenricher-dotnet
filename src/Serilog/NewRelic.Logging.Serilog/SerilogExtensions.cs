using Serilog;
using Serilog.Configuration;

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

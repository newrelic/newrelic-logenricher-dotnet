using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NewRelic.LogEnrichers.Microsoft.Extensions.Logging.Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to the New Relic Logging Extensions for Microsoft.Extensions.Logging");
            Console.WriteLine();

            #region Custom LoggerProvider Experiment

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .ClearProviders()   //?
                    .AddProvider(new LogEnricherLoggerProvider())
                    .AddLogEnricherFormatter(options => options.CustomPrefix = " ~~~~~ ");
            });

            // TODO: logger returned from here is a MEL.Logger, not a LogEnricherLogger
            // so something isn't plumbed correctly above.
            ILogger logger = loggerFactory.CreateLogger<Program>();

            // The call to GetLinkingMetadata must be made in the same context as the
            // call to Log(). If it is made later, like when the LogEntry is being formatted,
            // the context may not be the same as the log message and will not link up properly
            // in the NR UI.
            var agent = NewRelic.Api.Agent.NewRelic.GetAgent();
            var linkingMetadata = agent.GetLinkingMetadata();
            Dictionary<string, object> properties = new Dictionary<string, object>
            {
                ["property1key"] = "property1value",
                ["property2key"] = 66,
                ["newrelic.linkingmetadata"] = linkingMetadata
            };

            // TODO: As a result of the wrong logger type returned on line 28,
            // this call to Log() isn't going to the LogEnricherLogger and
            // no properties are being added.
            logger.Log(LogLevel.Debug, "Example using custom LogEnricherLoggerProvider", properties);

            #endregion Custom LoggerProvider Experiment

            #region BeginScope Experiment

            using ILoggerFactory melLoggerFactory =
                LoggerFactory.Create(builder =>
                    builder.AddJsonConsole(options =>
                    {
                        options.IncludeScopes = true;
                        options.TimestampFormat = "hh:mm:ss ";
                        options.JsonWriterOptions = new JsonWriterOptions
                        {
                            Indented = true
                        };
                    })
                );

            ILogger<Program> melLogger = melLoggerFactory.CreateLogger<Program>();
            using (melLogger.BeginScope(new Dictionary<string, object>
            {
                ["property1key"] = "property1value",
                ["property2key"] = 66,
                ["newrelic.linkingmetadata"] = linkingMetadata
            }))
            {
                melLogger.LogInformation("Example using Scope to add Properties");
            }

            #endregion BeginScope Experiment
        }
    }
}

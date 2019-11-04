using System;
using NewRelic.Api.Agent;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace NewRelic.Logging.Serilog
{
    public class NewRelicEnricher : ILogEventEnricher
    {
        private const string LinkingMetadataKey = "newrelic.linkingmetadata";

        private readonly Lazy<IAgent> _nrAgent;
        
        /// <summary>
        /// This constructor is available for testing purposes.
        /// </summary>
        /// <param name="agentFactory"></param>
        internal NewRelicEnricher(Func<IAgent> agentFactory)
        {
            _nrAgent = new Lazy<IAgent>(agentFactory);
        }

        public NewRelicEnricher() : this(NewRelic.Api.Agent.NewRelic.GetAgent)
        {
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var linkingMetadata = _nrAgent.Value?.GetLinkingMetadata();

            // null values within the dictionary will be handled within the formatter.
            if (linkingMetadata != null && linkingMetadata.Keys.Count != 0)
            {
                // our key is unique enough that we are okay with overwriting it.
                logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(LinkingMetadataKey, linkingMetadata));
            }
        }
    }
}

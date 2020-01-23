using System;
using System.Runtime.CompilerServices;
using log4net.Appender;
using log4net.Core;
using NewRelic.Api.Agent;

namespace NewRelic.LogEnrichers.Log4Net
{
	public class NewRelicAppender : ForwardingAppender
	{
        private const string LinkingMetadataKey = "newrelic.linkingmetadata";
        private readonly Lazy<IAgent> _nrAgent;

        internal NewRelicAppender(Func<IAgent> agentFactory)
        {
            _nrAgent = new Lazy<IAgent>(agentFactory);
        }

        public NewRelicAppender() : this(NewRelic.Api.Agent.NewRelic.GetAgent)
        {
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            var linkingMetadata = _nrAgent.Value?.GetLinkingMetadata();

            if (linkingMetadata != null && linkingMetadata.Keys.Count != 0)
            {
                loggingEvent.Properties[LinkingMetadataKey] = linkingMetadata;
            }

            base.Append(loggingEvent);
        }

        protected override void Append(LoggingEvent[] loggingEvents)
        {
            var linkingMetadata = _nrAgent.Value?.GetLinkingMetadata();

            if (linkingMetadata != null && linkingMetadata.Keys.Count != 0)
            {
                foreach (var loggingEvent in loggingEvents)
                {
                    loggingEvent.Properties[LinkingMetadataKey] = linkingMetadata;
                }
            }

            base.Append(loggingEvents);
        }
    }
}

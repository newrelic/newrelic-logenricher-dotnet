// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using NewRelic.Api.Agent;
using Serilog.Core;
using Serilog.Events;

namespace NewRelic.LogEnrichers.Serilog
{
    public class NewRelicEnricher : ILogEventEnricher
    {
        private const string LinkingMetadataKey = "newrelic.linkingmetadata";

        private readonly Lazy<IAgent> _nrAgent;

        public NewRelicEnricher()
            : this(NewRelic.Api.Agent.NewRelic.GetAgent)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NewRelicEnricher"/> class.
        /// This constructor is available for testing purposes.
        /// </summary>
        /// <param name="agentFactory"></param>
        internal NewRelicEnricher(Func<IAgent> agentFactory)
        {
            _nrAgent = new Lazy<IAgent>(agentFactory);
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

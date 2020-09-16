// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using log4net.Appender;
using log4net.Core;
using log4net.Util;
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
            try
            {
                var linkingMetadata = _nrAgent.Value?.GetLinkingMetadata();

                if (linkingMetadata != null && linkingMetadata.Keys.Count != 0)
                {
                    loggingEvent.Properties[LinkingMetadataKey] = linkingMetadata;
                }
            } 
            catch(Exception ex) 
            {
                LogLog.Error(GetType(), "Exception caught in NewRelic.LogEnrichers.Log4Net.NewRelicAppender.Append", ex);
            }

            base.Append(loggingEvent);
        }
    }
}

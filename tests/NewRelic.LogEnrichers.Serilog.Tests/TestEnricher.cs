// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Serilog.Core;
using Serilog.Events;

namespace NewRelic.LogEnrichers.Serilog.Tests
{
    public class TestEnricher : ILogEventEnricher
    {
        private const string LinkingMetadataKey = "newrelic.linkingmetadata";

        private readonly List<KeyValuePair<string, object>> _props = new List<KeyValuePair<string, object>>();

        public int CountNewRelicProps { get; private set; }

        public int CountUserProps { get; private set; }

        public TestEnricher WithUserPropValue(string name, object value)
        {
            CountUserProps++;
            _props.Add(new KeyValuePair<string, object>(name, value));
            return this;
        }

        public TestEnricher WithNewRelicMetadataValue(Dictionary<string, string> metadataProps)
        {
            CountNewRelicProps = metadataProps.Count;
            _props.Add(new KeyValuePair<string, object>(LinkingMetadataKey, metadataProps));
            return this;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            foreach (var prop in _props)
            {
                logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(prop.Key, prop.Value));
            }
        }
    }
}

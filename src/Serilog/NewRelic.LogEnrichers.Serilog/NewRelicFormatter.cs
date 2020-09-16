// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace NewRelic.LogEnrichers.Serilog
{
    public class NewRelicFormatter : ITextFormatter
    {
        private const string LinkingMetadataKey = "newrelic.linkingmetadata";
        private const char JsonOpen = '{';
        private const char JsonClose = '}';
        private const char JsonDelim = ',';
        private const char JsonColon = ':';
        private const char JsonAtSign = '@';

        private static readonly ScalarValue JsonNull = new ScalarValue(null);

        private readonly JsonValueFormatter _valueFormatter = new JsonValueFormatter();

        private readonly Dictionary<string, string> _propertyMappings = new Dictionary<string, string>();

        private static readonly Dictionary<LogEventLevel, string> _cacheLogLevelNames =
            Enum.GetValues(typeof(LogEventLevel))
            .Cast<LogEventLevel>()
            .ToDictionary(x => x, x => x.ToString());

        /// <summary>
        /// This formatter already handles these items.  Users should not 
        /// override their output values
        /// </summary>
        private readonly NewRelicLoggingProperty[] _reservedProperties = new[]
        {
            NewRelicLoggingProperty.Timestamp,
            NewRelicLoggingProperty.ErrorMessage,
            NewRelicLoggingProperty.ErrorClass,
            NewRelicLoggingProperty.ErrorStack,
            NewRelicLoggingProperty.MessageText,
            NewRelicLoggingProperty.MessageTemplate,
            NewRelicLoggingProperty.LogLevel
        };
        
        public NewRelicFormatter() 
        {
        }

        public NewRelicFormatter WithPropertyMapping(string propertyName, NewRelicLoggingProperty outputAsNewRelicProperty)
        {
            if (_reservedProperties.Contains(outputAsNewRelicProperty))
            {
                throw new InvalidOperationException($"The New Relic Serilog Extension does not allow mapping of property {outputAsNewRelicProperty}");
            }

            _propertyMappings[propertyName] = LoggingExtensions.GetOutputName(outputAsNewRelicProperty);
            return this;
        }

        public void Format(LogEvent logEvent, TextWriter output)
        {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));
            if (output == null) throw new ArgumentNullException(nameof(output));

            output.Write(JsonOpen);

            WriteIntrinsicProperties(logEvent, output);
            WriteExceptionProperties(logEvent.Exception, output);
            WriteUserProperties(logEvent, output);

            output.Write(JsonClose);
            output.WriteLine();
        }

        private void WriteIntrinsicProperties(LogEvent logEvent, TextWriter output)
        {
            WriteTimestamp(logEvent.Timestamp.DateTime.ToUnixTimeMilliseconds(), output); // do this first to make commas in JSON easier
            WriteFormattedJsonData(NewRelicLoggingProperty.LogLevel.GetOutputName(), _cacheLogLevelNames[logEvent.Level], output);
            WriteFormattedJsonData(NewRelicLoggingProperty.MessageTemplate.GetOutputName(), logEvent.MessageTemplate, output);
            WriteFormattedJsonData(NewRelicLoggingProperty.MessageText.GetOutputName(), logEvent.MessageTemplate.Render(logEvent.Properties), output);
        }

        private void WriteExceptionProperties(Exception exception, TextWriter output)
        {
            if (exception == null)
            {
                return;
            }

            WriteFormattedJsonData(NewRelicLoggingProperty.ErrorClass.GetOutputName(), exception.GetType().FullName, output);

            if (!string.IsNullOrWhiteSpace(exception.Message))
            {
                WriteFormattedJsonData(NewRelicLoggingProperty.ErrorMessage.GetOutputName(), exception.Message, output);
            }

            if (!string.IsNullOrWhiteSpace(exception.StackTrace))
            {
                WriteFormattedJsonData(NewRelicLoggingProperty.ErrorStack.GetOutputName(), exception.StackTrace, output);
            }
        }

        private void WriteLinkingMetadataProperties(LogEventPropertyValue nrPropValues, TextWriter output)
        {
            var nrPropValuesDic = nrPropValues as DictionaryValue;
            if (nrPropValuesDic == null)
            {
                return;
            }

            foreach (var nrPropValue in nrPropValuesDic.Elements)
            {
                var propName = nrPropValue.Key;
                var propValue = nrPropValue.Value;
                WriteFormattedJsonData(propName.Value.ToString(), propValue, output);
            }
        }

        private void WriteUserProperties(LogEvent logEvent, TextWriter output)
        {

            foreach (var kvp in logEvent.Properties)
            {
                var key = kvp.Key;
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                if (key == LinkingMetadataKey)
                {
                    WriteLinkingMetadataProperties(kvp.Value, output);
                    continue;
                }

                //We don't expect to receive any NR properties that have @ in their keys.
                if (key[0] == JsonAtSign && key.Length >= 2 && key[1] != JsonAtSign)
                {
                    key = JsonAtSign + key;
                }

                //If a custom property mapping exists, use it.
                if (_propertyMappings.TryGetValue(key, out var jsonPropName))
                {
                    WriteFormattedJsonData(jsonPropName, kvp.Value.ToString(), output);
                }
                else
                {
                    WriteFormattedJsonData(LoggingExtensions.UserPropertyPrefix + key, kvp.Value, output);
                }
            }
        }

        private void WriteTimestamp(long unixTimestampValue, TextWriter output)
        {
            JsonValueFormatter.WriteQuotedJsonString(NewRelicLoggingProperty.Timestamp.GetOutputName(), output);
            output.Write(JsonColon);
            output.Write(unixTimestampValue);
        }

        /// <summary>
        /// Writes out a single formatted and escaped JSON data entry.
        /// </summary>
        private void WriteFormattedJsonData(string key, LogEventPropertyValue value, TextWriter output)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            output.Write(JsonDelim);
            JsonValueFormatter.WriteQuotedJsonString(key, output);
            output.Write(JsonColon);
            _valueFormatter.Format(value, output);
        }

        private void WriteFormattedJsonData(string key, object value, TextWriter output)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            output.Write(JsonDelim);
            JsonValueFormatter.WriteQuotedJsonString(key, output);
            output.Write(JsonColon);

            if (value == null)
            {
                _valueFormatter.Format(JsonNull, output);
            }
            else
            {
                JsonValueFormatter.WriteQuotedJsonString(value.ToString(), output);
            }
        }
    }
}

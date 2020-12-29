// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.IO;
using log4net.Core;
using log4net.Layout;
using Newtonsoft.Json;

namespace NewRelic.LogEnrichers.Log4Net
{
    public class NewRelicLayout : LayoutSkeleton
    {
        private const string LinkingMetadataKey = "newrelic.linkingmetadata";

        public NewRelicLayout()
        {
            IgnoresException = false;
        }

        public override void ActivateOptions()
        {
        }

        public override void Format(TextWriter writer, LoggingEvent loggingEvent)
        {
            var dic = new Dictionary<string, object>();

            SetInstrinsics(dic, loggingEvent);
            SetExceptionData(dic, loggingEvent);
            SetUserProperties(dic, loggingEvent);
            SetLinkMetaData(dic, loggingEvent);

            writer.WriteLine(JsonConvert.SerializeObject(dic));
        }

        private void SetInstrinsics(Dictionary<string, object> dictionary, LoggingEvent loggingEvent)
        {
            dictionary.Add(LoggingExtensions.GetOutputName(NewRelicLoggingProperty.Timestamp), loggingEvent.TimeStamp.ToUnixTimeMilliseconds());
            dictionary.Add(LoggingExtensions.GetOutputName(NewRelicLoggingProperty.ThreadName), loggingEvent.ThreadName);
            dictionary.Add(LoggingExtensions.GetOutputName(NewRelicLoggingProperty.MessageText), loggingEvent.RenderedMessage);
            dictionary.Add(LoggingExtensions.GetOutputName(NewRelicLoggingProperty.LogLevel), loggingEvent.Level.Name);
            dictionary.Add(LoggingExtensions.GetOutputName(NewRelicLoggingProperty.LoggerName), loggingEvent.LoggerName);
        }

        private void SetExceptionData(Dictionary<string, object> dictionary, LoggingEvent loggingEvent)
        {
            if (loggingEvent.ExceptionObject != null)
            {
                if (!string.IsNullOrEmpty(loggingEvent.ExceptionObject.Message))
                {
                    dictionary.Add(LoggingExtensions.GetOutputName(NewRelicLoggingProperty.ErrorMessage), loggingEvent.ExceptionObject.Message);
                }

                if (!string.IsNullOrEmpty(loggingEvent.ExceptionObject.StackTrace))
                {
                    dictionary.Add(LoggingExtensions.GetOutputName(NewRelicLoggingProperty.ErrorStack), loggingEvent.ExceptionObject.StackTrace);
                }

                dictionary.Add(LoggingExtensions.GetOutputName(NewRelicLoggingProperty.ErrorClass), loggingEvent.ExceptionObject.GetType().ToString());
            }
        }

        private void SetUserProperties(Dictionary<string, object> dictionary, LoggingEvent e)
        {
            var properties = e.GetProperties();

            foreach (var key in properties.GetKeys())
            {
                if (key != LinkingMetadataKey)
                {
                    dictionary[LoggingExtensions.UserPropertyPrefix + key] = properties[key];
                }
            }
        }

        private void SetLinkMetaData(Dictionary<string, object> dictionary, LoggingEvent e)
        {
            if (e.Properties[LinkingMetadataKey] is Dictionary<string, string> linkdata)
            {
                foreach (var kv in linkdata)
                {
                    dictionary[kv.Key] = kv.Value;
                }
            }
        }
    }
}

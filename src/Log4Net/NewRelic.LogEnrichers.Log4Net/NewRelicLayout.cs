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
            base.IgnoresException = false;
        }
        public override void ActivateOptions()
        {
        }

        public override void Format(TextWriter writer, LoggingEvent e)
        {
            var dic = new Dictionary<string, object>();

            SetInstrinsics(dic, e);
            SetExceptionData(dic, e);
            SetUserProperties(dic, e);
            SetLinkMetaData(dic, e);

            writer.WriteLine(JsonConvert.SerializeObject(dic));
        }

        void SetInstrinsics(Dictionary<string, object> dictionary, LoggingEvent e)
        {
            if (dictionary == null)
            {
                return;
            }

            dictionary.Add(LoggingExtensions.GetOutputName(NewRelicLoggingProperty.Timestamp), e.TimeStamp.ToUnixTimeMilliseconds());
            dictionary.Add(LoggingExtensions.GetOutputName(NewRelicLoggingProperty.ThreadName), e.ThreadName);
            dictionary.Add(LoggingExtensions.GetOutputName(NewRelicLoggingProperty.MessageText), e.RenderedMessage);
            dictionary.Add(LoggingExtensions.GetOutputName(NewRelicLoggingProperty.LogLevel), e.Level);
        }

        void SetExceptionData(Dictionary<string, object> dictionary, LoggingEvent e) 
        {
            if (dictionary == null)
            {
                return;
            }

            if (e.ExceptionObject != null)
            {
                dictionary.Add(LoggingExtensions.GetOutputName(NewRelicLoggingProperty.ErrorMessage), e.ExceptionObject?.Message);
                dictionary.Add(LoggingExtensions.GetOutputName(NewRelicLoggingProperty.ErrorStack), e.ExceptionObject?.StackTrace);
                dictionary.Add(LoggingExtensions.GetOutputName(NewRelicLoggingProperty.ErrorClass), e.ExceptionObject?.GetType().ToString());
            }
        }

        void SetUserProperties(Dictionary<string, object> dictionary, LoggingEvent e)
        {
            var properties = e.GetProperties();
            if (dictionary == null)
            {
                return;
            }

            foreach (var key in properties.GetKeys())
            {
                if (key != LinkingMetadataKey)
                {
                    dictionary[LoggingExtensions.UserPropertyPrefix + key] = properties[key];
                }
            }
        }

        void SetLinkMetaData(Dictionary<string, object> dictionary, LoggingEvent e)
        {
            if (dictionary == null)
            {
                return;
            }

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

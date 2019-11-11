using System;
using System.Collections.Generic;
using System.Linq;

namespace NewRelic.Logging
{
    public enum NewRelicLoggingProperty
    {
        ThreadId,
        Timestamp,
        ErrorMessage,
        ErrorClass,
        ErrorStack,
        MessageText,
        MessageTemplate,
        LogLevel,
        ThreadName,
        FileName,
        NameSpace,
        ClassName,
        MethodName,
        LineNumber
    };

    internal static class LoggingExtensions
    {
        public const string UserPropertyPrefix = "Message Properties.";

        public static string GetOutputName(this NewRelicLoggingProperty property)
        {
            switch (property)
            {
                case NewRelicLoggingProperty.ThreadId:
                    return "thread.id";
                case NewRelicLoggingProperty.Timestamp:
                    return "timestamp";
                case NewRelicLoggingProperty.ErrorMessage:
                    return "error.message";
                case NewRelicLoggingProperty.ErrorClass:
                    return "error.class";
                case NewRelicLoggingProperty.ErrorStack:
                    return "error.stack";
                case NewRelicLoggingProperty.MessageText:
                    return "message";
                case NewRelicLoggingProperty.MessageTemplate:
                    return "message.template";
                case NewRelicLoggingProperty.LogLevel:
                    return "log.level";
                case NewRelicLoggingProperty.MethodName:
                    return "method.name";
                case NewRelicLoggingProperty.ThreadName:
                    return "thread.name";
                case NewRelicLoggingProperty.FileName:
                    return "file.name";
                case NewRelicLoggingProperty.NameSpace:
                    return "namespace";
                case NewRelicLoggingProperty.ClassName:
                    return "class.name";
                case NewRelicLoggingProperty.LineNumber:
                    return "line.number";

                default:
                    throw new KeyNotFoundException($"New Relic Logging Field {property}");
            }
        }

        private static NewRelicLoggingProperty[] _allNewRelicLoggingProperties;
        public static NewRelicLoggingProperty[] AllNewRelicLoggingProperties => 
            _allNewRelicLoggingProperties ?? (_allNewRelicLoggingProperties = Enum.GetValues(typeof(NewRelicLoggingProperty)).Cast<NewRelicLoggingProperty>().ToArray());
      
    }
}

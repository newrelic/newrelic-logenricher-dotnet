// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable
namespace NewRelic.LogEnrichers
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
        LineNumber,
        CorrelationId,
        ProcessId,
    }

    internal static class LoggingExtensions
    {
        public const string UserPropertyPrefix = "Message.Properties.";

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
                case NewRelicLoggingProperty.CorrelationId:
                    return "correlation.id";
                case NewRelicLoggingProperty.ProcessId:
                    return "process.id";
                default:
                    throw new KeyNotFoundException($"New Relic Logging Field {property}");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "StyleCop wants to apply two rules that are mutually exclusive, picked the most logical of the two.")]
        private static NewRelicLoggingProperty[]? _allNewRelicLoggingProperties;

        public static NewRelicLoggingProperty[] AllNewRelicLoggingProperties =>
            _allNewRelicLoggingProperties ?? (_allNewRelicLoggingProperties = Enum.GetValues(typeof(NewRelicLoggingProperty)).Cast<NewRelicLoggingProperty>().ToArray());
    }
}

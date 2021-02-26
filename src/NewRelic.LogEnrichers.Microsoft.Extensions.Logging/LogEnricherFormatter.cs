// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace NewRelic.LogEnrichers.Microsoft.Extensions.Logging
{
    // Custom ConsoleFormatter where the Properties could be added
    // in the Write() method. Depending on when the formatter gets called
    // in the MEL hierarchy, LinkingMetadata may need to be passed in if
    // Formatter gets called later/in a different context from the call
    // to Log()

    public sealed class LogEnricherFormatter : ConsoleFormatter, IDisposable
    {
        private readonly IDisposable _optionsReloadToken;
        private LogEnricherFormatterOptions _formatterOptions;

        public LogEnricherFormatter(IOptionsMonitor<LogEnricherFormatterOptions> options)
            // Case insensitive
            : base("customName") =>
            (_optionsReloadToken, _formatterOptions) =
                (options.OnChange(ReloadLoggerOptions), options.CurrentValue);

        private void ReloadLoggerOptions(LogEnricherFormatterOptions options) =>
            _formatterOptions = options;

        public override void Write<TState>(
            in LogEntry<TState> logEntry,
            IExternalScopeProvider scopeProvider,
            TextWriter textWriter)
        {
            if (logEntry.Exception is null)
            {
                return;
            }

            string message =
                logEntry.Formatter(
                    logEntry.State, logEntry.Exception);

            if (message == null)
            {
                return;
            }

            // Add the Properties here
            CustomLogicGoesHere(textWriter);
            textWriter.WriteLine(message);
        }

        private void CustomLogicGoesHere(TextWriter textWriter)
        {
            textWriter.Write(_formatterOptions.CustomPrefix);
        }

        public void Dispose() => _optionsReloadToken?.Dispose();
    }

    public static class LogEnricherLoggerExtensions
    {
        public static ILoggingBuilder AddLogEnricherFormatter(
            this ILoggingBuilder builder,
            Action<LogEnricherFormatterOptions> configure) =>
            builder.AddConsole(options => options.FormatterName = "customName")
                .AddConsoleFormatter<LogEnricherFormatter, LogEnricherFormatterOptions>(configure);
    }

    public class LogEnricherFormatterOptions : ConsoleFormatterOptions
    {
        public string CustomPrefix { get; set; }
    }
}

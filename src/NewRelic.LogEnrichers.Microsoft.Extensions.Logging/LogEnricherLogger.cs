// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using Microsoft.Extensions.Logging;

namespace NewRelic.LogEnrichers.Microsoft.Extensions.Logging
{
    // Custom Logger where Log() override, called by the application, can be
    // used to create a LogRecord to contain message and Properties

    internal class LogEnricherLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly LogEnricherLoggerProvider _provider;

        internal LogEnricherLogger(string categoryName, LogEnricherLoggerProvider provider)
        {
            _categoryName = categoryName ?? throw new ArgumentNullException(nameof(categoryName));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            {
                return;
            }

            var record = new LogRecord(DateTime.UtcNow, _categoryName, logLevel, eventId, state, exception);
        }
    }
}

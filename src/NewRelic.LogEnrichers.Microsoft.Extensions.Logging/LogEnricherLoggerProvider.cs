// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace NewRelic.LogEnrichers.Microsoft.Extensions.Logging
{
    // Custom LoggerProvider to pass back custom Logger (LogEnricherLogger)

    public class LogEnricherLoggerProvider : ILoggerProvider
    {
        private readonly IDictionary<string, ILogger> _loggers;

        public LogEnricherLoggerProvider()
        {
            _loggers = new Dictionary<string, ILogger>(StringComparer.Ordinal);
        }

        public ILogger CreateLogger(string categoryName)
        {
            lock (_loggers)
            {
                ILogger logger;

                if (_loggers.TryGetValue(categoryName, out logger))
                {
                    return logger;
                }

                logger = new LogEnricherLogger(categoryName, this);
                _loggers.Add(categoryName, logger);
                return logger;
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}

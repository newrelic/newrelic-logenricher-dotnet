// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using log4net;

namespace NewRelic.LogEnrichers.Log4Net.Tests
{
    public static class CustomLoggingExtensions
    {
        public static void Log(this ILog logger, string level, object message, Exception exception)
        {
            switch (level.ToUpper())
            {
                case "INFO":
                    logger.Info(message, exception);
                    break;
                case "DEBUG":
                    logger.Debug(message, exception);
                    break;
                case "WARN":
                    logger.Warn(message, exception);
                    break;
                case "FATAL":
                    logger.Fatal(message, exception);
                    break;
                case "ERROR":
                    logger.Error(message, exception);
                    break;
                default:
                    throw new Exception(@$"level:{level} not recognized logging level.");
            }
        }
    }
}

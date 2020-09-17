// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using NLog;
using NLog.LayoutRenderers;
using System.Text;

namespace NewRelic.LogEnrichers.NLog
{
    [LayoutRenderer(NewRelicJsonLayout.TimestampLayoutRendererName)]
    public class UnixTimestampLayoutRenderer : LayoutRenderer
    {
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            builder.Append(logEvent.TimeStamp.ToUnixTimeMilliseconds());
        }
    }
}

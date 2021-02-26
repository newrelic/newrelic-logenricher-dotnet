using System;
using Microsoft.Extensions.Logging;

namespace NewRelic.LogEnrichers.Microsoft.Extensions.Logging
{
    public sealed class LogRecord
    {
        internal LogRecord(DateTime timestamp, string categoryName, LogLevel logLevel, EventId eventId, object state, Exception exception)
        {
            this.Timestamp = timestamp;

            var activity = Activity.Current;
            if (activity != null)
            {
                this.TraceId = activity.TraceId;
                this.SpanId = activity.SpanId;
                this.TraceState = activity.TraceStateString;
                this.TraceFlags = activity.ActivityTraceFlags;
            }

            this.CategoryName = categoryName;
            this.LogLevel = logLevel;
            this.EventId = eventId;
            this.State = state;
            this.Exception = exception;
        }

        public DateTime Timestamp { get; }

        public ActivityTraceId TraceId { get; }

        public ActivitySpanId SpanId { get; }

        public ActivityTraceFlags TraceFlags { get; }

        public string TraceState { get; }

        public string CategoryName { get; }

        public LogLevel LogLevel { get; }

        public EventId EventId { get; }

        public object State { get; }

        public Exception Exception { get; }
    }
}

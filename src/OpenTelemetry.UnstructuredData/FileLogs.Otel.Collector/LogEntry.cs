using System;

namespace FileLogs.Otel.Collector
{
    public sealed class LogEntry
    {
        public DateTimeOffset Timestamp { get; }
        
        public string LogType { get; }
        
        public string Message { get; }

        public LogEntry(DateTimeOffset timestamp, string logType, string message)
        {
            Timestamp = timestamp;
            LogType = logType;
            Message = message;
        }
    }
}
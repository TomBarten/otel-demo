using System;

namespace FileLogs.Otel.Collector;

public sealed class LogEntry
{
    public DateTimeOffset Timestamp { get; init; }
        
    public string LogLevel { get; init; }
        
    public string Message { get; init; }
}
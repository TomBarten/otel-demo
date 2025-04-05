namespace FileLogs.Otel.Collector
{
    public sealed class LogEntry
    {
        public string LogEntryPrefix { get; }
        
        public string LogEntryMessage { get; }

        public LogEntry(string logEntryPrefix, string fullLogLine)
        {
            LogEntryPrefix = logEntryPrefix;
            LogEntryMessage = fullLogLine.Substring(logEntryPrefix.Length).Trim();
        }
    }
}
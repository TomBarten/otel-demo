using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;

namespace FileLogs.Otel.Collector
{
    public sealed class LogEntryOtelParser
    {
        public async Task WriteLogEntries(IEnumerable<LogEntry> logEntries)
        {
            foreach (var kv in GroupLogEntriesByTimestamp(logEntries))
            {
                await WriteLogs(kv.Value);
            }   
        }

        private async Task WriteLogs(IReadOnlyList<LogEntry> logEntries)
        {
            foreach (var logEntry in logEntries)
            {
            }
        }
        
        private IReadOnlyDictionary<DateTimeOffset, List<LogEntry>> GroupLogEntriesByTimestamp(IEnumerable<LogEntry> logEntries)
        {
            var maxDelayBetweenRelatedLogs = TimeSpan.FromMinutes(5);
            
            var orderedEntries = logEntries
                .OrderBy(logEntry => logEntry.Timestamp)
                .ToList();

            if (orderedEntries.Count <= 0)
            {
                return new Dictionary<DateTimeOffset, List<LogEntry>>(0);
            }
            
            var initialCapacity = orderedEntries.Count % 2 == 0 
                ? orderedEntries.Count / 2 
                : (orderedEntries.Count + 1) / 2;
            
            var groupedEntries = new Dictionary<DateTimeOffset, List<LogEntry>>(initialCapacity);
            
            DateTimeOffset? lastLogEntryTimestamp = null;

            foreach (var logEntry in orderedEntries)
            {
                var logEntryTimestamp = logEntry.Timestamp;

                if (groupedEntries.Count <= 0)
                {
                    groupedEntries.Add(logEntryTimestamp, new List<LogEntry> { logEntry });
                    lastLogEntryTimestamp = logEntryTimestamp;

                    continue;
                }

                if (lastLogEntryTimestamp == null)
                {
                    throw new InvalidOperationException("Last log entry timestamp is null");
                }
                
                var hasGreaterThanMaxDelay = logEntryTimestamp - lastLogEntryTimestamp > maxDelayBetweenRelatedLogs;

                if (hasGreaterThanMaxDelay)
                {
                    if (groupedEntries.ContainsKey(logEntryTimestamp))
                    {
                        throw new InvalidOperationException($"Entries are already grouped under timestamp {logEntryTimestamp}");
                    }
                    
                    groupedEntries.Add(logEntryTimestamp, new List<LogEntry> { logEntry });
                    
                    continue;
                }
                
                var lastTimestamp = lastLogEntryTimestamp.Value;
                    
                groupedEntries.TryGetValue(lastTimestamp, out var groupedLogEntries);

                if (groupedLogEntries == null)
                {
                    throw new InvalidOperationException(
                        $"Retrieved grouped log entries are null under timestamp: {lastTimestamp}");
                }
                    
                groupedLogEntries.Add(logEntry);

                if (!groupedEntries.Remove(lastTimestamp))
                {
                    throw new InvalidOperationException($"Failed to remove grouped log entries under timestamp {lastTimestamp}");
                }
                
                groupedEntries.Add(logEntryTimestamp, groupedLogEntries);
            }

            if (initialCapacity <= groupedEntries.Count)
            {
                return groupedEntries;
            }
            
            var trimmedGroupedEntries = new Dictionary<DateTimeOffset, List<LogEntry>>(groupedEntries);

            groupedEntries = trimmedGroupedEntries;

            return groupedEntries;
        }
    }
}
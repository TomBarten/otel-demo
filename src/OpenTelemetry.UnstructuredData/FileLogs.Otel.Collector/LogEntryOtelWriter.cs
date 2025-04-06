using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;

namespace FileLogs.Otel.Collector;

public sealed class LogEntryOtelWriter
{
    public void WriteLogEntries(IEnumerable<LogEntry> logEntries)
    {
        using var logFactory = LoggerFactory.Create(builder =>
        {
            builder.AddOpenTelemetry(otelOptions =>
            {
                // otelOptions.ParseStateValues = true;
                otelOptions.IncludeScopes = true;
                    
                otelOptions
                    .AddProcessor(new LogEntryTimestampProcessor())
                    .AddConsoleExporter();
            });
        });

        var logger = logFactory.CreateLogger<LogEntryOtelWriter>();
            
        foreach (var kv in GroupLogEntriesByTimestamp(logEntries))
        {
            WriteLogs(logger, kv.Value);
        }   
    }

    private static void WriteLogs(ILogger logger, IReadOnlyList<LogEntry> logEntries)
    {
        foreach (var logEntry in logEntries)
        {
            // Scope is used to later overwrite the timestamp using the processor
            using (logger.BeginScope(new Dictionary<string, object> 
                   {
                       { nameof(logEntry.Timestamp), logEntry.Timestamp },
                   }))
            {
                logger.Log(LogLevel.Critical, "{message}", logEntry.Message);
            }
        }
    }
        
    private static IReadOnlyDictionary<DateTimeOffset, List<LogEntry>> GroupLogEntriesByTimestamp(IEnumerable<LogEntry> logEntries)
    {
        var maxDelayBetweenRelatedLogs = TimeSpan.FromMinutes(5);
            
        var orderedEntries = logEntries
            .OrderBy(logEntry => logEntry.Timestamp)
            .ToList();

        if (orderedEntries.Count <= 0)
        {
            return ImmutableDictionary<DateTimeOffset, List<LogEntry>>.Empty;
        }
            
        var groupedEntries = new Dictionary<DateTimeOffset, List<LogEntry>>(3);
            
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
                    
                lastLogEntryTimestamp = logEntryTimestamp;
                    
                continue;
            }
                
            var lastTimestamp = lastLogEntryTimestamp.Value;

            if (!groupedEntries.TryGetValue(lastTimestamp, out var groupedLogEntries))
            {
                throw new InvalidOperationException(
                    $"There are no grouped entries under timestamp: {lastTimestamp}");
            }
                    
            groupedLogEntries.Add(logEntry);
                
            if (logEntryTimestamp == lastTimestamp)
            {
                lastLogEntryTimestamp = logEntryTimestamp;
                continue;
            }
                
            if (!groupedEntries.Remove(lastTimestamp))
            {
                throw new InvalidOperationException($"Failed to remove grouped log entries under timestamp {lastTimestamp}");
            }
                
            groupedEntries.Add(logEntryTimestamp, groupedLogEntries);
                
            lastLogEntryTimestamp = logEntryTimestamp;
        }
            
        groupedEntries.TrimExcess();

        return groupedEntries;
    }
}
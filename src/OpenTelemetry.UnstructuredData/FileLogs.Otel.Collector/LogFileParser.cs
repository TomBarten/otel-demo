using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace FileLogs.Otel.Collector;

public sealed class LogFileParser
{
    private readonly LogConfiguration _config;

    public LogFileParser(LogConfiguration config)
    {
        _config = config;
    }
        
    public IEnumerable<LogEntry> ParseFile(FileInfo fileInfo)
    {
        using var reader = new StreamReader(fileInfo.OpenRead());
            
        var logEntryBuffer = new StringBuilder();

        string currentLine;
        Match previousStartLogEntryMatch = null;
        Match currentStartLogEntryMatch = null;

        while ((currentLine = reader.ReadLine()) != null)
        {
            var logLineStartMatch = _config.LogLineStartRegex.Match(currentLine);

            if (logLineStartMatch.Success)
            {
                if (currentStartLogEntryMatch != null)
                {
                    previousStartLogEntryMatch = currentStartLogEntryMatch;
                }

                currentStartLogEntryMatch = logLineStartMatch;

                if (logEntryBuffer.Length > 0)
                {
                    if (previousStartLogEntryMatch == null)
                    {
                        throw new InvalidOperationException("Previous log entry prefix match is null");
                    }

                    yield return ConstructLogEntry(previousStartLogEntryMatch, logEntryBuffer.ToString());
                            
                    logEntryBuffer.Clear();
                }
            }

            logEntryBuffer.AppendLine(currentLine);
        }

        if (logEntryBuffer.Length <= 0)
        {
            yield break;
        }
                
        if (currentStartLogEntryMatch == null)
        {
            throw new InvalidOperationException("Remaining log entry in the buffer without a prefix match");
        }

        yield return ConstructLogEntry(currentStartLogEntryMatch, logEntryBuffer.ToString());

        logEntryBuffer.Clear();
    }
        
    private LogEntry ConstructLogEntry(Match logLineStartMatch, string fullLog)
    {
        var logMessage = fullLog.Substring(logLineStartMatch.Length).Trim();

        var hasRetrievedLogType = TryRetrieveLogType(logLineStartMatch, fullLog, out var logType);
        var hasRetrievedLogTimestamp = TryRetrieveLogTimestamp(logLineStartMatch, fullLog, out var logTimestamp);

        if (!hasRetrievedLogType && !hasRetrievedLogTimestamp)
        {
            throw new InvalidOperationException("Unable to match log timestamp and type", 
                new ArgumentException(fullLog, nameof(fullLog)));
        }

        return new LogEntry
        {
            Timestamp = logTimestamp ?? throw new InvalidOperationException("Unable to match log timestamp",
                new ArgumentException(fullLog, nameof(fullLog))),
            LogLevel = logType ?? throw new InvalidOperationException("Unable to match log type",
                new ArgumentException(fullLog, nameof(fullLog))),
            Message = logMessage
        };
    }

    private bool TryRetrieveLogType(Match logLineStartMatch, string fullLog, out string logType)
    {
        logType = null;
            
        if (!string.IsNullOrWhiteSpace(_config.LogLineStartTypeMatchingGroup))
        {
            logType = logLineStartMatch.Groups[_config.LogLineStartTypeMatchingGroup].Value;
        }

        if (!string.IsNullOrWhiteSpace(logType) || _config.LogLineTypeRegex == null)
        {
            return !string.IsNullOrWhiteSpace(logType);
        }
            
        var logTypeMatch = _config.LogLineTypeRegex.Match(fullLog);

        if (!logTypeMatch.Success)
        {
            return !string.IsNullOrWhiteSpace(logType);
        }
            
        if (!string.IsNullOrWhiteSpace(_config.LogLineTypeMatchingGroup))
        {
            logType = logTypeMatch.Groups[_config.LogLineTypeMatchingGroup].Value;
        }

        if (string.IsNullOrWhiteSpace(logType))
        {
            logType = logTypeMatch.Value;
        }
            
        return !string.IsNullOrWhiteSpace(logType);
    }
        
    private bool TryRetrieveLogTimestamp(Match logLineStartMatch, string fullLog, out DateTimeOffset? logTimestamp)
    {
        logTimestamp = null;
            
        if (!string.IsNullOrWhiteSpace(_config.LogLineStartTimestampMatchingGroup))
        {
            var logTimestampString = logLineStartMatch.Groups[_config.LogLineStartTimestampMatchingGroup].Value;
                
            if (DateTimeOffset.TryParse(logTimestampString, out var logLineStartTimestamp))
            {
                logTimestamp = logLineStartTimestamp;
            }
        }

        if (logTimestamp != null || _config.LogLineTimestampRegex == null)
        {
            return logTimestamp != null;
        }
            
        var timestampMatch = _config.LogLineTimestampRegex.Match(fullLog);

        if (!timestampMatch.Success)
        {
            return logTimestamp != null;
        }
            
        string timestampString = null;
                
        if (!string.IsNullOrWhiteSpace(_config.LogLineTimestampMatchingGroup))
        {
            timestampString = timestampMatch.Groups[_config.LogLineTimestampMatchingGroup].Value;
        }

        if (string.IsNullOrWhiteSpace(timestampString))
        {
            timestampString = timestampMatch.Value;
        }
                    
        if (DateTimeOffset.TryParse(timestampString, out var logLineTimestamp))
        {
            logTimestamp = logLineTimestamp;
        }

        return logTimestamp != null;
    }
}
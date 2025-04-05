using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace FileLogs.Otel.Collector
{
    public sealed class LogFileParser
    {
        private readonly LogConfiguration _config;

        public LogFileParser(LogConfiguration config)
        {
            _config = config;
        }
        
        public IEnumerable<LogEntry> ParseFile(FileInfo fileInfo)
        {
            using (var reader = new StreamReader(fileInfo.OpenRead()))
            {
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
        }

        private LogEntry ConstructLogEntry(Match logLineStartMatch, string fullLog)
        {
            string logType = null;
            DateTimeOffset? logTimestamp = null;
            
            var logMessage = fullLog.Substring(logLineStartMatch.Length).Trim();
            
            if (!string.IsNullOrWhiteSpace(_config.LogLineStartTypeMatchingGroup))
            {
                logType = logLineStartMatch.Groups[_config.LogLineStartTypeMatchingGroup].Value;
            }

            if (!string.IsNullOrWhiteSpace(_config.LogLineStartTimestampMatchingGroup))
            {
                var logTimestampString = logLineStartMatch.Groups[_config.LogLineStartTimestampMatchingGroup].Value;
                
                if (DateTimeOffset.TryParse(logTimestampString, out var timestamp))
                {
                    logTimestamp = timestamp;
                }
            }

            if (logTimestamp == null && _config.LogLineTimestampRegex != null)
            {
                var timestampMatch = _config.LogLineTimestampRegex.Match(fullLog);

                if (timestampMatch.Success && DateTimeOffset.TryParse(timestampMatch.Value, out var timestamp))
                {
                    logTimestamp = timestamp;
                }
            }

            if (string.IsNullOrWhiteSpace(logType) && _config.LogLineTypeRegex != null)
            {
                var logTypeMatch = _config.LogLineTypeRegex.Match(fullLog);
                
                if (logTypeMatch.Success)
                {
                    logType = logTypeMatch.Value;
                }
            }
            
            if (logTimestamp == null && string.IsNullOrWhiteSpace(logType))
            {
                throw new InvalidOperationException("Unable to match log timestamp and type", 
                    new ArgumentException(fullLog, nameof(fullLog)));
            }

            return new LogEntry(
                timestamp: logTimestamp ?? throw new InvalidOperationException("Unable to match log timestamp", 
                    new ArgumentException(fullLog, nameof(fullLog))),
                logType: logType ?? throw new InvalidOperationException("Unable to match log type", 
                    new ArgumentException(fullLog, nameof(fullLog))),
                message: logMessage
            );
        }
    }
}
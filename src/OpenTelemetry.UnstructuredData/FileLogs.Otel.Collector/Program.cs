using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FileLogs.Otel.Collector
{
    internal class Program
    {
        public static void Main(string[] args)
        {

            const string informationLogFileName = "Log-information.log";
            var logLineStartRegex = new Regex(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} \w+");
            
            var informationLogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, informationLogFileName);

            if (!File.Exists(informationLogFilePath))
            {
                throw new FileNotFoundException("Cannot find information log file", informationLogFilePath); 
            }

            var informationLogFile = new FileInfo(informationLogFilePath);
            
            using (var reader = new StreamReader(informationLogFile.OpenRead()))
            {
                var logEntryBuffer = new StringBuilder();
                
                string currentLine;
                Match previousStartLogEntryMatch = null;
                Match currentStartLogEntryMatch = null;

                var logEntries = new List<LogEntry>();
                
                while ((currentLine = reader.ReadLine()) != null)
                {
                    var logLineStartMatch = logLineStartRegex.Match(currentLine);
                    
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
                            
                            logEntries.Add(new LogEntry(previousStartLogEntryMatch.Value, logEntryBuffer.ToString()));
                            logEntryBuffer.Clear();
                        }
                    }

                    logEntryBuffer.AppendLine(currentLine);
                }

                if (logEntryBuffer.Length > 0)
                {
                    if (currentStartLogEntryMatch == null)
                    {
                        throw new InvalidOperationException("Remaining log entry in the buffer without a prefix match");
                    }
                    
                    logEntries.Add(new LogEntry(currentStartLogEntryMatch.Value, logEntryBuffer.ToString()));
                }

                foreach (var logEntry in logEntries)
                {
                    Console.WriteLine(JsonSerializer.Serialize(logEntry));
                }
            }
        }
    }
}
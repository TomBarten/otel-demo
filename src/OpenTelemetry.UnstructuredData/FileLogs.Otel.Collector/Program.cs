using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FileLogs.Otel.Collector
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            const string informationLogFileName = "Log-information.log";
            
            var logLineStartRegex = new Regex(@"^(?<LogTimestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) (?<LogType>\w+)");
            const string logLineStartTypeMatchingGroup = "LogType";
            const string logLineStartTimestampMatchingGroup = "LogTimestamp";
            
            var logLineTimestampRegex = new Regex(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}");
            var logLineTypeRegex = new Regex(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} (?<LogType>\w+)");
            const string logLineTypeMatchingGroup = "LogType";

            var config = new LogConfiguration
            (
                logLineStartRegex: logLineStartRegex,
                logLineStartTypeMatchingGroup: logLineStartTypeMatchingGroup,
                logLineStartTimestampMatchingGroup: logLineStartTimestampMatchingGroup,
                logLineTimestampRegex: logLineTimestampRegex,
                logLineTypeRegex: logLineTypeRegex,
                logLineTypeMatchingGroup: logLineTypeMatchingGroup
            );
            
            var informationLogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, informationLogFileName);

            if (!File.Exists(informationLogFilePath))
            {
                throw new FileNotFoundException("Cannot find information log file", informationLogFilePath); 
            }

            var informationLogFile = new FileInfo(informationLogFilePath);
            
            var logFileParser = new LogFileParser(config);
            
            foreach (var logEntry in logFileParser.ParseFile(informationLogFile))
            {
                Console.WriteLine(JsonSerializer.Serialize(logEntry));
            }
        }
    }
}
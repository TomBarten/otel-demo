using System.Text.RegularExpressions;

namespace FileLogs.Otel.Collector
{
    public sealed class LogConfiguration
    {
        public Regex LogLineStartRegex { get; }
        
        public string LogLineStartTypeMatchingGroup { get; }
        
        public string LogLineStartTimestampMatchingGroup { get; }
        
        public Regex LogLineTimestampRegex { get; }
        
        public string LogLineTimestampMatchingGroup { get; }
        
        public Regex LogLineTypeRegex { get; }
        
        public string LogLineTypeMatchingGroup { get; }

        public LogConfiguration(
            Regex logLineStartRegex,
            string logLineStartTypeMatchingGroup,
            string logLineStartTimestampMatchingGroup,
            Regex logLineTimestampRegex,
            string logLineTimestampMatchingGroup,
            Regex logLineTypeRegex,
            string logLineTypeMatchingGroup)
        {
            LogLineStartRegex = logLineStartRegex;
            
            LogLineStartTypeMatchingGroup = logLineStartTypeMatchingGroup;
            LogLineStartTimestampMatchingGroup = logLineStartTimestampMatchingGroup;
            
            LogLineTimestampRegex = logLineTimestampRegex;
            LogLineTimestampMatchingGroup = logLineTimestampMatchingGroup;
            
            LogLineTypeRegex = logLineTypeRegex;
            LogLineTypeMatchingGroup = logLineTypeMatchingGroup;
        }
    }
}
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace FileLogs.Otel.Collector;

public sealed class LogConfiguration
{
    public const string DirectoriesSectionName = "directories";
    
    [JsonPropertyName("files")]
    public required IReadOnlyList<FileInfo> Files { get; init; }
    
    [JsonPropertyName("logLineStartRegex")]
    public required Regex LogLineStartRegex { get; init; }
    
    [JsonPropertyName("logLineStartRegex")]
    public string? LogLineStartTypeMatchingGroup { get; init; }
        
    [JsonPropertyName("logLineStartTimestampMatchingGroup")]
    public string? LogLineStartTimestampMatchingGroup { get; init; }
        
    [JsonPropertyName("logLineTimestampRegex")]
    public Regex? LogLineTimestampRegex { get; init; }

    [JsonPropertyName("logLineTimestampMatchingGroup")]
    public string? LogLineTimestampMatchingGroup { get; init; }
        
    [JsonPropertyName("logLineTypeRegex")]
    public Regex? LogLineTypeRegex { get; init; }
        
    [JsonPropertyName("logLineTypeMatchingGroup")]
    public string? LogLineTypeMatchingGroup { get; init; }
}
using System.Diagnostics;

namespace NumberService;

public sealed class DiagnosticConfig
{
    private static readonly ActivitySource _activitySource;

    internal const string ServiceName = "NumberService";
    internal const string ServiceVersion = "1.0.0";

    public ActivitySource Source => _activitySource;

    static DiagnosticConfig()
    {
        _activitySource = new ActivitySource(ServiceName, ServiceVersion);
    }
}
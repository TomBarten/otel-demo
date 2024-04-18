using System.Diagnostics;
using OrderService.Telemetry.Abstractions;

namespace OrderService.Telemetry;

internal sealed class DiagnosticConfig : IActivitySourceProvider
{
    private static readonly ActivitySource _activitySource;

    internal const string ServiceName = "OrderService";

    internal const string ServiceVersion = "1.0.0";

    public ActivitySource Source => _activitySource;
    
    static DiagnosticConfig()
    {
        _activitySource = new ActivitySource(ServiceName, ServiceVersion);
    }
}
using System.Diagnostics;

namespace OrderService.Telemetry.Abstractions;

public interface IActivitySourceProvider
{
    public ActivitySource Source { get; }
}
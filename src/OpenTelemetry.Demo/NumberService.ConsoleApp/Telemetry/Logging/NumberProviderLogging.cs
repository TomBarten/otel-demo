using Microsoft.Extensions.Logging;

namespace NumberService.Telemetry.Logging;

internal partial class NumberProviderLogging
{
    [LoggerMessage(LogLevel.Information, Message = "Providing number: \"{number}\"")]
    public static partial void LogNumberProvided(ILogger<NumberProvider> logger, int number);
}
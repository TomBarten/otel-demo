using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace NumberService;

internal partial class NumberProviderLogs
{
    [LoggerMessage(LogLevel.Information, Message = "Providing number: \"{number}\"")]
    public static partial void LogNumberProvided(ILogger<NumberProvider> logger, int number);
}

public sealed class NumberProvider
{
    private readonly ILogger<NumberProvider> _logger;
    private readonly DiagnosticConfig _diagnosticConfig;

    public NumberProvider(ILogger<NumberProvider> logger, DiagnosticConfig diagnosticConfig)
    {
        _logger = logger;
        _diagnosticConfig = diagnosticConfig;
    }
    
    public IEnumerable<int> GetNumbers(int amount)
    {
        const int lessThanZeroNumber = -1;
        
        using var activity = _diagnosticConfig.Source.StartActivity();
        
        activity?.SetTag(DiagnosticNames.AmountOfNumbers, amount);

        ValidateAmountInput(ref amount);
        
        using var loggingScope = _logger.BeginScope(new Dictionary<string, object>
        {
            [DiagnosticNames.AmountOfNumbers] = amount
        });
        
        _logger.LogInformation("Providing \"{amount}\" number(s)", amount);

        if (amount <= 0)
        {
            yield return lessThanZeroNumber;
            NumberProviderLogs.LogNumberProvided(_logger, lessThanZeroNumber);
            
            yield break;
        }
        
        activity?.SetTag(DiagnosticNames.AmountIsLessThanOne, false);

        for (var i = 1; i <= amount; i++)
        {
            yield return i;
            NumberProviderLogs.LogNumberProvided(_logger, i);
        }
    }

    private void ValidateAmountInput(ref int amount)
    {
        if (amount > 0)
        {
            _logger.LogDebug("Amount is greater than 0");
            return;
        }
        
        Activity.Current?.SetTag(DiagnosticNames.AmountIsLessThanOne, true);

        _logger.LogDebug("Amount is less than or equal to 0, overwriting value to 0");
            
        amount = 0;
    }
}
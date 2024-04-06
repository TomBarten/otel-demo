using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NumberService.Telemetry.Diagnostics;
using NumberService.Telemetry.Logging;

namespace NumberService;

internal sealed class NumberProvider
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

        var validationFlag = ValidateAmountInput(ref amount);
        
        using var loggingScope = _logger.BeginScope(new Dictionary<string, object>
        {
            [DiagnosticNames.AmountOfNumbers] = amount
        });
        
        _logger.LogInformation("Providing \"{amount}\" number(s)", amount);

        if (!validationFlag)
        {
            yield return lessThanZeroNumber;
            NumberProviderLogging.LogNumberProvided(_logger, lessThanZeroNumber);
            
            yield break;
        }
        
        activity?.SetTag(DiagnosticNames.AmountIsLessThanOne, false);

        for (var i = 1; i <= amount; i++)
        {
            yield return i;
            NumberProviderLogging.LogNumberProvided(_logger, i);
        }
    }

    private bool ValidateAmountInput(ref int amount)
    {
        if (amount > 0)
        {
            _logger.LogDebug("Amount is greater than 0");
            return true;
        }
        
        Activity.Current?.SetTag(DiagnosticNames.AmountIsLessThanOne, true);

        _logger.LogDebug("Amount is less than or equal to 0, overwriting value to 1");
            
        amount = 1;

        return false;
    }
}
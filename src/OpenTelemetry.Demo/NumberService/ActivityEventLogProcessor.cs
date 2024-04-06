using System.Diagnostics;
using System.Text;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace NumberService;

internal sealed class ActivityEventLogProcessor : BaseProcessor<LogRecord>
{
    public override void OnEnd(LogRecord data)
    {
        base.OnEnd(data);
        
        var currentActivity = Activity.Current;

        if (currentActivity is null)
        {
            return;
        }

        string? activityDescription = null;

        // Depends on the IncludeFormattedMessage boolean
        if (!string.IsNullOrWhiteSpace(data.FormattedMessage))
        {
            activityDescription = data.FormattedMessage;
        }
        else if (data.Attributes?.Count > 0)
        {
            // Will be the same as the data.FormattedMessage, but IncludeFormattedMessage == false, so can do it here
            activityDescription = data.Attributes.ToString();

            if (string.IsNullOrWhiteSpace(activityDescription))
            {
                var stringBuilder = new StringBuilder();
            
                var attributeValues = data.Attributes
                    .Select(kvp => $"[{kvp.Key}:{kvp.Value?.ToString() ?? "null"}]");

                stringBuilder.AppendJoin(',', attributeValues);
                activityDescription = stringBuilder.ToString();
                stringBuilder.Clear();
            }
        }

        if (activityDescription is not null)
        {
            currentActivity.AddEvent(new ActivityEvent(activityDescription));
        }
    }
}
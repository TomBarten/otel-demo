using System;
using System.Collections.Generic;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace FileLogs.Otel.Collector;

public sealed class LogEntryTimestampProcessor : BaseProcessor<LogRecord>
{
    public override void OnEnd(LogRecord data)
    {
        data.ForEachScope((scope, logRecord) =>
        {
            if (scope.Scope is not Dictionary<string, object> dictionaryScope)
            {
                throw new InvalidOperationException("Expected scope to be a dictionary");
            }
            
            logRecord.Timestamp = ((DateTimeOffset)dictionaryScope[nameof(LogEntry.Timestamp)]).DateTime;

        }, data);
        
        base.OnEnd(data);
    }
}
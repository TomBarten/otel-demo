using FileLogs.Otel.Collector.Helpers;
using Microsoft.Extensions.Configuration;

namespace FileLogs.Otel.Collector;

internal class Program
{
    public static void Main(string[] args)
    {
        var appConfiguration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();
        
        var otelLogWriter = new LogEntryOtelWriter();
        
        foreach (var config in appConfiguration.GetLogConfigurations())
        {
            var logFileParser = new LogFileParser(config);

            foreach (var file in config.Files)
            {
                otelLogWriter.WriteLogEntries(logFileParser.ParseFile(file));
            }
        }
    }
}
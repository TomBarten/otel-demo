using Microsoft.Extensions.Logging;
using NumberService;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

/*
 * Need this to get reference to ResourceBuilder used in the tracer provider
 * Otherwise logger has different ResourceBuilder, bare bones otel setup, that's why
 */
var otelResourceBuilder = ResourceBuilder.CreateEmpty();

var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .ConfigureResource(resourceBuilder =>
    {
        // Comment to add back default information
        resourceBuilder.Clear();
        
        resourceBuilder.AddService(
            serviceName: DiagnosticConfig.ServiceName,
            serviceVersion: DiagnosticConfig.ServiceVersion);

        /*
         * Capture reference mentioned earlier here
         * Only need to do this, because this is a bare bones demo
         */
        otelResourceBuilder = resourceBuilder;
    })
    .AddSource(DiagnosticConfig.ServiceName)
    .AddConsoleExporter()
    .Build();

using var loggerFactory = LoggerFactory.Create(loggingBuilder =>
{
    loggingBuilder.AddOpenTelemetry(loggerOptions =>
    {
        loggerOptions.IncludeScopes = true;
        
        // TODO, toggle boolean value to see difference in ActivityEventLogProcessor
        loggerOptions.IncludeFormattedMessage = true;

        loggerOptions
            .SetResourceBuilder(otelResourceBuilder)
            .AddProcessor(new ActivityEventLogProcessor())
            .AddConsoleExporter();
    });
});

var numberProvider = new NumberProvider(loggerFactory.CreateLogger<NumberProvider>(), new DiagnosticConfig());

foreach (var number in numberProvider.GetNumbers(int.MinValue))
{
    // Console.WriteLine(number);
}

// foreach (var number in numberProvider.GetNumbers(10))
// {
//     // Console.WriteLine(number);
// }

tracerProvider.Dispose();

using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OrderService;
using OrderService.Telemetry;
using OrderService.Telemetry.Abstractions;
using OrderService.Telemetry.Processors;

Action<ResourceBuilder> configureOtelResource = resourceBuilder => resourceBuilder
    .Clear()
    .AddService(
        serviceName: DiagnosticConfig.ServiceName,
        serviceVersion: DiagnosticConfig.ServiceVersion);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IActivitySourceProvider, DiagnosticConfig>();

builder.Services.AddOpenTelemetry()
    .UseAzureMonitor(configureAzure =>
    {
        // configureAzure.SamplingRatio = 0.4F;
        
        configureAzure.ConnectionString =
            builder.Configuration.GetValue<string>("ApplicationInsights:ConnectionString");
    })
    .ConfigureResource(configureOtelResource)
    .WithTracing(traceBuilder =>
    {
        traceBuilder
            .AddSource(DiagnosticConfig.ServiceName)
            .AddHttpClientInstrumentation(httpClientConfig =>
            {
                httpClientConfig.RecordException = true;
            })
            .AddAspNetCoreInstrumentation(aspnetConfig =>
            {
                aspnetConfig.RecordException = true;
                aspnetConfig.Filter = req =>
                {
                    try
                    {
                        var uri = req.Request.Path.ToUriComponent();
                        
                        return !(uri.Contains("index.html", StringComparison.OrdinalIgnoreCase) 
                                 || uri.Contains("swagger", StringComparison.OrdinalIgnoreCase) 
                                 || uri.Contains("status", StringComparison.OrdinalIgnoreCase));
                    }
                    catch
                    {
                        return true;
                    }
                };
            });
        
        traceBuilder
            .AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("Otlp:Endpoint") 
                                               ?? throw new InvalidOperationException("Missing otel endpoint"));
            })
            .AddConsoleExporter();
    });

builder.Logging.ClearProviders();

builder.Logging.AddOpenTelemetry(telemetryOptions =>
{
    telemetryOptions.IncludeFormattedMessage = true;
    telemetryOptions.IncludeScopes = true;

    var resourceBuilder = ResourceBuilder.CreateEmpty();

    configureOtelResource(resourceBuilder);

    telemetryOptions.SetResourceBuilder(resourceBuilder);
    // telemetryOptions.AddProcessor(new ActivityEventLogProcessor());
    
    telemetryOptions
        .AddOtlpExporter(otlpOptions =>
        {
            otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("Otlp:Endpoint") 
                                           ?? throw new InvalidOperationException("Missing otel endpoint"));
        })
        .AddConsoleExporter();
});

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", ([FromServices] IActivitySourceProvider activitySourceProvider, [FromServices] ILogger<Program> logger) =>
    {
        using var activity = activitySourceProvider.Source.StartActivity("GetWeatherForecast");
        
        var forecast = Enumerable.Range(1, 5).Select(index =>
            {
                var weatherForecast = new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                );
                
                logger.LogInformation("Temperature celsius: {temperatureC}, item: {itemIndex}", weatherForecast.TemperatureC, index);

                return weatherForecast;
            })
            .ToArray();

        activity?.SetTag("forecasts.count", forecast.Length);
        
        return forecast;
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi();

app.Run();

namespace OrderService
{
    record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}
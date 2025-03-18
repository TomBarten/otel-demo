using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Monitor.OpenTelemetry.Exporter;
using Coffee.Api;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<CoffeeDbContext>(options => options.UseInMemoryDatabase("Coffees"));

const string appiConnString =
    "InstrumentationKey=e01c681e-e17a-4b80-84e6-703114f7bf35;IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/;LiveEndpoint=https://westeurope.livediagnostics.monitor.azure.com/;ApplicationId=68f5f2ce-ceed-410c-9638-cfc518f1efd9";

builder.Services.AddOpenTelemetry().UseAzureMonitor()
    .ConfigureResource(resource => resource.AddService("Coffee.Api"))
    .WithMetrics(metrics =>
    {
        // metrics
        //     .AddAspNetCoreInstrumentation()
        //     .AddHttpClientInstrumentation();
    
        metrics
            .AddOtlpExporter()
            .AddAzureMonitorMetricExporter(x => x.ConnectionString = appiConnString);
    })
    .WithTracing(tracing =>
    {
        // tracing
        //     .AddHttpClientInstrumentation()
        //     .AddAspNetCoreInstrumentation(options =>
        //     {
        //         options.Filter = context => !context.Request.Path.StartsWithSegments("/scalar");
        //     });
        //
        tracing
            .AddOtlpExporter()
            .AddAzureMonitorTraceExporter(x => x.ConnectionString = appiConnString);
    });

builder.Logging.AddOpenTelemetry(logging =>
{
    logging
        .AddOtlpExporter()
        .AddAzureMonitorLogExporter(x => x.ConnectionString = appiConnString);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
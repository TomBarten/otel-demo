using OpenTelemetry.Resources;
using OTel.Common.Resource;

namespace OTel.Common
{
    public static class ResourceBuilderProvider
    {
        public static ResourceBuilder CreateResourceBuilder(ResourceConfig config)
        {
            var resourceBuilder = config.AddDefaultAttributes
                ? ResourceBuilder.CreateDefault()
                : ResourceBuilder.CreateEmpty();
                
            return resourceBuilder.AddService(
                    serviceName: config.ServiceName,
                    serviceVersion: config.ServiceVersion);
        }
    }
}
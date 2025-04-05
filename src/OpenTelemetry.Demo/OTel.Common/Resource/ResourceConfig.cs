namespace OTel.Common.Resource
{
    public sealed class ResourceConfig
    {
        public string ServiceName { get; }
        
        public string ServiceVersion { get; }
        
        public bool AddDefaultAttributes { get; }
        
        public ResourceConfig(string serviceName, string serviceVersion, bool addDefaultAttributes = false)
        {
            ServiceName = serviceName;
            ServiceVersion = serviceVersion;
            AddDefaultAttributes = addDefaultAttributes;
        }
    }
}
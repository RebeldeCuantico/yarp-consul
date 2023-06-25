using Consul;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
               .LoadFromMemory(GetRoutes(), await GetClusters());

var app = builder.Build();
app.MapReverseProxy();
app.Run();

async Task<IReadOnlyList<ClusterConfig>> GetClusters()
{
    return new[]
    {
                new ClusterConfig()
                {
                    ClusterId = "cluster1",
                    SessionAffinity = new SessionAffinityConfig { Enabled = true, Policy = "Cookie", AffinityKeyName = ".Yarp.ReverseProxy.Affinity" },
                    Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "api1", new DestinationConfig() { Address = await GetUrlFromServiceDiscoveryByName("API 1") } },
                    }
                }
            };
}

IReadOnlyList<RouteConfig> GetRoutes()
{
    return new[]
           {
                new RouteConfig()
                {
                    RouteId = "route" + Random.Shared.Next(), 
                    ClusterId = "cluster1",
                    Match = new RouteMatch
                    {
                        Path = "{**catch-all}"
                    }
                }
            };
}

static async Task<string> GetUrlFromServiceDiscoveryByName(string name)
{
    var consulClient = new ConsulClient();
    var services = await consulClient.Catalog.Service(name);
    var service = services.Response?.First();

    if (service == null) return string.Empty;

    return $"http://{service.ServiceAddress}:{service.ServicePort}";
}
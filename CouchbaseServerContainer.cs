using Testcontainers.Couchbase;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Configurations;

namespace My.Testcontainers.Couchbase.SyncGateway;
public class CouchbaseServerContainer : DockerContainer
{
    public CouchbaseServerContainer(IContainerConfiguration configuration)
        : base(configuration)
    {

    }

    public string GetConnectionString()
    {
        return new UriBuilder("couchbase", Hostname, GetMappedPublicPort(CouchbaseBuilder.KvPort)).Uri.Authority;
    }
}

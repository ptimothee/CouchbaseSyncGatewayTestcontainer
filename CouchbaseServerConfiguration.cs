using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;

namespace My.Testcontainers.Couchbase.SyncGateway;
public class CouchbaseServerConfiguration : ContainerConfiguration
{
    public CouchbaseServerConfiguration(string? username = null, string? password = null, IEnumerable<Bucket>? buckets = null)
    {
        Username = username;
        Password = password;
        Buckets = buckets;
    }

    public CouchbaseServerConfiguration(IContainerConfiguration resourceConfiguration)
        : base(resourceConfiguration)
    {

    }

    public CouchbaseServerConfiguration(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
        : base(resourceConfiguration)
    {

    }

    public CouchbaseServerConfiguration(CouchbaseServerConfiguration oldValue, CouchbaseServerConfiguration newValue)
        : base(oldValue, newValue)
    {
        Username = BuildConfiguration.Combine(oldValue.Username, newValue.Username);
        Password = BuildConfiguration.Combine(oldValue.Password, newValue.Password);
        Buckets = BuildConfiguration.Combine(oldValue.Buckets, newValue.Buckets);
    }

    public string? Username { get; }

    public string? Password { get; }

    public IEnumerable<Bucket>? Buckets { get; }
}

using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;

namespace My.Testcontainers.Couchbase.SyncGateway;

public class CouchbaseServerBuilder : ContainerBuilder<CouchbaseServerBuilder, CouchbaseServerContainer, CouchbaseServerConfiguration>
{
    public const string DefaultUsername = "Administrator";
    public const string DefaultPassword = "password";

    protected override CouchbaseServerConfiguration DockerResourceConfiguration { get; }

    private CouchbaseServerBuilder(CouchbaseServerConfiguration resourceConfiguration)
        : base(resourceConfiguration)
    {
        DockerResourceConfiguration = resourceConfiguration;
    }

    public CouchbaseServerBuilder()
        : this(new CouchbaseServerConfiguration())
    {
        DockerResourceConfiguration = Init().DockerResourceConfiguration;
    }

    protected override CouchbaseServerBuilder Init()
    {
        return base.Init()
            .WithImage("couchbase:latest")
            .WithPortBinding("8091", "8091")
            .WithPortBinding("8093", "8093")
            .WithPortBinding("11210", "11210")
            .WithPortBinding("11207", "11207")
            .WithHostname("couchbase-server")
            .WithWorkingDirectory("/opt/couchbase")
            .WithUsername(DefaultUsername)
            .WithPassword(DefaultPassword)
            .WithBucket(new Bucket("market"))
            .WithEntrypoint("/entrypoint.sh", "couchbase-server")
            .WithStartupCallback(OnContainerStartingAsync);
    }

    public CouchbaseServerBuilder WithUsername(string username)
    {
        return Merge(DockerResourceConfiguration, new CouchbaseServerConfiguration(username: username))
                .WithEnvironment("COUCHBASE_ADMINISTRATOR_USERNAME", username);
    }

    public CouchbaseServerBuilder WithPassword(string password)
    {
        return Merge(DockerResourceConfiguration, new CouchbaseServerConfiguration(password: password))
                .WithEnvironment("COUCHBASE_ADMINISTRATOR_PASSWORD", password);
    }

    private CouchbaseServerBuilder WithBucket(params Bucket[] bucket)
    {
        return Merge(DockerResourceConfiguration, new CouchbaseServerConfiguration(buckets: bucket));
    }

    public override CouchbaseServerContainer Build()
    {
        return new CouchbaseServerContainer(DockerResourceConfiguration);
    }

    public async Task OnContainerStartingAsync(CouchbaseServerContainer container, CancellationToken cancellationToken)
    {
        await TryInvoke(() => CreateCluster(container, cancellationToken), 3);

        await TryInvoke(() => CreateBuckets(container, cancellationToken), 3);

        await TryInvoke(() => ConfigureSyncGatewayUser(container, cancellationToken), 3);
    }

    public async Task CreateCluster(CouchbaseServerContainer container, CancellationToken cancellationToken)
    {
        var command = new[]
        {
            "/opt/couchbase/bin/couchbase-cli", "cluster-init", "-c", "127.0.0.1",
            "--cluster-username", DockerResourceConfiguration.Username,
            "--cluster-password", DockerResourceConfiguration.Password,
            "--services", "data,index,query,eventing",
            "--cluster-ramsize", "1000",//"$COUCHBASE_RAM_SIZE",
            "--cluster-index-ramsize", "3139", //$COUCHBASE_INDEX_RAM_SIZE \
            "--cluster-eventing-ramsize", "694",//$COUCHBASE_EVENTING_RAM_SIZE \
            "--index-storage-setting", "default",
        };

        var result = await container.ExecAsync(command, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            string errorMessage = (string.IsNullOrEmpty(result.Stderr)) ? result.Stdout : result.Stderr;
            throw new InvalidOperationException(errorMessage);
        }
    }

    public async Task CreateBuckets(CouchbaseServerContainer container, CancellationToken cancellationToken)
    {
        if (DockerResourceConfiguration.Buckets is null)
        {
            return;
        }

        foreach (var bucket in DockerResourceConfiguration.Buckets)
        {
            var command = new[]
            {
                "/opt/couchbase/bin/couchbase-cli", "bucket-create", "-c", "localhost:8091",
                "--username", DockerResourceConfiguration.Username,
                "--password", DockerResourceConfiguration.Password,
                "--bucket", bucket.Name,
                "--bucket-ramsize", bucket.QuotaMiB.ToString(),
                "--bucket-type", "couchbase"
            };

            var result = await container.ExecAsync(command, cancellationToken).ConfigureAwait(false);
            if (result.ExitCode != 0)
            {
                string errorMessage = (string.IsNullOrEmpty(result.Stderr)) ? result.Stdout : result.Stderr;
                throw new InvalidOperationException(errorMessage);
            }
        }
    }

    public async Task ConfigureSyncGatewayUser(CouchbaseServerContainer container, CancellationToken cancellationToken)
    {
        var command = new[]
        {
            "/opt/couchbase/bin/couchbase-cli", "user-manage",
            "--cluster", "http://127.0.0.1",
            "--username", DockerResourceConfiguration.Username,
            "--password", DockerResourceConfiguration.Password,
            "--set",
            "--rbac-username", "demo@example.com", //$COUCHBASE_RBAC_USERNAME
            "--rbac-password", "password",  //$COUCHBASE_RBAC_PASSWORD
            "--roles", "mobile_sync_gateway[*]",
            "--auth-domain", "local"
        };

        var result = await container.ExecAsync(command, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            string errorMessage = (string.IsNullOrEmpty(result.Stderr)) ? result.Stdout : result.Stderr;
            throw new InvalidOperationException(errorMessage);
        }
    }

    private async Task TryInvoke(Func<Task> func, int retryCount)
    {
        try
        {
            await func.Invoke();
        }
        catch (Exception)
        {
            if (retryCount == 0)
            {
                throw;
            }
            await Task.Delay(TimeSpan.FromSeconds(5));
            await TryInvoke(func, --retryCount);
        }
    }

    protected override CouchbaseServerBuilder Clone(IContainerConfiguration resourceConfiguration)
    {
        return Merge(DockerResourceConfiguration, new CouchbaseServerConfiguration(resourceConfiguration));
    }

    protected override CouchbaseServerBuilder Clone(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
    {
        return Merge(DockerResourceConfiguration, new CouchbaseServerConfiguration(resourceConfiguration));
    }

    protected override CouchbaseServerBuilder Merge(CouchbaseServerConfiguration oldValue, CouchbaseServerConfiguration newValue)
    {
        return new CouchbaseServerBuilder(new CouchbaseServerConfiguration(oldValue, newValue));
    }
}

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using My.Testcontainers.Couchbase.SyncGateway;

var network = new NetworkBuilder()
                        .WithDriver(NetworkDriver.Bridge)
                        .Build();

var couchbaseServer = new CouchbaseServerBuilder()
                        .WithNetwork(network)
                        .Build();

var syncGateway = new ContainerBuilder()
                        .WithImage("couchbase/sync-gateway:latest")
                        .WithPortBinding("4984", "4984")
                        .WithResourceMapping(new FileInfo("../../../Config/config.json"), "/etc/sync_gateway/")
                        .WithEntrypoint("/entrypoint.sh", "/etc/sync_gateway/config.json")
                        .WithWorkingDirectory("/docker-syncgateway")
                        .WithHostname("sync-gateway")
                        .DependsOn(network)
                        .DependsOn(couchbaseServer)
                        .Build();

await syncGateway.StartAsync();

Console.WriteLine($"Press any key to stop the containers. ");
Console.ReadKey();

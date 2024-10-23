using System.Text;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Configurations;

namespace My.Testcontainers.Couchbase.SyncGateway;

public static class ContainerExtensions
{
    public static async Task<ExecResult> ExecScriptAsync(this IContainer container, string scriptContent, CancellationToken ct = default)
    {
        var scriptFilePath = string.Join("/", string.Empty, "tmp", Guid.NewGuid().ToString("D"), Path.GetRandomFileName());

        await container.CopyAsync(Encoding.Default.GetBytes(scriptContent), scriptFilePath, Unix.FileMode644, ct)
            .ConfigureAwait(false);

        var command = new string[]
        {
        };

        return await container.ExecAsync(command, ct)
            .ConfigureAwait(false);
    }
}

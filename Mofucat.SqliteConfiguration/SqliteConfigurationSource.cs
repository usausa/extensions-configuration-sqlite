namespace Mofucat.SqliteConfiguration;

using Microsoft.Extensions.Configuration;

internal sealed class SqliteConfigurationSource : IConfigurationSource
{
    public SqliteConfigurationOptions Options { get; set; } = new();

    public IConfigurationProvider Build(IConfigurationBuilder builder) =>
        new SqliteConfigurationProvider(Options);
}

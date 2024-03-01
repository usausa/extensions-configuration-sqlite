namespace Extensions.Configuration.Sqlite;

using Microsoft.Extensions.Configuration;

internal sealed class SqliteConfigurationSource : IConfigurationSource
{
    public string Path { get; set; } = default!;

    public string Table { get; set; } = default!;

    public IConfigurationProvider Build(IConfigurationBuilder builder) =>
        new SqliteConfigurationProvider(Path, Table);
}

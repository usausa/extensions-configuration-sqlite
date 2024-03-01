namespace Extensions.Configuration.Sqlite;

using Microsoft.Extensions.Configuration;

public static class SqliteConfigurationExtensions
{
    public static IConfigurationBuilder AddSqliteConfiguration(
        this IConfigurationBuilder builder,
        string path = "setting.db",
        string table = "setting")
    {
        return builder.Add(new SqliteConfigurationSource
        {
            Path = path,
            Table = table
        });
    }

    public static IConfigurationOperator GetConfigurationOperator(this IConfigurationRoot configuration) =>
        configuration.Providers.OfType<IConfigurationOperator>().First();
}

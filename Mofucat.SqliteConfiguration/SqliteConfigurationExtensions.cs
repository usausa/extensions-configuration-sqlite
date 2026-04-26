namespace Mofucat.SqliteConfiguration;

using Microsoft.Extensions.Configuration;

public static class SqliteConfigurationExtensions
{
    public static IConfigurationBuilder AddSqliteConfiguration(
        this IConfigurationBuilder builder) =>
        builder.AddSqliteConfiguration(new SqliteConfigurationOptions());

    public static IConfigurationBuilder AddSqliteConfiguration(
        this IConfigurationBuilder builder,
        Action<SqliteConfigurationOptions> configure)
    {
        var options = new SqliteConfigurationOptions();
        configure(options);

        return builder.AddSqliteConfiguration(options);
    }

    public static IConfigurationBuilder AddSqliteConfiguration(
        this IConfigurationBuilder builder,
        SqliteConfigurationOptions options)
    {
        return builder.Add(new SqliteConfigurationSource
        {
            Options = options
        });
    }

    public static IConfigurationOperator GetConfigurationOperator(this IConfigurationRoot configuration) =>
        configuration.Providers.OfType<IConfigurationOperator>().First();
}

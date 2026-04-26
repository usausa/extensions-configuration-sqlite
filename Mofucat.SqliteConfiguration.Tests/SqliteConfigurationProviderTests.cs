namespace Mofucat.SqliteConfiguration.Tests;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

using Xunit;

public sealed class SqliteConfigurationProviderTests
{
    [Fact]
    public void GetRequiredConfigurationOperatorWithoutProviderThrows()
    {
        var root = new ConfigurationBuilder()
            .AddInMemoryCollection()
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(root.GetRequiredConfigurationOperator);

        Assert.Contains("No IConfigurationOperator provider", exception.Message, StringComparison.Ordinal);
        Assert.False(root.TryGetConfigurationOperator(out var configurationOperator));
        Assert.Null(configurationOperator);
    }

    [Fact]
    public void NewTableUsesCaseInsensitivePrimaryKey()
    {
        using var database = new TemporarySqliteDatabase();
        using var root = CreateConfiguration(database.Path);

        using var connection = OpenConnection(database.Path);
        InsertRow(connection, "MyKey", "1");

        var exception = Assert.Throws<SqliteException>(() => InsertRow(connection, "mykey", "2"));

        Assert.Contains("UNIQUE", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateAndDeleteAsyncUpdatesDatabaseAndConfiguration()
    {
        using var database = new TemporarySqliteDatabase();
        using var root = CreateConfiguration(database.Path);
        var configurationOperator = root.GetRequiredConfigurationOperator();

        await configurationOperator.UpdateAsync("Dynamic:Value1", "Updated").ConfigureAwait(false);

        Assert.Equal("Updated", root["dynamic:value1"]);
        Assert.Equal("Updated", GetValue(database.Path, "Dynamic:Value1"));

        await configurationOperator.DeleteAsync("Dynamic:Value1").ConfigureAwait(false);

        Assert.Null(root["Dynamic:Value1"]);
        Assert.Null(GetValue(database.Path, "Dynamic:Value1"));
    }

    [Fact]
    public async Task BulkUpdateAndDeleteAsyncUpdatesDatabaseAndConfiguration()
    {
        using var database = new TemporarySqliteDatabase();
        using var root = CreateConfiguration(database.Path);
        var configurationOperator = root.GetRequiredConfigurationOperator();

        await configurationOperator.BulkUpdateAsync(
            new KeyValuePair<string, object?>("Feature:A", true),
            new KeyValuePair<string, object?>("Feature:B", false)).ConfigureAwait(false);

        Assert.Equal("True", root["Feature:A"]);
        Assert.Equal("False", root["Feature:B"]);

        await configurationOperator.BulkDeleteAsync("Feature:A", "Feature:B").ConfigureAwait(false);

        Assert.Null(root["Feature:A"]);
        Assert.Null(root["Feature:B"]);
    }

    [Fact]
    public async Task ReloadAsyncReflectsExternalDatabaseChanges()
    {
        using var database = new TemporarySqliteDatabase();
        using var root = CreateConfiguration(database.Path);
        var configurationOperator = root.GetRequiredConfigurationOperator();

        using (var connection = OpenConnection(database.Path))
        {
            InsertRow(connection, "External:Value", "42");
        }

        Assert.Null(root["External:Value"]);

        await configurationOperator.ReloadAsync().ConfigureAwait(false);

        Assert.Equal("42", root["External:Value"]);
    }

    [Fact]
    public void SeedDataIsAppliedOnlyWhenTheTableIsCreated()
    {
        using var database = new TemporarySqliteDatabase();

        using var firstRoot = CreateConfiguration(database.Path, options =>
        {
            options.Data.Add(new KeyValuePair<string, string?>("Seed:Value", "First"));
        });

        Assert.Equal("First", firstRoot["Seed:Value"]);

        using var secondRoot = CreateConfiguration(database.Path, options =>
        {
            options.Data.Add(new KeyValuePair<string, string?>("Seed:Value", "Second"));
        });

        Assert.Equal("First", secondRoot["Seed:Value"]);
    }

    private static ConfigurationRoot CreateConfiguration(string path, Action<SqliteConfigurationOptions>? configure = null) =>
        (ConfigurationRoot)new ConfigurationBuilder()
            .AddSqliteConfiguration(options =>
            {
                options.Path = path;
                options.Table = "setting";
                configure?.Invoke(options);
            })
            .Build();

    private static object? GetValue(string path, string key)
    {
        using var connection = OpenConnection(path);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Value FROM setting WHERE Key = @Key";
        AddParameter(command, "Key", key);

        return command.ExecuteScalar();
    }

    private static void InsertRow(SqliteConnection connection, string key, string? value)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO setting(Key, Value) VALUES (@Key, @Value)";
        AddParameter(command, "Key", key);
        AddParameter(command, "Value", value);
        _ = command.ExecuteNonQuery();
    }

    private static SqliteConnection OpenConnection(string path)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            Cache = SqliteCacheMode.Shared,
            DataSource = path,
            Pooling = true
        };

        var connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();
        return connection;
    }

    private static void AddParameter(SqliteCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        _ = command.Parameters.Add(parameter);
    }

    private sealed class TemporarySqliteDatabase : IDisposable
    {
        public TemporarySqliteDatabase()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        }

        public string Path { get; }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();

            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}

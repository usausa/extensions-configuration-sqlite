namespace Extensions.Configuration.Sqlite;

using System.Data.Common;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

#pragma warning disable CA2100
internal sealed class SqliteConfigurationProvider : ConfigurationProvider, IConfigurationOperator
{
    private readonly string connectionString;

    private readonly string selectSql;
    private readonly string updateSql;

    public SqliteConfigurationProvider(string path, string table)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Pooling = true,
            Cache = SqliteCacheMode.Shared
        };
        connectionString = builder.ConnectionString;

        selectSql = $"SELECT Key, Value FROM {table} ORDER BY Key";
        updateSql = $"REPLACE INTO {table}(Key, Value) VALUES (@Key, @Value)";

        using var con = new SqliteConnection(connectionString);
        con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = $"CREATE TABLE IF NOT EXISTS {table} (Key TEXT NOT NULL PRIMARY KEY, VALUE TEXT)";
        cmd.ExecuteNonQuery();
    }

    public override void Load()
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        using var con = new SqliteConnection(connectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = selectSql;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            data[reader.GetString(0)] = reader.IsDBNull(1) ? null : reader.GetString(1);
        }

        Data = data;
    }

    public async ValueTask UpdateAsync(string key, object? value)
    {
#pragma warning disable CA2007
        await using var con = new SqliteConnection(connectionString);
#pragma warning restore CA2007
        await con.OpenAsync().ConfigureAwait(false);

        await UpdateAsync(con, key, value?.ToString()).ConfigureAwait(false);

        OnReload();
    }

    public ValueTask BulkUpdateAsync(params KeyValuePair<string, object?>[] source) =>
        BulkUpdateAsync((IEnumerable<KeyValuePair<string, object?>>)source);

    public async ValueTask BulkUpdateAsync(IEnumerable<KeyValuePair<string, object?>> source)
    {
#pragma warning disable CA2007
        await using var con = new SqliteConnection(connectionString);
#pragma warning restore CA2007
        await con.OpenAsync().ConfigureAwait(false);

        foreach (var pair in source)
        {
            await UpdateAsync(con, pair.Key, pair.Value?.ToString()).ConfigureAwait(false);
        }

        OnReload();
    }

    private async ValueTask UpdateAsync(DbConnection con, string key, string? value)
    {
#pragma warning disable CA2007
        await using var cmd = con.CreateCommand();
#pragma warning restore CA2007
        cmd.CommandText = updateSql;

        var p1 = cmd.CreateParameter();
        p1.ParameterName = "Key";
        p1.Value = key;
        cmd.Parameters.Add(p1);

        var p2 = cmd.CreateParameter();
        p2.ParameterName = "Value";
        p2.Value = (object?)value ?? DBNull.Value;
        cmd.Parameters.Add(p2);

        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

        Data[key] = value;
    }
}
#pragma warning restore CA2100

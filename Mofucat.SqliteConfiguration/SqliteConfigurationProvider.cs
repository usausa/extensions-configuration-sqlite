namespace Mofucat.SqliteConfiguration;

using System.Data.Common;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

#pragma warning disable CA2100
internal sealed class SqliteConfigurationProvider : ConfigurationProvider, IConfigurationOperator
{
#if NET9_0_OR_GREATER
    private readonly Lock sync = new();
#else
    private readonly object sync = new();
#endif

    private readonly SqliteConfigurationOptions options;
    private readonly string connectionString;

    private readonly string quotedTableName;
    private readonly string selectSql;
    private readonly string updateSql;
    private readonly string deleteSql;

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    public SqliteConfigurationProvider(SqliteConfigurationOptions options)
    {
        this.options = options;
        quotedTableName = QuoteIdentifier(this.options.Table);

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = this.options.Path,
            Pooling = true,
            Cache = SqliteCacheMode.Shared
        };
        connectionString = builder.ConnectionString;

        selectSql = $"SELECT Key, Value FROM {quotedTableName} ORDER BY Key";
        updateSql = $"REPLACE INTO {quotedTableName} (Key, Value) VALUES (@Key, @Value)";
        deleteSql = $"DELETE FROM {quotedTableName} WHERE Key = @Key";

        InitializeDatabase();
    }

    private static string QuoteIdentifier(string value) =>
        $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    //--------------------------------------------------------------------------------
    // Override
    //--------------------------------------------------------------------------------

    public override void Load()
    {
        var data = LoadData();

        lock (sync)
        {
            Data = data;
        }
    }

    //--------------------------------------------------------------------------------
    // Operator
    //--------------------------------------------------------------------------------

    public async ValueTask UpdateAsync(string key, object? value)
    {
        var stringValue = value?.ToString();

#pragma warning disable CA2007
        await using var con = new SqliteConnection(connectionString);
#pragma warning restore CA2007
        await con.OpenAsync().ConfigureAwait(false);

        await ExecuteUpdateAsync(con, null, key, stringValue).ConfigureAwait(false);

        lock (sync)
        {
            Data[key] = stringValue;
        }

        OnReload();
    }

    public ValueTask BulkUpdateAsync(params KeyValuePair<string, object?>[] source) =>
        BulkUpdateAsync((IEnumerable<KeyValuePair<string, object?>>)source);

    public async ValueTask BulkUpdateAsync(IEnumerable<KeyValuePair<string, object?>> source)
    {
        // TODO remove ToArray
        var entries = source.Select(static pair => new KeyValuePair<string, string?>(pair.Key, pair.Value?.ToString())).ToArray();
        if (entries.Length == 0)
        {
            return;
        }

#pragma warning disable CA2007
        await using var con = new SqliteConnection(connectionString);
#pragma warning restore CA2007
        await con.OpenAsync().ConfigureAwait(false);
#pragma warning disable CA2007
        await using var tx = await con.BeginTransactionAsync().ConfigureAwait(false);
#pragma warning restore CA2007

        foreach (var pair in entries)
        {
            await ExecuteUpdateAsync(con, tx, pair.Key, pair.Value).ConfigureAwait(false);
        }

        await tx.CommitAsync().ConfigureAwait(false);

        lock (sync)
        {
            foreach (var pair in entries)
            {
                Data[pair.Key] = pair.Value;
            }
        }

        OnReload();
    }

    public async ValueTask DeleteAsync(string key)
    {
#pragma warning disable CA2007
        await using var con = new SqliteConnection(connectionString);
#pragma warning restore CA2007
        await con.OpenAsync().ConfigureAwait(false);

        await ExecuteDeleteAsync(con, null, key).ConfigureAwait(false);

        lock (sync)
        {
            Data.Remove(key);
        }

        OnReload();
    }

    public ValueTask BulkDeleteAsync(params string[] keys) =>
        BulkDeleteAsync((IEnumerable<string>)keys);

    public async ValueTask BulkDeleteAsync(IEnumerable<string> keys)
    {
        // TODO remove ToArray
        var keyArray = keys.ToArray();
        if (keyArray.Length == 0)
        {
            return;
        }

#pragma warning disable CA2007
        await using var con = new SqliteConnection(connectionString);
#pragma warning restore CA2007
        await con.OpenAsync().ConfigureAwait(false);
#pragma warning disable CA2007
        await using var tx = await con.BeginTransactionAsync().ConfigureAwait(false);
#pragma warning restore CA2007

        foreach (var key in keyArray)
        {
            await ExecuteDeleteAsync(con, tx, key).ConfigureAwait(false);
        }

        await tx.CommitAsync().ConfigureAwait(false);

        lock (sync)
        {
            foreach (var key in keyArray)
            {
                Data.Remove(key);
            }
        }

        OnReload();
    }

    public async ValueTask ReloadAsync()
    {
        var data = await LoadDataAsync().ConfigureAwait(false);

        lock (sync)
        {
            Data = data;
        }

        OnReload();
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        _ = command.Parameters.Add(parameter);
    }

    private void InitializeDatabase()
    {
        using var con = new SqliteConnection(connectionString);
        con.Open();

        var tableExists = TableExists(con);

        using (var cmd = con.CreateCommand())
        {
            cmd.CommandText = $"CREATE TABLE IF NOT EXISTS {quotedTableName} (Key TEXT NOT NULL COLLATE NOCASE PRIMARY KEY, Value TEXT)";
            _ = cmd.ExecuteNonQuery();
        }

        if (!tableExists && (options.Data.Count > 0))
        {
            using var tx = con.BeginTransaction();
            foreach (var pair in options.Data)
            {
                ExecuteUpdate(con, tx, pair.Key, pair.Value);
            }

            tx.Commit();
        }
    }

    private bool TableExists(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT EXISTS(SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = @Name)";
        AddParameter(cmd, "Name", options.Table);

        return cmd.ExecuteScalar() is 1L;
    }

    private Dictionary<string, string?> LoadData()
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

        return data;
    }

    private async ValueTask<Dictionary<string, string?>> LoadDataAsync()
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

#pragma warning disable CA2007
        await using var con = new SqliteConnection(connectionString);
#pragma warning restore CA2007
        await con.OpenAsync().ConfigureAwait(false);

#pragma warning disable CA2007
        await using var cmd = con.CreateCommand();
#pragma warning restore CA2007
        cmd.CommandText = selectSql;

#pragma warning disable CA2007
        await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
#pragma warning restore CA2007
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            data[reader.GetString(0)] = await reader.IsDBNullAsync(1).ConfigureAwait(false) ? null : reader.GetString(1);
        }

        return data;
    }

    private void ExecuteUpdate(DbConnection connection, DbTransaction? transaction, string key, string? value)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = updateSql;
        cmd.Transaction = transaction;
        AddParameter(cmd, "Key", key);
        AddParameter(cmd, "Value", value);
        _ = cmd.ExecuteNonQuery();
    }

    private async ValueTask ExecuteUpdateAsync(DbConnection connection, DbTransaction? transaction, string key, string? value)
    {
#pragma warning disable CA2007
        await using var cmd = connection.CreateCommand();
#pragma warning restore CA2007
        cmd.CommandText = updateSql;
        cmd.Transaction = transaction;
        AddParameter(cmd, "Key", key);
        AddParameter(cmd, "Value", value);
        _ = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    private async ValueTask ExecuteDeleteAsync(DbConnection connection, DbTransaction? transaction, string key)
    {
#pragma warning disable CA2007
        await using var cmd = connection.CreateCommand();
#pragma warning restore CA2007
        cmd.CommandText = deleteSql;
        cmd.Transaction = transaction;
        AddParameter(cmd, "Key", key);
        _ = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }
}
#pragma warning restore CA2100

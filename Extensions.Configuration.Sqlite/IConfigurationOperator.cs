namespace Extensions.Configuration.Sqlite;

public interface IConfigurationOperator
{
    ValueTask UpdateAsync(string key, object? value);

    ValueTask BulkUpdateAsync(params KeyValuePair<string, object?>[] source);

    ValueTask BulkUpdateAsync(IEnumerable<KeyValuePair<string, object?>> source);
}

namespace Mofucat.SqliteConfiguration;

public interface IConfigurationOperator
{
    ValueTask UpdateAsync(string key, string? value);

    ValueTask UpdateAsync(string key, object? value);

    ValueTask BulkUpdateAsync(params KeyValuePair<string, object?>[] source);

    ValueTask BulkUpdateAsync(IEnumerable<KeyValuePair<string, object?>> source);

    ValueTask DeleteAsync(string key);

    ValueTask BulkDeleteAsync(params string[] keys);

    ValueTask BulkDeleteAsync(IEnumerable<string> keys);

    ValueTask ReloadAsync();
}

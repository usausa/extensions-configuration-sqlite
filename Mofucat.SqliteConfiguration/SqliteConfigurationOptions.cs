namespace Mofucat.SqliteConfiguration;

public sealed class SqliteConfigurationOptions
{
    public string Path { get; set; } = "setting.db";

    public string Table { get; set; } = "setting";

    public ICollection<KeyValuePair<string, string?>> Data { get; } = [];
}

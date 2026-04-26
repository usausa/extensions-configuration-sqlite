namespace Mofucat.SqliteConfiguration;

public sealed class SqliteConfigurationOptions
{
    public string Path { get; set; } = "setting.db";

    public ICollection<KeyValuePair<string, string?>> SeedData { get; } = [];

    public string Table { get; set; } = "setting";

    internal SqliteConfigurationOptions Clone()
    {
        var clone = new SqliteConfigurationOptions
        {
            Path = Path,
            Table = Table
        };

        foreach (var pair in SeedData)
        {
            clone.SeedData.Add(pair);
        }

        return clone;
    }

    internal void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Path);
        ArgumentException.ThrowIfNullOrWhiteSpace(Table);

        if (!IsValidTableName(Table))
        {
            throw new ArgumentException(
                "Table must start with a letter or underscore and contain only ASCII letters, digits, or underscores.",
                nameof(Table));
        }
    }

    private static bool IsValidTableName(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        var first = value[0];
        if (!(char.IsAsciiLetter(first) || first == '_'))
        {
            return false;
        }

        for (var i = 1; i < value.Length; i++)
        {
            var current = value[i];
            if (!(char.IsAsciiLetterOrDigit(current) || current == '_'))
            {
                return false;
            }
        }

        return true;
    }
}

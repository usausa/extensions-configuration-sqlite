# Mofucat.SqliteConfiguration

[![NuGet](https://img.shields.io/nuget/v/Mofucat.SqliteConfiguration.svg)](https://www.nuget.org/packages/Mofucat.SqliteConfiguration)

.NET configuration provider using SQLite.

## Usage

### Register

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddSqliteConfiguration(options =>
{
    options.Path = "setting.db";
    options.Table = "setting";
});
```

### Update configuration

```csharp
var op = ((IConfigurationRoot)builder.Configuration).GetConfigurationOperator();

// Update
await op.UpdateAsync("Key1", "Value1");

// Delete
await op.DeleteAsync("Key2");

// Reload from the database
await op.ReloadAsync();
```

`SqliteConfigurationOptions` exposes `Path` and `Table`.

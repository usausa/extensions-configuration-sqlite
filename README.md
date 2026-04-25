# Mofucat.SqliteConfiguration

[![NuGet](https://img.shields.io/nuget/v/Mofucat.SqliteConfiguration.svg)](https://www.nuget.org/packages/Mofucat.SqliteConfiguration)

A .NET configuration provider that stores key-value settings in a SQLite database.

## Features

- Integrates with `Microsoft.Extensions.Configuration`
- Stores configuration in a SQLite database file
- Supports runtime updates via `IConfigurationOperator`
- Targets .NET 8, .NET 9, and .NET 10

## Installation

```
dotnet add package Mofucat.SqliteConfiguration
```

## Usage

### Register the provider

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddSqliteConfiguration(
    path: "setting.db",
    table: "setting");
```

### Read configuration

```csharp
var value = configuration["MyKey"];
```

### Update configuration at runtime

```csharp
var op = ((IConfigurationRoot)configuration).GetConfigurationOperator();

// Update a single value
await op.UpdateAsync("MyKey", "NewValue");

// Update multiple values at once
await op.BulkUpdateAsync(
    new KeyValuePair<string, object?>("Key1", "Value1"),
    new KeyValuePair<string, object?>("Key2", "Value2"));
```

## License

This project is licensed under the MIT License.

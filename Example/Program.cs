using Example;

using Extensions.Configuration.Sqlite;

using Microsoft.FeatureManagement;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddSqliteConfiguration();
builder.Services.AddSingleton(p => ((IConfigurationRoot)p.GetRequiredService<IConfiguration>()).GetConfigurationOperator());

// Add services to the container.
builder.Services.AddFeatureManagement();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<DynamicSetting>(builder.Configuration.GetSection("Dynamic"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

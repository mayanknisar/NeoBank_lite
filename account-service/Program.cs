using StackExchange.Redis;
using AccountService.Data;
using AccountService.Caching;
using AccountService.Grpc;
using Microsoft.EntityFrameworkCore;
using AccountService.Services;

var builder = WebApplication.CreateBuilder(args);

// Two Kestrel endpoints: 5001 for REST (HTTP/1.1, hit by the API Gateway
// and React dev proxy), 6001 for gRPC (HTTP/2, hit by Transaction Service
// and Loan Service for synchronous debit/credit/standing checks).
// builder.WebHost.ConfigureKestrel(options =>
// {
//     options.ListenAnyIP(5001, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1);
//     options.ListenAnyIP(6001, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2);
// });

builder.Services.AddGrpc();
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
    });
builder.Services.AddSwaggerGen();           // Generates the Swagger JSON schema

builder.Services.AddDbContext<SqliteDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Sqlite")
        ?? throw new InvalidOperationException("Sqlite connection string not configured")));

// builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
// ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379"));

builder.Services.AddScoped<AccountRepository>();
// builder.Services.AddScoped<AccountCacheService>();
builder.Services.AddScoped<IDatabaseService, SqliteDbService>();
// builder.Services.AddScoped<IDatabaseService, PostgresDbService>();  //uncomment when using Postgres

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();   // Serves the documentation as a JSON endpoint
    app.UseSwaggerUI(); // Serves the interactive web UI webpage
}
app.MapGrpcService<AccountGrpcServiceImpl>();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

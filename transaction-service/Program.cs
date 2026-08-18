using TransactionService.Data;
using TransactionService.Grpc;
using TransactionService.Outbox;

var builder = WebApplication.CreateBuilder(args);

// REST only — this service is a gRPC *client* (calls Account Service), not
// a server, so there's no second Kestrel endpoint to configure.
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5002, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1);
});

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddScoped<TransactionRepository>();
builder.Services.AddSingleton<AccountGrpcClient>();
builder.Services.AddHostedService<OutboxPublisher>();

var app = builder.Build();

app.UseCors("Frontend");
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

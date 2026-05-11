using CozyTts.Api.Health;
using CozyTts.Api.Middleware;
using CozyTts.Application;
using CozyTts.Infrastructure;
using CozyTts.Infrastructure.Persistence;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

var connectionString = builder.Configuration["POSTGRES_CONNECTION"]
    ?? builder.Configuration.GetConnectionString("Postgres")
    ?? "Host=localhost;Port=5432;Database=cozytts;Username=cozytts;Password=cozytts";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "http://127.0.0.1:3000",
                "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));

builder.Services.AddHangfireServer(options =>
{
    options.ServerName = $"cozy-tts-{Environment.MachineName}";
    options.WorkerCount = Math.Max(1, Environment.ProcessorCount / 2);
});

builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("postgres");

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("frontend");
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire");

app.MapControllers();
app.MapHealthChecks("/health");

if (app.Configuration.GetValue("APPLY_MIGRATIONS", true))
{
    await ApplyMigrationsAsync(app);
}

app.Run();

static async Task ApplyMigrationsAsync(WebApplication app)
{
    const int maxAttempts = 12;

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CozyTtsDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigrator");

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied.");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex, "Database is not ready. Migration attempt {Attempt}/{MaxAttempts}.", attempt, maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }

    await db.Database.MigrateAsync();
}

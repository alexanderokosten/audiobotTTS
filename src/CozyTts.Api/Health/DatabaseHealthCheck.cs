using CozyTts.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CozyTts.Api.Health;

public sealed class DatabaseHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CozyTtsDbContext>();
            var canConnect = await db.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy("PostgreSQL is reachable.")
                : HealthCheckResult.Unhealthy("PostgreSQL is not reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL healthcheck failed.", ex);
        }
    }
}

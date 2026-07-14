using Ego.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ego.Api;

public class DatabaseHealthCheck(IServiceScopeFactory serviceScopeFactory) : IHealthCheck
{
    private static readonly TimeSpan DatabaseCheckTimeout = TimeSpan.FromSeconds(5);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EgoDbContext>();
        using var timeoutCts = new CancellationTokenSource(DatabaseCheckTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(linkedCts.Token);

            return canConnect
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Database connection check failed.");
        }
        catch (OperationCanceledException exception) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("Database connection check timed out.", exception);
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Database is unavailable.", exception);
        }
    }
}

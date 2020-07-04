using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Chaucer.Backend.Health
{
    public class Heartbeat :
        IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
            => Task.FromResult(HealthCheckResult.Healthy());
    }
}
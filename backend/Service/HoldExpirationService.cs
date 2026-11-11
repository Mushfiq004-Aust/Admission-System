using api.Helper;
using api.Interfaces;

namespace api.Service
{
    // A background worker that wakes up every few seconds and flips stale HELD rows to EXPIRED,
    // so the data stays tidy and the history is accurate.
    // It is NOT what protects capacity: every seat count, confirm and status read
    // also checks ExpiresAt against the clock directly.
    public class HoldExpirationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly HoldOptions _options;
        private readonly ILogger<HoldExpirationService> _logger;

        public HoldExpirationService(IServiceScopeFactory scopeFactory, HoldOptions options, ILogger<HoldExpirationService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.SweepIntervalSeconds));
                do
                {
                    await SweepAsync(stoppingToken);
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException)
            {
                // normal shutdown
            }
        }

        private async Task SweepAsync(CancellationToken ct)
        {
            try
            {
                // A background service lives for the whole app, but a repository lives for one "unit of work",
                // so we open a fresh scope each time we sweep.
                using var scope = _scopeFactory.CreateScope();
                var holdRepo = scope.ServiceProvider.GetRequiredService<IHoldRepository>();

                var expired = await holdRepo.ExpireAllStaleHoldsAsync(DateTime.UtcNow, ct);

                if (expired > 0)
                {
                    _logger.LogInformation("Expired {Count} stale hold(s).", expired);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Never let a temporary database error kill the worker.
                _logger.LogWarning(ex, "Hold expiration sweep failed; will retry.");
            }
        }
    }
}

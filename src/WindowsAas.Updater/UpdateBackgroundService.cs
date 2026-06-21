using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WindowsAas.Updater;

/// <summary>
/// Periodically polls the update feed. Records availability in <see cref="UpdateState"/>
/// for the UI and, on the <see cref="UpdateChannel.Automatic"/> channel, applies the
/// update immediately.
/// </summary>
public sealed class UpdateBackgroundService(
  IUpdateChecker checker,
  UpdateOrchestrator orchestrator,
  UpdateState state,
  IOptions<UpdaterOptions> options,
  ILogger<UpdateBackgroundService> logger) : BackgroundService
{
  private readonly UpdaterOptions _options = options.Value;

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    using var timer = new PeriodicTimer(_options.CheckInterval);
    do
    {
      await CheckOnceAsync(stoppingToken);
    }
    while (await timer.WaitForNextTickAsync(stoppingToken));
  }

  private async Task CheckOnceAsync(CancellationToken ct)
  {
    try
    {
      var update = await checker.GetAvailableUpdateAsync(ct);
      state.LastCheckedUtc = DateTimeOffset.UtcNow;
      state.Available = update;

      if (update is not null)
      {
        logger.LogInformation("Update available: {Version}.", update.Version);
        if (_options.Channel == UpdateChannel.Automatic)
        {
          await orchestrator.ApplyAsync(update, ct);
        }
      }
    }
    catch (OperationCanceledException)
    {
      // Shutting down.
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Update check failed.");
    }
  }
}

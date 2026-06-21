using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WindowsAas.Abstractions.Plugins;
using WindowsAas.Aas;
using WindowsAas.Mqtt;
using WindowsAas.PluginHost;

namespace WindowsAas.Service.Bridge;

/// <summary>
/// Coordinates startup of the host ↔ AAS bridge: connects to MQTT, initializes the
/// plugin host, registers the host shell and plugin submodels in the AAS environment,
/// and routes inbound writes (AAS → host) arriving on <c>/set</c> topics to the
/// owning plugin, reporting the actual value back to the AAS.
/// </summary>
public sealed class BridgeOrchestrator(
  IMqttBus bus,
  MqttTopics topics,
  IPluginHost pluginHost,
  IAasEnvironmentClient aas,
  IPluginValueReporter reporter,
  ILogger<BridgeOrchestrator> logger) : BackgroundService
{
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    try
    {
      await bus.ConnectAsync(stoppingToken);
      bus.MessageReceived += OnMessageAsync;
      await bus.SubscribeAsync(topics.AllSets(), stoppingToken);

      await pluginHost.InitializeAsync(stoppingToken);

      await aas.EnsureHostShellAsync(stoppingToken);
      foreach (var submodel in await pluginHost.GetSubmodelsAsync(stoppingToken))
      {
        await aas.PutSubmodelAsync(submodel, stoppingToken);
      }

      logger.LogInformation("Bridge started; listening for AAS writes.");
    }
    catch (OperationCanceledException)
    {
      // Normal shutdown.
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Bridge failed to start.");
    }
  }

  private async Task OnMessageAsync(MqttMessage message, CancellationToken ct)
  {
    if (!topics.TryParseSet(message.Topic, out var submodelId, out var idShortPath))
    {
      return;
    }

    var request = new PropertyWriteRequest(submodelId, idShortPath, message.Payload);
    var result = await pluginHost.WriteAsync(request, ct);

    if (!result.Success)
    {
      logger.LogWarning("Write to {Submodel}/{Path} rejected: {Error}",
        submodelId, idShortPath, result.Error);
      return;
    }

    if (result.ActualValue is { } actual)
    {
      await reporter.ReportAsync(submodelId, idShortPath, actual, ct);
    }
  }

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    bus.MessageReceived -= OnMessageAsync;
    await base.StopAsync(cancellationToken);
  }
}

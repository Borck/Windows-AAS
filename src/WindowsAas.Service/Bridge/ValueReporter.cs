using WindowsAas.Aas;
using WindowsAas.Mqtt;
using WindowsAas.PluginHost;

namespace WindowsAas.Service.Bridge;

/// <summary>
/// Host-side <see cref="IPluginValueReporter"/>: when a plugin reports a changed
/// value it is both published to MQTT (host → AAS telemetry topic) and written back
/// into the AAS environment so the shell stays queryable (the ingestion bridge).
/// </summary>
public sealed class ValueReporter(IMqttBus bus, MqttTopics topics, IAasEnvironmentClient aas)
  : IPluginValueReporter
{
  public async ValueTask ReportAsync(
    string submodelId,
    string idShortPath,
    string value,
    CancellationToken ct = default)
  {
    await bus.PublishAsync(topics.Value(submodelId, idShortPath), value, retain: true, ct);
    await aas.SetElementValueAsync(submodelId, idShortPath, value, ct);
  }
}

namespace WindowsAas.PluginHost;

/// <summary>
/// Sink the host provides so plugins can push changed values toward the AAS. The
/// service implementation publishes to MQTT and reflects the value into the AAS
/// environment. Kept as a separate abstraction so the plugin host has no direct
/// dependency on the MQTT/AAS layers.
/// </summary>
public interface IPluginValueReporter
{
  ValueTask ReportAsync(string submodelId, string idShortPath, string value, CancellationToken ct = default);
}

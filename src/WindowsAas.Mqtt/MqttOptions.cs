namespace WindowsAas.Mqtt;

/// <summary>
/// Settings for the connection to the MQTT broker the AAS environment also uses.
/// Bound from the encrypted configuration store.
/// </summary>
public sealed class MqttOptions
{
  public const string SectionName = "Mqtt";

  public string Host { get; set; } = "localhost";

  public int Port { get; set; } = 8883;

  /// <summary>Use TLS for the broker connection (recommended / default).</summary>
  public bool UseTls { get; set; } = true;

  public string? Username { get; set; }

  public string? Password { get; set; }

  /// <summary>Client id used when connecting; defaults to the machine name.</summary>
  public string ClientId { get; set; } = $"windows-aas-{Environment.MachineName}";

  /// <summary>Root topic prefix for all Windows-AAS traffic.</summary>
  public string TopicPrefix { get; set; } = "windows-aas";
}

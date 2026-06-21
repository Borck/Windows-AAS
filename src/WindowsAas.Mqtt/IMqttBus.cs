namespace WindowsAas.Mqtt;

/// <summary>A message received from the broker.</summary>
/// <param name="Topic">Full topic the message arrived on.</param>
/// <param name="Payload">UTF-8 decoded payload.</param>
public readonly record struct MqttMessage(string Topic, string Payload);

/// <summary>
/// Thin abstraction over the MQTT broker connection used by the host ↔ AAS bridge.
/// </summary>
public interface IMqttBus
{
  /// <summary>Raised for every message on a subscribed topic.</summary>
  event Func<MqttMessage, CancellationToken, Task>? MessageReceived;

  /// <summary>Connects to the broker (idempotent).</summary>
  Task ConnectAsync(CancellationToken ct = default);

  /// <summary>Subscribes to a topic filter (may contain wildcards).</summary>
  Task SubscribeAsync(string topicFilter, CancellationToken ct = default);

  /// <summary>Publishes a retained or transient message.</summary>
  Task PublishAsync(string topic, string payload, bool retain = true, CancellationToken ct = default);
}

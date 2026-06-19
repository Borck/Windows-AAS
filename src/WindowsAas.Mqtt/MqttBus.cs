using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace WindowsAas.Mqtt;

/// <summary>
/// <see cref="IMqttBus"/> backed by MQTTnet. Connects (optionally over TLS) with
/// credentials from <see cref="MqttOptions"/> and dispatches inbound messages to
/// the <see cref="MessageReceived"/> handler.
/// </summary>
public sealed class MqttBus : IMqttBus, IAsyncDisposable
{
  private readonly MqttOptions _options;
  private readonly ILogger<MqttBus> _logger;
  private readonly IMqttClient _client;
  private readonly MqttClientOptions _clientOptions;

  public MqttBus(IOptions<MqttOptions> options, ILogger<MqttBus> logger)
  {
    _options = options.Value;
    _logger = logger;
    _client = new MqttFactory().CreateMqttClient();

    var builder = new MqttClientOptionsBuilder()
      .WithClientId(_options.ClientId)
      .WithTcpServer(_options.Host, _options.Port)
      .WithCleanSession();

    if (_options.UseTls)
    {
      builder = builder.WithTlsOptions(o => o.UseTls());
    }

    if (!string.IsNullOrEmpty(_options.Username))
    {
      builder = builder.WithCredentials(_options.Username, _options.Password ?? string.Empty);
    }

    _clientOptions = builder.Build();
    _client.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
  }

  public event Func<MqttMessage, CancellationToken, Task>? MessageReceived;

  public async Task ConnectAsync(CancellationToken ct = default)
  {
    if (_client.IsConnected)
    {
      return;
    }

    await _client.ConnectAsync(_clientOptions, ct);
    _logger.LogInformation("Connected to MQTT broker {Host}:{Port} (TLS={Tls}).",
      _options.Host, _options.Port, _options.UseTls);
  }

  public Task SubscribeAsync(string topicFilter, CancellationToken ct = default) =>
    _client.SubscribeAsync(topicFilter, MqttQualityOfServiceLevel.AtLeastOnce, ct);

  public Task PublishAsync(string topic, string payload, bool retain = true, CancellationToken ct = default)
  {
    var message = new MqttApplicationMessageBuilder()
      .WithTopic(topic)
      .WithPayload(Encoding.UTF8.GetBytes(payload))
      .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
      .WithRetainFlag(retain)
      .Build();

    return _client.PublishAsync(message, ct);
  }

  private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
  {
    var handler = MessageReceived;
    if (handler is null)
    {
      return;
    }

    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
    try
    {
      await handler(new MqttMessage(e.ApplicationMessage.Topic, payload), CancellationToken.None);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "MQTT message handler failed for topic {Topic}.", e.ApplicationMessage.Topic);
    }
  }

  public async ValueTask DisposeAsync()
  {
    if (_client.IsConnected)
    {
      await _client.DisconnectAsync();
    }

    _client.Dispose();
  }
}

namespace WindowsAas.Plugins.Av.Audio;

/// <summary>Direction of an audio endpoint.</summary>
public enum AudioDirection
{
  Render,
  Capture,
}

/// <summary>Snapshot of an audio endpoint's state and properties.</summary>
public sealed record AudioDeviceInfo
{
  public required string Id { get; init; }

  public required string FriendlyName { get; init; }

  public required AudioDirection Direction { get; init; }

  /// <summary>Device state (Active, Disabled, NotPresent, Unplugged).</summary>
  public required string State { get; init; }

  /// <summary>Whether the device is active/enabled (read-only; hardware info).</summary>
  public bool Enabled { get; init; }

  /// <summary>Master volume scalar 0.0–1.0 (writable for active devices).</summary>
  public float Volume { get; init; }

  public bool Muted { get; init; }

  public int SampleRate { get; init; }

  public int Channels { get; init; }

  public int BitsPerSample { get; init; }
}

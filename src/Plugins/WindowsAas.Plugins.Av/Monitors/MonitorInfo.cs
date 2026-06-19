namespace WindowsAas.Plugins.Av.Monitors;

/// <summary>Snapshot of a monitor's current settings and hardware identity.</summary>
public sealed record MonitorInfo
{
  /// <summary>1-based index matching the order shown in Windows 11 Settings.</summary>
  public required int Index { get; init; }

  /// <summary>GDI device name, e.g. <c>\\.\DISPLAY1</c> (read-only).</summary>
  public required string DeviceName { get; init; }

  /// <summary>Adapter/monitor hardware description (read-only).</summary>
  public required string HardwareId { get; init; }

  /// <summary>Whether the monitor is attached to the desktop.</summary>
  public bool Enabled { get; init; }

  public int Width { get; init; }

  public int Height { get; init; }

  public int RefreshRate { get; init; }

  public int BitDepth { get; init; }
}

namespace WindowsAas.Plugins.Av.Monitors;

/// <summary>Reads and controls the host's monitors, addressed by 1-based index.</summary>
public interface IMonitorController
{
  /// <summary>Enumerates attached monitors in Settings order.</summary>
  IReadOnlyList<MonitorInfo> List();

  /// <summary>Changes the resolution of the monitor at <paramref name="index"/>.</summary>
  bool SetResolution(int index, int width, int height);

  /// <summary>Changes the refresh rate (Hz).</summary>
  bool SetRefreshRate(int index, int hertz);

  /// <summary>Changes the colour depth (bits per pixel).</summary>
  bool SetBitDepth(int index, int bitsPerPixel);
}

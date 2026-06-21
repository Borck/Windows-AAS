using System.Collections.Concurrent;
using System.Globalization;
using WindowsAas.Abstractions.Plugins;
using WindowsAas.Abstractions.Submodels;
using WindowsAas.Plugins.Av.Audio;
using WindowsAas.Plugins.Av.Monitors;

namespace WindowsAas.Plugins.Av;

/// <summary>
/// AV plugin: exposes monitor settings (per-monitor submodels, addressed by the
/// 1-based Settings index) and audio input/output endpoints (one submodel per
/// device) to the AAS, plus an overview/info submodel.
/// </summary>
public sealed class AvPlugin : IPlugin
{
  public const string InfoSubmodelId = "urn:windows-aas:av:info";
  public const string MonitorSubmodelPrefix = "urn:windows-aas:av:monitor:";
  public const string AudioSubmodelPrefix = "urn:windows-aas:av:audio:";

  private readonly IMonitorController _monitors;
  private readonly IAudioController _audio;

  // Maps an audio submodel id back to the real (opaque) endpoint id.
  private readonly ConcurrentDictionary<string, string> _audioIds = new(StringComparer.Ordinal);

  private IPluginContext? _context;

  public AvPlugin() : this(new WindowsMonitorController(), new WindowsAudioController())
  {
  }

  /// <summary>Test seam allowing controllers to be injected.</summary>
  public AvPlugin(IMonitorController monitors, IAudioController audio)
  {
    _monitors = monitors;
    _audio = audio;
  }

  public string Id => "windows-aas.av";

  public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
  {
    _context = context;
    return Task.CompletedTask;
  }

  public Task<IReadOnlyList<SubmodelDefinition>> GetSubmodelsAsync(CancellationToken ct = default)
  {
    var submodels = new List<SubmodelDefinition> { BuildInfoSubmodel() };

    foreach (var monitor in _monitors.List())
    {
      submodels.Add(BuildMonitorSubmodel(monitor));
    }

    _audioIds.Clear();
    var audioIndex = 0;
    foreach (var device in _audio.List())
    {
      audioIndex++;
      var submodelId = $"{AudioSubmodelPrefix}{audioIndex}";
      _audioIds[submodelId] = device.Id;
      submodels.Add(BuildAudioSubmodel(submodelId, audioIndex, device));
    }

    return Task.FromResult<IReadOnlyList<SubmodelDefinition>>(submodels);
  }

  public Task<string?> ReadAsync(string submodelId, string idShortPath, CancellationToken ct = default)
  {
    if (submodelId.StartsWith(MonitorSubmodelPrefix, StringComparison.Ordinal))
    {
      var index = int.Parse(submodelId[MonitorSubmodelPrefix.Length..], CultureInfo.InvariantCulture);
      var monitor = _monitors.List().FirstOrDefault(m => m.Index == index);
      return Task.FromResult(monitor is null ? null : ReadMonitor(monitor, idShortPath));
    }

    if (_audioIds.TryGetValue(submodelId, out var deviceId))
    {
      var device = _audio.List().FirstOrDefault(d => d.Id == deviceId);
      return Task.FromResult(device is null ? null : ReadAudio(device, idShortPath));
    }

    return Task.FromResult<string?>(null);
  }

  public Task<PropertyWriteResult> WriteAsync(PropertyWriteRequest request, CancellationToken ct = default)
  {
    if (request.SubmodelId.StartsWith(MonitorSubmodelPrefix, StringComparison.Ordinal))
    {
      return Task.FromResult(WriteMonitor(request));
    }

    if (_audioIds.TryGetValue(request.SubmodelId, out var deviceId))
    {
      return Task.FromResult(WriteAudio(deviceId, request));
    }

    return Task.FromResult(PropertyWriteResult.Fail($"Unknown submodel '{request.SubmodelId}'."));
  }

  public ValueTask DisposeAsync() => ValueTask.CompletedTask;

  private PropertyWriteResult WriteMonitor(PropertyWriteRequest request)
  {
    var index = int.Parse(request.SubmodelId[MonitorSubmodelPrefix.Length..], CultureInfo.InvariantCulture);

    var ok = request.IdShortPath switch
    {
      "Resolution" when TryParseResolution(request.Value, out var w, out var h) =>
        _monitors.SetResolution(index, w, h),
      "RefreshRate" when int.TryParse(request.Value, out var hz) =>
        _monitors.SetRefreshRate(index, hz),
      "BitDepth" when int.TryParse(request.Value, out var bpp) =>
        _monitors.SetBitDepth(index, bpp),
      _ => false,
    };

    if (!ok)
    {
      return PropertyWriteResult.Fail($"Monitor write '{request.IdShortPath}={request.Value}' not applied.");
    }

    var monitor = _monitors.List().FirstOrDefault(m => m.Index == index);
    return PropertyWriteResult.Ok(monitor is null ? request.Value : ReadMonitor(monitor, request.IdShortPath) ?? request.Value);
  }

  private PropertyWriteResult WriteAudio(string deviceId, PropertyWriteRequest request)
  {
    var ok = request.IdShortPath switch
    {
      "Volume" when float.TryParse(request.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) =>
        _audio.SetVolume(deviceId, v),
      "Muted" when bool.TryParse(request.Value, out var m) =>
        _audio.SetMuted(deviceId, m),
      _ => false,
    };

    if (!ok)
    {
      return PropertyWriteResult.Fail($"Audio write '{request.IdShortPath}={request.Value}' not applied.");
    }

    var device = _audio.List().FirstOrDefault(d => d.Id == deviceId);
    return PropertyWriteResult.Ok(device is null ? request.Value : ReadAudio(device, request.IdShortPath) ?? request.Value);
  }

  private static string? ReadMonitor(MonitorInfo m, string idShortPath) => idShortPath switch
  {
    "Resolution" => $"{m.Width}x{m.Height}",
    "RefreshRate" => m.RefreshRate.ToString(CultureInfo.InvariantCulture),
    "BitDepth" => m.BitDepth.ToString(CultureInfo.InvariantCulture),
    "Enabled" => Bool(m.Enabled),
    "DeviceName" => m.DeviceName,
    "HardwareId" => m.HardwareId,
    _ => null,
  };

  private static string? ReadAudio(AudioDeviceInfo d, string idShortPath) => idShortPath switch
  {
    "Volume" => d.Volume.ToString(CultureInfo.InvariantCulture),
    "Muted" => Bool(d.Muted),
    "Enabled" => Bool(d.Enabled),
    "FriendlyName" => d.FriendlyName,
    "Direction" => d.Direction.ToString(),
    "State" => d.State,
    "SampleRate" => d.SampleRate.ToString(CultureInfo.InvariantCulture),
    "Channels" => d.Channels.ToString(CultureInfo.InvariantCulture),
    "BitsPerSample" => d.BitsPerSample.ToString(CultureInfo.InvariantCulture),
    _ => null,
  };

  private SubmodelDefinition BuildInfoSubmodel()
  {
    var monitors = _monitors.List().Count;
    var audio = _audio.List().Count;
    return new SubmodelDefinition
    {
      Id = InfoSubmodelId,
      IdShort = "AvInfo",
      SemanticId = "urn:windows-aas:av:semantics:info",
      Elements =
      [
        Prop("PluginId", Id),
        Prop("MonitorCount", monitors.ToString(CultureInfo.InvariantCulture), AasValueType.Integer),
        Prop("AudioDeviceCount", audio.ToString(CultureInfo.InvariantCulture), AasValueType.Integer),
      ],
    };
  }

  private static SubmodelDefinition BuildMonitorSubmodel(MonitorInfo m) => new()
  {
    Id = $"{MonitorSubmodelPrefix}{m.Index}",
    IdShort = $"Monitor{m.Index}",
    SemanticId = "urn:windows-aas:av:semantics:monitor",
    Elements =
    [
      Prop("Enabled", Bool(m.Enabled), AasValueType.Boolean, writable: true),
      Prop("Resolution", $"{m.Width}x{m.Height}", writable: true),
      Prop("RefreshRate", m.RefreshRate.ToString(CultureInfo.InvariantCulture), AasValueType.Integer, writable: true),
      Prop("BitDepth", m.BitDepth.ToString(CultureInfo.InvariantCulture), AasValueType.Integer, writable: true),
      Prop("DeviceName", m.DeviceName),
      Prop("HardwareId", m.HardwareId),
    ],
  };

  private static SubmodelDefinition BuildAudioSubmodel(string submodelId, int index, AudioDeviceInfo d) => new()
  {
    Id = submodelId,
    IdShort = $"{d.Direction}{index}",
    SemanticId = "urn:windows-aas:av:semantics:audio",
    Elements =
    [
      Prop("FriendlyName", d.FriendlyName),
      Prop("Direction", d.Direction.ToString()),
      Prop("State", d.State),
      Prop("Enabled", Bool(d.Enabled), AasValueType.Boolean),
      Prop("Volume", d.Volume.ToString(CultureInfo.InvariantCulture), AasValueType.Double, writable: true),
      Prop("Muted", Bool(d.Muted), AasValueType.Boolean, writable: true),
      Prop("SampleRate", d.SampleRate.ToString(CultureInfo.InvariantCulture), AasValueType.Integer),
      Prop("Channels", d.Channels.ToString(CultureInfo.InvariantCulture), AasValueType.Integer),
      Prop("BitsPerSample", d.BitsPerSample.ToString(CultureInfo.InvariantCulture), AasValueType.Integer),
    ],
  };

  private static SubmodelElement Prop(
    string idShort,
    string? value,
    AasValueType type = AasValueType.String,
    bool writable = false) => new()
  {
    IdShort = idShort,
    Kind = SubmodelElementKind.Property,
    ValueType = type,
    Value = value,
    Writable = writable,
  };

  private static string Bool(bool value) => value ? "true" : "false";

  private static bool TryParseResolution(string value, out int width, out int height)
  {
    width = height = 0;
    var parts = value.Split('x', 'X');
    return parts.Length == 2
      && int.TryParse(parts[0], out width)
      && int.TryParse(parts[1], out height);
  }
}

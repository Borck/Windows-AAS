namespace WindowsAas.Plugins.Av.Audio;

/// <summary>Reads and controls the host's audio endpoints.</summary>
public interface IAudioController
{
  /// <summary>Enumerates all render and capture endpoints in a stable order.</summary>
  IReadOnlyList<AudioDeviceInfo> List();

  /// <summary>Sets the master volume scalar (0.0–1.0) of an endpoint.</summary>
  bool SetVolume(string deviceId, float volume);

  /// <summary>Mutes/unmutes an endpoint.</summary>
  bool SetMuted(string deviceId, bool muted);
}

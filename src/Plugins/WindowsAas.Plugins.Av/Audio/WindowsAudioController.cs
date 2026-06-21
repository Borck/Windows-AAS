using NAudio.CoreAudioApi;

namespace WindowsAas.Plugins.Av.Audio;

/// <summary>
/// <see cref="IAudioController"/> implemented with NAudio's Core Audio (MMDevice) API.
/// Volume and mute are writable for active endpoints; device format and state are
/// reported read-only. The Windows platform is implied by the project's
/// <c>net10.0-windows</c> target framework.
/// </summary>
public sealed class WindowsAudioController : IAudioController
{
  public IReadOnlyList<AudioDeviceInfo> List()
  {
    using var enumerator = new MMDeviceEnumerator();
    var devices = new List<AudioDeviceInfo>();

    foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.All))
    {
      using (device)
      {
        devices.Add(Describe(device));
      }
    }

    // Stable order: render endpoints first, then capture, by friendly name.
    return devices
      .OrderBy(d => d.Direction)
      .ThenBy(d => d.FriendlyName, StringComparer.OrdinalIgnoreCase)
      .ToList();
  }

  public bool SetVolume(string deviceId, float volume)
  {
    var clamped = Math.Clamp(volume, 0f, 1f);
    return WithDevice(deviceId, device => device.AudioEndpointVolume.MasterVolumeLevelScalar = clamped);
  }

  public bool SetMuted(string deviceId, bool muted) =>
    WithDevice(deviceId, device => device.AudioEndpointVolume.Mute = muted);

  private static bool WithDevice(string deviceId, Action<MMDevice> action)
  {
    using var enumerator = new MMDeviceEnumerator();
    using var device = enumerator.GetDevice(deviceId);
    if (device is null || device.State != DeviceState.Active)
    {
      return false;
    }

    action(device);
    return true;
  }

  private static AudioDeviceInfo Describe(MMDevice device)
  {
    var direction = device.DataFlow == DataFlow.Capture ? AudioDirection.Capture : AudioDirection.Render;
    var active = device.State == DeviceState.Active;

    float volume = 0f;
    bool muted = false;
    int sampleRate = 0, channels = 0, bits = 0;

    if (active)
    {
      // These accessors throw for inactive endpoints, hence the guard above.
      volume = device.AudioEndpointVolume.MasterVolumeLevelScalar;
      muted = device.AudioEndpointVolume.Mute;

      var format = device.AudioClient.MixFormat;
      sampleRate = format.SampleRate;
      channels = format.Channels;
      bits = format.BitsPerSample;
    }

    return new AudioDeviceInfo
    {
      Id = device.ID,
      FriendlyName = device.FriendlyName,
      Direction = direction,
      State = device.State.ToString(),
      Enabled = active,
      Volume = volume,
      Muted = muted,
      SampleRate = sampleRate,
      Channels = channels,
      BitsPerSample = bits,
    };
  }
}

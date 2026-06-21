using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace WindowsAas.Plugins.Av.Monitors;

/// <summary>
/// <see cref="IMonitorController"/> implemented with the classic GDI display APIs
/// (<c>EnumDisplayDevices</c> / <c>EnumDisplaySettings</c> / <c>ChangeDisplaySettingsEx</c>)
/// via CsWin32-generated P/Invoke. The Windows platform is implied by the project's
/// <c>net10.0-windows</c> target framework.
/// </summary>
public sealed class WindowsMonitorController : IMonitorController
{
  private const uint AttachedToDesktop = 0x1; // DISPLAY_DEVICE_ATTACHED_TO_DESKTOP

  public IReadOnlyList<MonitorInfo> List()
  {
    var monitors = new List<MonitorInfo>();
    var index = 0;

    foreach (var (device, name) in EnumerateAttachedAdapters())
    {
      index++;
      var info = new MonitorInfo
      {
        Index = index,
        DeviceName = name,
        HardwareId = device.DeviceString.ToString(),
        Enabled = true,
      };

      if (TryGetCurrentMode(name, out var mode))
      {
        info = info with
        {
          Width = (int)mode.dmPelsWidth,
          Height = (int)mode.dmPelsHeight,
          RefreshRate = (int)mode.dmDisplayFrequency,
          BitDepth = (int)mode.dmBitsPerPel,
        };
      }

      monitors.Add(info);
    }

    return monitors;
  }

  public bool SetResolution(int index, int width, int height) =>
    ApplyChange(index, (ref DEVMODEW mode) =>
    {
      mode.dmPelsWidth = (uint)width;
      mode.dmPelsHeight = (uint)height;
      mode.dmFields |= DEVMODE_FIELD_FLAGS.DM_PELSWIDTH | DEVMODE_FIELD_FLAGS.DM_PELSHEIGHT;
    });

  public bool SetRefreshRate(int index, int hertz) =>
    ApplyChange(index, (ref DEVMODEW mode) =>
    {
      mode.dmDisplayFrequency = (uint)hertz;
      mode.dmFields |= DEVMODE_FIELD_FLAGS.DM_DISPLAYFREQUENCY;
    });

  public bool SetBitDepth(int index, int bitsPerPixel) =>
    ApplyChange(index, (ref DEVMODEW mode) =>
    {
      mode.dmBitsPerPel = (uint)bitsPerPixel;
      mode.dmFields |= DEVMODE_FIELD_FLAGS.DM_BITSPERPEL;
    });

  private delegate void ModeMutator(ref DEVMODEW mode);

  private unsafe bool ApplyChange(int index, ModeMutator mutate)
  {
    var name = DeviceNameFor(index);
    if (name is null || !TryGetCurrentMode(name, out var mode))
    {
      return false;
    }

    mutate(ref mode);

    fixed (char* namePtr = name)
    {
      var result = PInvoke.ChangeDisplaySettingsEx(
        new PCWSTR(namePtr), &mode, HWND.Null, CDS_TYPE.CDS_UPDATEREGISTRY, null);
      return result == DISP_CHANGE.DISP_CHANGE_SUCCESSFUL;
    }
  }

  private string? DeviceNameFor(int index)
  {
    var current = 0;
    foreach (var (_, name) in EnumerateAttachedAdapters())
    {
      if (++current == index)
      {
        return name;
      }
    }

    return null;
  }

  private static IEnumerable<(DISPLAY_DEVICEW Device, string Name)> EnumerateAttachedAdapters()
  {
    var device = new DISPLAY_DEVICEW { cb = (uint)Marshal.SizeOf<DISPLAY_DEVICEW>() };
    uint i = 0;
    while (PInvoke.EnumDisplayDevices(null, i, ref device, 0))
    {
      if (((uint)device.StateFlags & AttachedToDesktop) != 0)
      {
        yield return (device, device.DeviceName.ToString());
      }

      device = new DISPLAY_DEVICEW { cb = (uint)Marshal.SizeOf<DISPLAY_DEVICEW>() };
      i++;
    }
  }

  private static bool TryGetCurrentMode(string deviceName, out DEVMODEW mode)
  {
    mode = new DEVMODEW { dmSize = (ushort)Marshal.SizeOf<DEVMODEW>() };
    return PInvoke.EnumDisplaySettings(deviceName, ENUM_DISPLAY_SETTINGS_MODE.ENUM_CURRENT_SETTINGS, ref mode);
  }
}

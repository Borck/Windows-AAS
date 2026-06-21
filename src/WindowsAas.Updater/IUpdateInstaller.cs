namespace WindowsAas.Updater;

/// <summary>Applies a verified installer, handing off so the service can restart itself.</summary>
public interface IUpdateInstaller
{
  /// <summary>
  /// Launches the update install for <paramref name="msiPath"/> (or via winget) in a
  /// detached process, then returns so the service can shut down and be restarted by
  /// the installer / service control manager.
  /// </summary>
  void Install(string msiPath);
}

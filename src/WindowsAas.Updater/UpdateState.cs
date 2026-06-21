namespace WindowsAas.Updater;

/// <summary>
/// Shared, observable state about update availability. Updated by the background
/// checker and read by the admin UI to show "an update is available".
/// </summary>
public sealed class UpdateState
{
  private UpdateInfo? _available;

  public event Action? Changed;

  public UpdateInfo? Available
  {
    get => _available;
    set
    {
      _available = value;
      Changed?.Invoke();
    }
  }

  public DateTimeOffset? LastCheckedUtc { get; set; }
}

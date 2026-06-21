namespace WindowsAas.Updater;

/// <summary>Queries the update feed for a release newer than the running version.</summary>
public interface IUpdateChecker
{
  /// <summary>Returns the newest available update, or <c>null</c> if up to date.</summary>
  Task<UpdateInfo?> GetAvailableUpdateAsync(CancellationToken ct = default);
}

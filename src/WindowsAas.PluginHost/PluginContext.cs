using Microsoft.Extensions.Logging;
using WindowsAas.Abstractions.Plugins;

namespace WindowsAas.PluginHost;

/// <summary>
/// Host implementation of <see cref="IPluginContext"/> handed to each plugin on
/// initialization.
/// </summary>
internal sealed class PluginContext(
  ILogger logger,
  IReadOnlyDictionary<string, string> configuration,
  string dataDirectory,
  IPluginValueReporter reporter) : IPluginContext
{
  public ILogger Logger => logger;

  public IReadOnlyDictionary<string, string> Configuration => configuration;

  public string DataDirectory => dataDirectory;

  public ValueTask ReportValueAsync(string submodelId, string idShortPath, string value, CancellationToken ct = default) =>
    reporter.ReportAsync(submodelId, idShortPath, value, ct);
}

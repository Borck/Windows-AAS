using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WindowsAas.Abstractions.Plugins;
using WindowsAas.Abstractions.Submodels;

namespace WindowsAas.PluginHost;

/// <summary>
/// Default <see cref="IPluginHost"/>. Each plugin lives in its own sub-directory of
/// <see cref="PluginHostOptions.PluginsDirectory"/> with a <c>plugin.json</c> manifest,
/// and is loaded into an isolated, collectible <see cref="PluginLoadContext"/>.
/// </summary>
public sealed class PluginHost(
  IOptions<PluginHostOptions> options,
  IPluginValueReporter reporter,
  ILoggerFactory loggerFactory,
  ILogger<PluginHost> logger) : IPluginHost
{
  private const string ManifestFileName = "plugin.json";
  private const string DisabledMarker = ".disabled";

  private static readonly JsonSerializerOptions ManifestJson = new(JsonSerializerDefaults.Web);

  private readonly PluginHostOptions _options = options.Value;
  private readonly ConcurrentDictionary<string, LoadedPlugin> _plugins = new(StringComparer.OrdinalIgnoreCase);
  private readonly ConcurrentDictionary<string, string> _submodelToPlugin = new(StringComparer.Ordinal);

  public async Task InitializeAsync(CancellationToken ct = default)
  {
    Directory.CreateDirectory(_options.PluginsDirectory);
    Directory.CreateDirectory(_options.DataDirectory);

    foreach (var dir in Directory.EnumerateDirectories(_options.PluginsDirectory))
    {
      var manifestPath = Path.Combine(dir, ManifestFileName);
      if (!File.Exists(manifestPath))
      {
        continue;
      }

      try
      {
        var manifest = await ReadManifestAsync(manifestPath, ct);
        var loaded = new LoadedPlugin { Manifest = manifest, Directory = dir };
        _plugins[manifest.Id] = loaded;

        if (!File.Exists(Path.Combine(dir, DisabledMarker)))
        {
          await EnableAsync(manifest.Id, ct);
        }
        else
        {
          loaded.State = PluginState.Disabled;
        }
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Failed to load plugin manifest at {Path}.", manifestPath);
      }
    }
  }

  public IReadOnlyList<PluginInfo> List() => _plugins.Values.Select(p => p.ToInfo()).ToList();

  public async Task EnableAsync(string pluginId, CancellationToken ct = default)
  {
    if (!_plugins.TryGetValue(pluginId, out var loaded))
    {
      throw new InvalidOperationException($"Plugin '{pluginId}' is not installed.");
    }

    if (loaded.State == PluginState.Enabled)
    {
      return;
    }

    try
    {
      var assemblyPath = Path.Combine(loaded.Directory, loaded.Manifest.EntryAssembly);
      var context = new PluginLoadContext(assemblyPath);
      var assembly = context.LoadFromAssemblyPath(assemblyPath);
      var type = assembly.GetType(loaded.Manifest.EntryType, throwOnError: true)!;

      if (Activator.CreateInstance(type) is not IPlugin instance)
      {
        throw new InvalidOperationException(
          $"Type '{loaded.Manifest.EntryType}' does not implement IPlugin.");
      }

      var dataDir = Path.Combine(_options.DataDirectory, loaded.Manifest.Id);
      Directory.CreateDirectory(dataDir);

      var pluginLogger = loggerFactory.CreateLogger($"Plugin.{loaded.Manifest.Id}");
      var pluginContext = new PluginContext(pluginLogger, ReadConfiguration(loaded), dataDir, reporter);
      await instance.InitializeAsync(pluginContext, ct);

      loaded.Instance = instance;
      loaded.LoadContext = context;
      loaded.State = PluginState.Enabled;
      loaded.Error = null;

      foreach (var submodel in await instance.GetSubmodelsAsync(ct))
      {
        _submodelToPlugin[submodel.Id] = loaded.Manifest.Id;
      }

      File.Delete(Path.Combine(loaded.Directory, DisabledMarker));
      logger.LogInformation("Enabled plugin {PluginId} v{Version}.", loaded.Manifest.Id, loaded.Manifest.Version);
    }
    catch (Exception ex)
    {
      loaded.State = PluginState.Faulted;
      loaded.Error = ex.Message;
      logger.LogError(ex, "Failed to enable plugin {PluginId}.", pluginId);
      throw;
    }
  }

  public async Task DisableAsync(string pluginId, CancellationToken ct = default)
  {
    if (!_plugins.TryGetValue(pluginId, out var loaded) || loaded.Instance is null)
    {
      return;
    }

    foreach (var key in _submodelToPlugin.Where(kv => kv.Value == pluginId).Select(kv => kv.Key).ToList())
    {
      _submodelToPlugin.TryRemove(key, out _);
    }

    await loaded.Instance.DisposeAsync();
    UnloadContext(loaded);

    loaded.Instance = null;
    loaded.State = PluginState.Disabled;
    await File.WriteAllTextAsync(Path.Combine(loaded.Directory, DisabledMarker), string.Empty, ct);
    logger.LogInformation("Disabled plugin {PluginId}.", pluginId);
  }

  public async Task<IReadOnlyList<SubmodelDefinition>> GetSubmodelsAsync(CancellationToken ct = default)
  {
    var result = new List<SubmodelDefinition>();
    foreach (var plugin in _plugins.Values.Where(p => p is { State: PluginState.Enabled, Instance: not null }))
    {
      result.AddRange(await plugin.Instance!.GetSubmodelsAsync(ct));
    }

    return result;
  }

  public Task<PropertyWriteResult> WriteAsync(PropertyWriteRequest request, CancellationToken ct = default)
  {
    if (!_submodelToPlugin.TryGetValue(request.SubmodelId, out var pluginId) ||
        !_plugins.TryGetValue(pluginId, out var loaded) ||
        loaded.Instance is null)
    {
      return Task.FromResult(PropertyWriteResult.Fail($"No enabled plugin owns submodel '{request.SubmodelId}'."));
    }

    return loaded.Instance.WriteAsync(request, ct);
  }

  private static async Task<PluginManifest> ReadManifestAsync(string path, CancellationToken ct)
  {
    await using var stream = File.OpenRead(path);
    var manifest = await JsonSerializer.DeserializeAsync<PluginManifest>(stream, ManifestJson, ct);
    return manifest ?? throw new InvalidOperationException($"Empty or invalid manifest: {path}");
  }

  private Dictionary<string, string> ReadConfiguration(LoadedPlugin plugin)
  {
    // Per-plugin configuration is stored as config.json in the plugin's data dir;
    // values are written by the admin UI (secrets are protected by the host).
    var configPath = Path.Combine(_options.DataDirectory, plugin.Manifest.Id, "config.json");
    if (!File.Exists(configPath))
    {
      return new Dictionary<string, string>();
    }

    var json = File.ReadAllText(configPath);
    return JsonSerializer.Deserialize<Dictionary<string, string>>(json, ManifestJson)
      ?? new Dictionary<string, string>();
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static void UnloadContext(LoadedPlugin loaded)
  {
    // Unload must run where no local can keep the context alive, so the GC can
    // collect the collectible ALC and release the plugin's assemblies.
    loaded.LoadContext?.Unload();
    loaded.LoadContext = null;
    GC.Collect();
    GC.WaitForPendingFinalizers();
  }
}

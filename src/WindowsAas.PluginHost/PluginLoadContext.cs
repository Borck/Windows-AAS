using System.Reflection;
using System.Runtime.Loader;

namespace WindowsAas.PluginHost;

/// <summary>
/// Collectible <see cref="AssemblyLoadContext"/> that isolates a single plugin and
/// its private dependencies, resolving them from the plugin's own folder via the
/// assembly's <c>.deps.json</c>. Being collectible lets the host unload a plugin
/// when it is disabled or upgraded.
/// </summary>
internal sealed class PluginLoadContext(string entryAssemblyPath)
  : AssemblyLoadContext(name: Path.GetFileNameWithoutExtension(entryAssemblyPath), isCollectible: true)
{
  private readonly AssemblyDependencyResolver _resolver = new(entryAssemblyPath);

  protected override Assembly? Load(AssemblyName assemblyName)
  {
    // Types from the shared contract must unify with the host's copy, so let those
    // fall through to the default context rather than loading a private duplicate.
    if (assemblyName.Name is "WindowsAas.Abstractions")
    {
      return null;
    }

    var path = _resolver.ResolveAssemblyToPath(assemblyName);
    return path is null ? null : LoadFromAssemblyPath(path);
  }

  protected override nint LoadUnmanagedDll(string unmanagedDllName)
  {
    var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
    return path is null ? nint.Zero : LoadUnmanagedDllFromPath(path);
  }
}

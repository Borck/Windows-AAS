using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using WindowsAas.Web.Logging;

namespace WindowsAas.Web;

/// <summary>DI helpers for the admin web UI.</summary>
public static class WebServiceCollectionExtensions
{
  /// <summary>
  /// Registers MudBlazor, the interactive-server Razor components, and the shared
  /// <see cref="LogStore"/> used by the log viewer.
  /// </summary>
  public static IServiceCollection AddWindowsAasWebUi(this IServiceCollection services)
  {
    services.AddMudServices();
    services.AddRazorComponents().AddInteractiveServerComponents();
    services.AddSingleton<LogStore>();
    return services;
  }
}

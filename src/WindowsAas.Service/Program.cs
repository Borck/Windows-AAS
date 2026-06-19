using System.Globalization;
using System.Runtime.Versioning;
using Serilog;
using WindowsAas.Aas;
using WindowsAas.Mqtt;
using WindowsAas.PluginHost;
using WindowsAas.Security;
using WindowsAas.Service;
using WindowsAas.Service.Bridge;
using WindowsAas.Service.Logging;
using WindowsAas.Web;
using WindowsAas.Web.Components;
using WindowsAas.Web.Logging;

var builder = WebApplication.CreateBuilder(args);

// Run as a Windows Service when hosted by the SCM (no-op when run interactively).
builder.Host.UseWindowsService(o => o.ServiceName = "WindowsAAS");

// Admin UI must only be reachable from the local machine (REQUIREMENTS.md). The
// bind addresses come from the "Urls" setting and default to localhost-only. The
// installer provisions a certificate and adds the https://127.0.0.1 binding.
builder.WebHost.UseUrls(
  (builder.Configuration["Urls"] ?? "http://127.0.0.1:5080")
  .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

// LogStore feeds the in-app log viewer; also write a rolling file for support.
var logStore = new LogStore();
builder.Services.AddSingleton(logStore);
builder.Host.UseSerilog((context, services, configuration) => configuration
  .ReadFrom.Configuration(context.Configuration)
  .Enrich.FromLogContext()
  .WriteTo.File(
    Path.Combine(Path.GetTempPath(), "windows-aas", "service-.log"),
    rollingInterval: RollingInterval.Day,
    formatProvider: CultureInfo.InvariantCulture)
  .WriteTo.Sink(new LogStoreSink(logStore)));

// Options bound from configuration (the encrypted store overlays secrets).
builder.Services.Configure<AasOptions>(builder.Configuration.GetSection(AasOptions.SectionName));
builder.Services.Configure<MqttOptions>(builder.Configuration.GetSection(MqttOptions.SectionName));
builder.Services.Configure<PluginHostOptions>(builder.Configuration.GetSection(PluginHostOptions.SectionName));

// Secret protection: DPAPI on Windows, throwing stub elsewhere (dev builds).
RegisterSecretProtector(builder.Services);

// AAS environment client.
builder.Services.AddHttpClient<IAasEnvironmentClient, BaSyxAasEnvironmentClient>();

// MQTT + topic conventions.
builder.Services.AddSingleton<IMqttBus, MqttBus>();
builder.Services.AddSingleton(sp =>
{
  var prefix = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MqttOptions>>().Value.TopicPrefix;
  return new MqttTopics(prefix);
});

// Plugin host + the value reporter that bridges plugin output to MQTT/AAS.
builder.Services.AddSingleton<IPluginHost, PluginHost>();
builder.Services.AddSingleton<IPluginValueReporter, ValueReporter>();

// Bridge orchestration runs as a background service.
builder.Services.AddHostedService<BridgeOrchestrator>();

// Admin web UI (MudBlazor + interactive server components).
builder.Services.AddWindowsAasWebUi();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

[SupportedOSPlatform("windows")]
static void RegisterWindowsProtector(IServiceCollection services) =>
  services.AddSingleton<ISecretProtector, DpapiSecretProtector>();

static void RegisterSecretProtector(IServiceCollection services)
{
  if (OperatingSystem.IsWindows())
  {
    RegisterWindowsProtector(services);
  }
  else
  {
    // Non-Windows dev builds: secret protection is a Windows-only feature.
    services.AddSingleton<ISecretProtector, PassthroughSecretProtector>();
  }
}

# Windows-AAS — Architecture

Windows-AAS is a plugin-based **Asset Administration Shell (AAS)** front end for a
Windows host. A system-level Windows service reads and writes host properties and
bridges them to an AAS that lives in a BaSyx `aas-environment` container. The
service talks to that environment **over MQTT**; it does not host the AAS itself.

## Components

| Project | Responsibility |
| --- | --- |
| `WindowsAas.Abstractions` | Plugin contract (`IPlugin`, `IPluginContext`), `PluginManifest`, and the AAS-neutral submodel model (`SubmodelDefinition`/`SubmodelElement`/`AasValueType`). |
| `WindowsAas.Service` | ASP.NET Core host registered as a Windows Service. Kestrel bound to `127.0.0.1`. Hosts the admin UI and the background bridge/updater services. |
| `WindowsAas.Web` | Blazor Server + MudBlazor admin UI (Overview, Plugins, Repository, Logs, Updates) and the in-memory `LogStore`. |
| `WindowsAas.PluginHost` | Discovers, loads (isolated, collectible `AssemblyLoadContext`), enables/disables plugins and routes reads/writes. |
| `WindowsAas.Aas` | AAS v3 REST client for the BaSyx environment (host shell + submodel registration, element value write-back) and the submodel→JSON mapper. |
| `WindowsAas.Mqtt` | MQTTnet client and topic conventions for the control/telemetry paths. |
| `WindowsAas.Security` | DPAPI secret protection (config-at-rest) and RSA plugin-package signature verification. |
| `WindowsAas.Repository.Client` / `.Server` | Online plugin repository client (download + verify + install) and a minimal-API server. |
| `WindowsAas.Updater` | Update feed checker, SHA-256 + Authenticode verification, MSI/winget hand-off, background poller. |
| `WindowsAas.Plugins.Av` | Monitor + audio submodels (CsWin32 display APIs, NAudio Core Audio). |
| `WindowsAas.Plugins.Automation` | Scripts/applications exposed as runnable tasks with timer triggers. |

## Runtime topology

```
Admin browser ──https──▶ 127.0.0.1  (WindowsAas.Service, LocalSystem, starts on boot)
                                     ├─ Blazor admin UI (MudBlazor)
                                     ├─ PluginHost  ── AV / Automation plugins
                                     ├─ BridgeOrchestrator ─┐
                                     └─ UpdateBackgroundService
                                                            │ MQTT (TLS)
                          Docker ◀───────────────────────────┘
                          eclipsebasyx/aas-environment ⇄ AAS + Submodel registries
                          eclipse-mosquitto (broker)
```

## Integration model (control + telemetry)

1. An AAS client writes a submodel element value via the environment's REST API.
2. The environment emits an **MQTT value-change event**. `BridgeOrchestrator`
   subscribes (`{prefix}/{submodelId}/{path}/set`), parses it, and routes a
   `PropertyWriteRequest` to the owning plugin (**AAS → host control path**).
3. The plugin applies the change and returns the *actual* value. `ValueReporter`
   publishes it to `{prefix}/{submodelId}/{path}/value` and writes it back into the
   environment (**host → AAS telemetry / ingestion bridge**).

Submodel ids (IRIs) are base64url-encoded into a single MQTT topic level so the
topic hierarchy stays well-formed (`MqttTopics`).

## Plugin model

Each plugin ships a `plugin.json` manifest and is loaded into its own collectible
`PluginLoadContext`, sharing only `WindowsAas.Abstractions` with the host so plugin
dependencies stay isolated and the context can be unloaded on disable/upgrade. A
plugin contributes one info/overview submodel plus capability submodels — the AV
plugin contributes one submodel per monitor and per audio device.

See [PLUGIN-AUTHORING.md](PLUGIN-AUTHORING.md) to build one.

## Security

Localhost-only admin binding, DPAPI-encrypted secrets at rest, mandatory RSA
signature verification for repository plugins, and SHA-256 + Authenticode
verification for updates. Full threat model in [STRIDE.md](STRIDE.md).

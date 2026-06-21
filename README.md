# Windows-AAS

Windows-AAS is a plugin-based **Asset Administration Shell (AAS)** for Windows hosts.
A system-level Windows service reads and writes host properties (displays, audio,
automation, …) and bridges them — over MQTT — to an AAS hosted in a BaSyx
`aas-environment` container. An admin web UI (Blazor + MudBlazor, loopback-only)
manages plugins, logs and updates.

## Highlights

- **System service**, starts on boot (LocalSystem), not at user login.
- **AAS lives in Docker** (`eclipsebasyx/aas-environment`) and is registered to BaSyx
  registries; the service talks to it via **MQTT**.
- **Plugin architecture** with isolated, collectible load contexts and an online,
  **signature-verified** plugin repository.
- **Secured & encrypted:** loopback admin, DPAPI secrets, TLS, signed plugins/updates
  ([STRIDE analysis](docs/STRIDE.md)).
- **Windows installer** (MSI) published to **winget**, with a built-in **auto-updater**.

## Bundled plugins

- **AV** — read/write monitor settings (resolution, refresh rate, bit depth, enabled;
  by Settings index) and audio in/out devices (volume, mute, format); one submodel
  per device.
- **Automation** — attach scripts/applications and run them from the AAS via triggers.

## Documentation

| Doc | Contents |
| --- | --- |
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Components, topology, integration model |
| [docs/INSTALL.md](docs/INSTALL.md) | Docker stack, MSI/winget install, configuration |
| [docs/STRIDE.md](docs/STRIDE.md) | Threat model and mitigations |
| [docs/PLUGIN-AUTHORING.md](docs/PLUGIN-AUTHORING.md) | Build and publish a plugin |
| [REQUIREMENTS.md](REQUIREMENTS.md) | Original requirements (ground truth) |

## Build

```bash
dotnet build Windows-AAS.slnx
dotnet test tests/WindowsAas.Tests/WindowsAas.Tests.csproj
```

Most projects target `net10.0`; `WindowsAas.Plugins.Av` targets `net10.0-windows`
(build with `EnableWindowsTargeting=true` on non-Windows CI).

## Run locally

```bash
cd deploy && docker compose up -d          # BaSyx env + registries + MQTT
dotnet run --project src/WindowsAas.Service # admin UI on http://127.0.0.1:5080
```

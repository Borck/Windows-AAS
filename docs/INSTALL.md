# Windows-AAS — Installation

## Prerequisites

- Windows 10/11 or Windows Server 2019+ (x64).
- .NET 10 runtime (bundled by the MSI; not required if you use the self-contained build).
- Docker Engine / Docker Desktop to run the BaSyx AAS environment (locally or on a
  reachable host).

## 1. Start the AAS infrastructure (Docker)

The core service connects to a BaSyx `aas-environment`, the AAS/submodel registries
and an MQTT broker. A ready-to-use compose stack is in [`deploy/`](../deploy):

```bash
cd deploy
docker compose up -d
```

This starts:

| Service | Port | Purpose |
| --- | --- | --- |
| `aas-environment` | 8081 | Hosts the AAS + submodels, emits MQTT events |
| `aas-registry` | 8082 | AAS discovery |
| `submodel-registry` | 8083 | Submodel discovery |
| `mqtt` (Mosquitto) | 1883 / 8883 | Bridge transport (plain dev / TLS prod) |

> **Production:** enable the TLS listener in `deploy/mosquitto/mosquitto.conf`,
> provide certificates under `deploy/mosquitto/certs`, require credentials, and
> point the service's `Mqtt:UseTls=true` (default) at port 8883.

## 2. Install the Windows service

### Option A — winget (recommended)

```powershell
winget install Borck.WindowsAas
```

### Option B — MSI

Download `WindowsAAS-x.y.z.msi` from the
[releases](https://github.com/Borck/Windows-AAS/releases) and run it (or silently):

```powershell
msiexec /i WindowsAAS-x.y.z.msi /qn
```

The MSI installs the **WindowsAAS** service (LocalSystem, **Automatic** start so it
runs on boot before any user logs in), writes default configuration, and restricts
the admin endpoint to the loopback interface.

## 3. Configure

Settings live in `appsettings.json` next to the service binary; secrets are stored
encrypted via DPAPI (LocalMachine). Key sections:

```jsonc
{
  "Aas":    { "EnvironmentUrl": "http://localhost:8081", "HostShellId": "urn:windows-aas:host" },
  "Mqtt":   { "Host": "localhost", "Port": 8883, "UseTls": true },
  "Repository": { "BaseUrl": "https://plugins.windows-aas.example", "TrustedPublisherKeys": [ "<PEM>" ] },
  "Updater":    { "Channel": "Manual", "FeedUrl": "https://api.github.com/repos/Borck/Windows-AAS/releases/latest" },
  "Urls":   "https://127.0.0.1:5443"
}
```

## 4. Open the admin UI

Browse to **https://127.0.0.1:5443** (loopback only). From there you can install,
enable/disable and configure plugins, read logs, and apply updates.

## Updates

- **winget:** `winget upgrade Borck.WindowsAas`
- **In-app:** the *Updates* page checks the feed, verifies the MSI's SHA-256 and
  Authenticode signature, and applies it; the service restarts automatically.
- **Channel:** set `Updater:Channel` to `Automatic` to install updates as soon as
  they are detected.

## Uninstall

```powershell
winget uninstall Borck.WindowsAas   # or: Add/Remove Programs
```

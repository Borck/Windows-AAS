# Windows-AAS — STRIDE Threat Analysis

Scope: the Windows core service, its admin UI, the host↔AAS MQTT bridge, the AAS
environment, the plugin host/plugins, the online plugin repository, and the update
channel. Each element is analysed against the six STRIDE categories with the
mitigations implemented (or planned) in this repository.

## Trust boundaries

1. Admin browser ↔ service (loopback HTTP/S).
2. Service ↔ MQTT broker ↔ AAS environment (network).
3. Service ↔ online plugin repository (internet).
4. Service ↔ update feed (internet).
5. Host process ↔ plugin code (in-process, isolated load contexts).

## Analysis

### Spoofing
- **Admin UI:** bound to `127.0.0.1` only, so remote actors cannot reach it. Add
  OS-authenticated admin (Negotiate/Windows auth) before exposing beyond loopback.
- **MQTT / AAS:** TLS with broker credentials (and optional client certs) prevents
  a rogue broker or client from impersonating the bridge.
- **Repository / updates:** packages and the MSI are verified by signature, so a
  spoofed mirror cannot supply trusted artifacts.

### Tampering
- **Plugins:** every repository package is verified (SHA-256 + RSA detached
  signature via `RsaPackageVerifier`) before extraction; unsigned/altered packages
  are rejected.
- **Updates:** the MSI's SHA-256 and Authenticode signature are validated before it
  is launched (`UpdateOrchestrator` + `WindowsAuthenticodeValidator`).
- **Config at rest:** secrets are DPAPI-encrypted (LocalMachine) so tampering or
  theft of the config file does not expose credentials.
- **In transit:** TLS on MQTT and the AAS endpoints.

### Repudiation
- Structured Serilog logging (rolling file + in-memory viewer) records plugin
  enable/disable, writes applied, automation runs, and update actions. Forward to a
  central SIEM in production for non-repudiation.

### Information disclosure
- Loopback-only admin endpoint; TLS everywhere off-box; DPAPI for secrets;
  least-privilege file ACLs on the ProgramData plugin/data directories.

### Denial of service
- Plugins run in isolated, collectible load contexts; a faulted plugin is marked
  `Faulted` and can be disabled without taking down the host. Bound the MQTT/HTTP
  client timeouts and the in-memory `LogStore` capacity (done) to cap memory.

### Elevation of privilege
- The service runs as LocalSystem (required for display/audio/device control), so
  the plugin supply chain is the key risk — hence mandatory signature verification
  and load-context isolation. Automation tasks run under the service account;
  restrict who can author `automation.json` via directory ACLs and treat task
  definitions as privileged configuration.

## Residual risks / follow-ups
- Add Windows-authenticated authorization to the admin UI for non-loopback scenarios.
- Replace the managed Authenticode check with `WinVerifyTrust` for full PE validation.
- Consider per-plugin capability scoping so a plugin cannot touch unrelated devices.
- Run automation tasks under a reduced-privilege token where the task does not need
  LocalSystem.

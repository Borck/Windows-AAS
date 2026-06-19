# Windows-AAS — Plugin Authoring

A plugin is a .NET class library that references **`WindowsAas.Abstractions`** and
implements **`IPlugin`**, packaged with a `plugin.json` manifest.

## 1. Reference the contract

Reference `WindowsAas.Abstractions` with `<Private>false</Private>` — the host
provides the shared contract assembly, and keeping a private copy would break type
unification across the plugin load context.

```xml
<ItemGroup>
  <ProjectReference Include="..\WindowsAas.Abstractions\WindowsAas.Abstractions.csproj">
    <Private>false</Private>
  </ProjectReference>
</ItemGroup>
<ItemGroup>
  <None Update="plugin.json" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

## 2. Implement `IPlugin`

```csharp
public sealed class MyPlugin : IPlugin
{
    public string Id => "vendor.my-plugin";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default) { /* capture context */ }

    public Task<IReadOnlyList<SubmodelDefinition>> GetSubmodelsAsync(CancellationToken ct = default) { /* info + capability submodels */ }

    public Task<string?> ReadAsync(string submodelId, string idShortPath, CancellationToken ct = default) { /* read host property */ }

    public Task<PropertyWriteResult> WriteAsync(PropertyWriteRequest request, CancellationToken ct = default) { /* apply + return actual value */ }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

Guidelines:
- Contribute one **info/overview submodel** plus capability submodels. Use stable
  IRI submodel ids (e.g. `urn:vendor:my-plugin:info`).
- Mark elements `Writable = true` only when the host accepts AAS → host writes; keep
  hardware/identity properties read-only.
- Return the **actual** value from `WriteAsync` (it may differ from the request); the
  host reports it back to the AAS.
- Push asynchronous/telemetry changes via `IPluginContext.ReportValueAsync`.

## 3. The manifest (`plugin.json`)

```json
{
  "id": "vendor.my-plugin",
  "name": "My Plugin",
  "version": "1.0.0",
  "entryAssembly": "Vendor.MyPlugin.dll",
  "entryType": "Vendor.MyPlugin.MyPlugin",
  "minHostVersion": "0.1.0"
}
```

## 4. Package and publish

1. Publish the plugin and zip the output (manifest + assemblies + `.deps.json`).
2. Sign the zip with your RSA private key (detached signature); the host verifies it
   against the repository's `TrustedPublisherKeys`.
3. Place `{base}.zip`, `{base}.zip.sig` and `{base}.json` metadata in the repository
   server's `packages/` directory. The server publishes them in `/index.json` and
   admins install from the **Repository** page.

For local development, drop the unpacked plugin folder directly into the host's
plugins directory (`%ProgramData%\WindowsAAS\plugins\<id>`) and use **Rescan** /
enable from the Plugins page.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using WindowsAas.Repository.Server;

var builder = WebApplication.CreateBuilder(args);

// Directory holding the {base}.zip / {base}.zip.sig / {base}.json package trios.
var packagesDir = builder.Configuration["Repository:PackagesDirectory"]
  ?? Path.Combine(builder.Environment.ContentRootPath, "packages");
Directory.CreateDirectory(packagesDir);

builder.Services.AddSingleton(new PackageCatalog(packagesDir));

var app = builder.Build();

// Catalogue: GET /index.json
app.MapGet("/index.json", (PackageCatalog catalog) => Results.Json(catalog.BuildIndex()));

// Package + signature downloads: GET /packages/{file}
app.UseStaticFiles(new StaticFileOptions
{
  FileProvider = new PhysicalFileProvider(packagesDir),
  RequestPath = "/packages",
  ServeUnknownFileTypes = true,
});

app.Run();

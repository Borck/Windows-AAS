# Build context must be the repository root (Directory.Build.props, Directory.Packages.props
# and global.json are shared across all projects):
#   docker build -f src/WindowsAas.Repository.Server/Dockerfile -t windowsaas-repository-server .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/WindowsAas.Repository.Server/WindowsAas.Repository.Server.csproj src/WindowsAas.Repository.Server/
COPY src/WindowsAas.Repository.Client/WindowsAas.Repository.Client.csproj src/WindowsAas.Repository.Client/
COPY src/WindowsAas.Security/WindowsAas.Security.csproj src/WindowsAas.Security/
RUN dotnet restore src/WindowsAas.Repository.Server/WindowsAas.Repository.Server.csproj

COPY src/WindowsAas.Repository.Server/ src/WindowsAas.Repository.Server/
COPY src/WindowsAas.Repository.Client/ src/WindowsAas.Repository.Client/
COPY src/WindowsAas.Security/ src/WindowsAas.Security/
RUN dotnet publish src/WindowsAas.Repository.Server/WindowsAas.Repository.Server.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .

# {base}.zip / {base}.zip.sig / {base}.json package trios are read from here; mount a
# volume to persist/populate them.
RUN mkdir -p /app/packages
VOLUME /app/packages

ENV ASPNETCORE_URLS=http://+:8080
ENV Repository__PackagesDirectory=/app/packages
EXPOSE 8080

ENTRYPOINT ["dotnet", "WindowsAas.Repository.Server.dll"]

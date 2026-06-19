namespace WindowsAas.Repository.Client;

/// <summary>Settings for the online plugin repository the host installs from.</summary>
public sealed class RepositoryOptions
{
  public const string SectionName = "Repository";

  /// <summary>Base URL of the repository (its index lives at <c>{BaseUrl}/index.json</c>).</summary>
  public string BaseUrl { get; set; } = "https://plugins.windows-aas.example";

  /// <summary>PEM-encoded RSA public keys trusted to sign plugin packages.</summary>
  public IList<string> TrustedPublisherKeys { get; set; } = [];
}

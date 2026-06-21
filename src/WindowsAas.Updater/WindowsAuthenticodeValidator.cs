using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WindowsAas.Updater;

/// <summary>
/// Authenticode validator that reads the certificate embedded in the signed file and
/// checks the chain and expected publisher subject.
/// <para>
/// Note: full PE signature validation should use <c>WinVerifyTrust</c>; this managed
/// check verifies the signer certificate and chain and is a pragmatic first line. On
/// non-Windows hosts it returns <c>true</c> (dev builds only).
/// </para>
/// </summary>
public sealed class WindowsAuthenticodeValidator(
  IOptions<UpdaterOptions> options,
  ILogger<WindowsAuthenticodeValidator> logger) : IAuthenticodeValidator
{
  private readonly UpdaterOptions _options = options.Value;

  public bool Validate(string path)
  {
    if (!OperatingSystem.IsWindows())
    {
      logger.LogWarning("Authenticode validation skipped: not running on Windows.");
      return true;
    }

    try
    {
      // SYSLIB0057: CreateFromSignedFile is the supported way to read the Authenticode
      // signer certificate from a signed file; the X509CertificateLoader replacement
      // does not cover signed-file extraction.
#pragma warning disable SYSLIB0057
      using var cert = new X509Certificate2(X509Certificate.CreateFromSignedFile(path));
#pragma warning restore SYSLIB0057
      if (!cert.Subject.Contains(_options.ExpectedPublisher, StringComparison.OrdinalIgnoreCase))
      {
        logger.LogError("Installer signed by unexpected publisher: {Subject}", cert.Subject);
        return false;
      }

      using var chain = new X509Chain();
      chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
      if (!chain.Build(cert))
      {
        logger.LogError("Installer certificate chain is not trusted.");
        return false;
      }

      return true;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Authenticode validation failed for {Path}.", path);
      return false;
    }
  }
}

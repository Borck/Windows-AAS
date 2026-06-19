using WindowsAas.Security;

namespace WindowsAas.Service;

/// <summary>
/// Development-only <see cref="ISecretProtector"/> used on non-Windows hosts where
/// DPAPI is unavailable. It does NOT encrypt anything and must never be used in
/// production; the shipped Windows Service always uses <c>DpapiSecretProtector</c>.
/// </summary>
internal sealed class PassthroughSecretProtector : ISecretProtector
{
  public string Protect(string plaintext) => plaintext;

  public string Unprotect(string protectedValue) => protectedValue;
}

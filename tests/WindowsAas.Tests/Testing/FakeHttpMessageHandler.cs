namespace WindowsAas.Tests.Testing;

/// <summary>Routes <see cref="HttpClient"/> calls to a callback instead of the network.</summary>
internal sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
  protected override Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request, CancellationToken cancellationToken) =>
    Task.FromResult(respond(request));
}

using System.Net.Http.Headers;
using Egov.Platform.Identity;

namespace ElectionService.Web.Services;

public sealed class BearerTokenHandler(OidcAccessTokenService accessTokenService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var accessToken = await accessTokenService.GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(accessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
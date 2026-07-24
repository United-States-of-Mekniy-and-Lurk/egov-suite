using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

namespace OrganizationRegistry.Web.Services;

public sealed class BearerTokenHandler(IHttpContextAccessor contextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var context = contextAccessor.HttpContext;
        var accessToken = context is null
            ? null
            : await context.GetTokenAsync("access_token");
        if (!string.IsNullOrWhiteSpace(accessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
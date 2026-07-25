using System.Security.Claims;
using System.Net.Http.Json;

namespace ElectionService.Api.Middleware;

public sealed class PersonIdEnrichmentMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IHttpClientFactory httpClientFactory)
    {
        if (context.User.Identity is ClaimsIdentity identity && identity.IsAuthenticated &&
            !identity.HasClaim(claim => claim.Type == "person_id"))
        {
            var authorization = context.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(authorization))
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "/me");
                request.Headers.TryAddWithoutValidation("Authorization", authorization);
                using var response = await httpClientFactory.CreateClient("PersonRegistry").SendAsync(request, context.RequestAborted);
                if (response.IsSuccessStatusCode)
                {
                    var person = await response.Content.ReadFromJsonAsync<CurrentPerson>(cancellationToken: context.RequestAborted);
                    if (person is not null) identity.AddClaim(new Claim("person_id", person.Id.ToString()));
                }
            }
        }
        await next(context);
    }

    private sealed record CurrentPerson(Guid Id);
}
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Egov.Platform.Identity;

public sealed class MkluApiAuthOptions
{
    public string ServiceName { get; set; } = string.Empty;
    public Dictionary<string, string[]> Policies { get; set; } = [];
}

public static class MkluApiAuthExtensions
{
    /// <summary>
    /// Adds JWT Bearer authentication, Keycloak claims transformation, and role-based
    /// authorization policies for an API service.
    /// <para>Configuration section: <c>Jwt:Authority</c>, <c>Jwt:Audience</c></para>
    /// </summary>
    public static IServiceCollection AddMkluApiAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<MkluApiAuthOptions>? configure = null)
    {
        var options = new MkluApiAuthOptions();
        configure?.Invoke(options);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.Authority = configuration["Jwt:Authority"];
                jwt.Audience = configuration["Jwt:Audience"];
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true
                };
                jwt.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtBearer")
                            .LogWarning(context.Exception,
                                "JWT authentication failed for {Method} {Path}; issuer={Issuer} audience={Audience}",
                                context.Request.Method, context.Request.Path, jwt.Authority, jwt.Audience);
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtBearer")
                            .LogWarning(
                                "JWT challenge for {Method} {Path}: error={Error} description={Description}",
                                context.Request.Method, context.Request.Path, context.Error, context.ErrorDescription);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(auth =>
        {
            foreach (var (policyName, roles) in options.Policies)
                auth.AddPolicy(policyName, policy => policy.RequireRole(roles));
        });

        services.AddTransient<IClaimsTransformation, KeycloakClaimsTransformation>();
        return services;
    }

    /// <summary>
    /// Adds middleware that enriches the authenticated user's claims with a <c>person_id</c>
    /// claim by calling the person registry's <c>/me</c> endpoint.
    /// <para>Requires an <see cref="HttpClient"/> named <c>"PersonRegistry"</c> registered via
    /// <see cref="IHttpClientFactory"/>.</para>
    /// </summary>
    public static IApplicationBuilder UseMkluPersonIdEnrichment(this IApplicationBuilder app) =>
        app.UseMiddleware<PersonIdEnrichmentMiddleware>();
}

public sealed class PersonIdEnrichmentMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IHttpClientFactory httpClientFactory, ILogger<PersonIdEnrichmentMiddleware> logger)
    {
        if (context.User.Identity is ClaimsIdentity identity && identity.IsAuthenticated &&
            !identity.HasClaim(claim => claim.Type == "person_id"))
        {
            var authorization = context.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(authorization))
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, "/me");
                    request.Headers.TryAddWithoutValidation("Authorization", authorization);
                    using var response = await httpClientFactory.CreateClient("PersonRegistry")
                        .SendAsync(request, context.RequestAborted);
                    if (response.IsSuccessStatusCode)
                    {
                        var person = await response.Content
                            .ReadFromJsonAsync<PersonIdResponse>(cancellationToken: context.RequestAborted);
                        if (person is not null)
                            identity.AddClaim(new Claim("person_id", person.Id.ToString()));
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to enrich person_id for {Path}", context.Request.Path);
                }
            }
        }
        await next(context);
    }

    private sealed record PersonIdResponse(Guid Id);
}

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                               ForwardedHeaders.XForwardedProto |
                               ForwardedHeaders.XForwardedHost;

    // Cloudflare/ingress proxies are dynamic in many local setups.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
})
.AddOpenIdConnect(options =>
{
    options.Authority = builder.Configuration["Oidc:Authority"];
    options.ClientId = builder.Configuration["Oidc:ClientId"];
    options.ClientSecret = builder.Configuration["Oidc:ClientSecret"];
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    // Some Keycloak setups reject PAR with generic invalid_request errors.
    // Disable PAR and use the standard authorization code flow endpoint.
    options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
    options.RequireHttpsMetadata = true;
    options.Events = new OpenIdConnectEvents
    {
        OnRedirectToIdentityProvider = context =>
        {
            var configuredPublicBaseUrl = builder.Configuration["Oidc:PublicBaseUrl"]?.TrimEnd('/');
            if (!string.IsNullOrWhiteSpace(configuredPublicBaseUrl))
            {
                context.ProtocolMessage.RedirectUri = $"{configuredPublicBaseUrl}{options.CallbackPath}";
            }

            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("OpenIdConnect");

            logger.LogInformation(
                "OIDC challenge authority={Authority} redirectUri={RedirectUri} requestScheme={Scheme} requestHost={Host} pathBase={PathBase}",
                options.Authority,
                context.ProtocolMessage.RedirectUri,
                context.Request.Scheme,
                context.Request.Host,
                context.Request.PathBase);

            return Task.CompletedTask;
        },
        OnRemoteFailure = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("OpenIdConnect");

            logger.LogWarning(
                context.Failure,
                "OIDC remote failure path={Path} query={QueryString}",
                context.Request.Path,
                context.Request.QueryString);
            return Task.CompletedTask;
        }
    };
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<BearerTokenHandler>();

builder.Services.AddHttpClient("CitizenApi", client =>
{
    var baseUrl = builder.Configuration["CitizenApi:BaseUrl"] ?? "http://citizen-service";
    client.BaseAddress = new Uri(baseUrl);
}).AddHttpMessageHandler<BearerTokenHandler>();

builder.Services.AddHttpClient("PersonRegistry", client =>
{
    var baseUrl = builder.Configuration["PersonRegistry:BaseUrl"] ?? "http://person-registry";
    client.BaseAddress = new Uri(baseUrl);
}).AddHttpMessageHandler<BearerTokenHandler>();

var app = builder.Build();

app.UseForwardedHeaders();

app.Use(async (context, next) =>
{
    app.Logger.LogInformation(
        "Incoming request {Method} {Path} scheme={Scheme} host={Host} x-forwarded-proto={XForwardedProto} x-forwarded-host={XForwardedHost} cf-visitor={CfVisitor}",
        context.Request.Method,
        context.Request.Path + context.Request.QueryString,
        context.Request.Scheme,
        context.Request.Host,
        context.Request.Headers["X-Forwarded-Proto"].ToString(),
        context.Request.Headers["X-Forwarded-Host"].ToString(),
        context.Request.Headers["CF-Visitor"].ToString());

    await next();
});

var publicBaseUrl = builder.Configuration["Oidc:PublicBaseUrl"];
if (Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var parsedPublicBaseUrl))
{
    app.Use(async (context, next) =>
    {
        context.Request.Scheme = parsedPublicBaseUrl.Scheme;
        context.Request.Host = parsedPublicBaseUrl.IsDefaultPort
            ? new HostString(parsedPublicBaseUrl.Host)
            : new HostString(parsedPublicBaseUrl.Host, parsedPublicBaseUrl.Port);

        if (!string.IsNullOrWhiteSpace(parsedPublicBaseUrl.AbsolutePath) && parsedPublicBaseUrl.AbsolutePath != "/")
        {
            context.Request.PathBase = parsedPublicBaseUrl.AbsolutePath;
        }

        await next();
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();

public class BearerTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BearerTokenHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var accessToken = await httpContext.GetTokenAsync("access_token");
            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}


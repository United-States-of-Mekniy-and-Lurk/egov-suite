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
    options.KnownNetworks.Clear();
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
    var publicBaseUrl = builder.Configuration["Oidc:PublicBaseUrl"]?.TrimEnd('/');
    if (!string.IsNullOrWhiteSpace(publicBaseUrl))
    {
        options.Events ??= new OpenIdConnectEvents();
        options.Events.OnRedirectToIdentityProvider = context =>
        {
            context.ProtocolMessage.RedirectUri = $"{publicBaseUrl}{options.CallbackPath}";
            return Task.CompletedTask;
        };
    }
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient("CitizenApi", client =>
{
    var baseUrl = builder.Configuration["CitizenApi:BaseUrl"] ?? "http://citizen-service";
    client.BaseAddress = new Uri(baseUrl);
});

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


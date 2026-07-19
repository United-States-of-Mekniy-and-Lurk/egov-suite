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


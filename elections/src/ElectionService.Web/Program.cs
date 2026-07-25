using ElectionService.Web.Services;
using ElectionService.Web;
using Egov.Platform.Identity;
using Egov.Platform.Localization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddRazorPages()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options => options.DataAnnotationLocalizerProvider = (_, factory) =>
        factory.Create(typeof(SharedResource)));
builder.Services.AddHealthChecks();
builder.Services.AddMkluRequestLocalization(builder.Configuration);

var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(keysPath))
{
    builder.Services.AddDataProtection()
        .SetApplicationName("ElectionService.Web")
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                               ForwardedHeaders.XForwardedProto |
                               ForwardedHeaders.XForwardedHost;
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
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
})
.AddOpenIdConnect(options =>
{
    options.Authority = builder.Configuration["Oidc:Authority"];
    options.ClientId = builder.Configuration["Oidc:ClientId"];
    options.ClientSecret = builder.Configuration["Oidc:ClientSecret"];
    options.RequireHttpsMetadata = builder.Configuration.GetValue("Oidc:RequireHttpsMetadata", true);
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Events.OnTokenValidated = context =>
    {
        KeycloakClaimsTransformation.AddRolesFromAccessToken(
            context.Principal,
            context.TokenEndpointResponse?.AccessToken);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToIdentityProvider = context =>
    {
        var publicBaseUrl = builder.Configuration["Oidc:PublicBaseUrl"]?.TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(publicBaseUrl))
            context.ProtocolMessage.RedirectUri = $"{publicBaseUrl}{options.CallbackPath}";

        context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("OpenIdConnect")
            .LogInformation(
                "OIDC challenge client={ClientId} redirectUri={RedirectUri} requestScheme={Scheme} requestHost={Host}",
                options.ClientId,
                context.ProtocolMessage.RedirectUri,
                context.Request.Scheme,
                context.Request.Host);
        return Task.CompletedTask;
    };
    options.Events.OnRemoteFailure = context =>
    {
        context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("OpenIdConnect")
            .LogWarning(
                context.Failure,
                "OIDC remote failure path={Path} client={ClientId}; restart sign-in from the portal instead of reusing the callback URL",
                context.Request.Path,
                options.ClientId);
        context.HandleResponse();
        context.Response.Redirect("/Error");
        return Task.CompletedTask;
    };
});
builder.Services.AddTransient<IClaimsTransformation, KeycloakClaimsTransformation>();
builder.Services.AddAuthorization(options =>
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("election-service:admin")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<OidcAccessTokenService>();
builder.Services.AddTransient<BearerTokenHandler>();
builder.Services.AddHttpClient<PublicElectionClient>(ConfigureElectionApi);
builder.Services.AddHttpClient<ManagedElectionClient>(ConfigureElectionApi)
    .AddHttpMessageHandler<BearerTokenHandler>();
builder.Services.AddSingleton(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var client = new HttpClient
    {
        BaseAddress = new Uri(configuration["ElectionApi:BaseUrl"] ?? "http://election-service-api"),
        Timeout = TimeSpan.FromSeconds(15)
    };
    return new InvitationElectionClient(client);
});

var app = builder.Build();

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRequestLocalization();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapHealthChecks("/health");

app.Run();

void ConfigureElectionApi(HttpClient client)
{
    var configuration = builder.Configuration;
    client.BaseAddress = new Uri(configuration["ElectionApi:BaseUrl"] ?? "http://election-service-api");
    client.Timeout = TimeSpan.FromSeconds(15);
}

public partial class Program;
using Egov.Platform.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using OrganizationRegistry.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddRazorPages();
builder.Services.AddHealthChecks();

var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    builder.Services.AddDataProtection()
        .SetApplicationName("OrganizationRegistry.Web")
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
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
.AddCookie(options => options.Cookie.SecurePolicy = CookieSecurePolicy.Always)
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
        return Task.CompletedTask;
    };
});
builder.Services.AddTransient<IClaimsTransformation, KeycloakClaimsTransformation>();
builder.Services.AddAuthorization(options =>
    options.AddPolicy("RequireClerk", policy =>
        policy.RequireRole("organization-registry:clerk", "organization-registry:admin")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<BearerTokenHandler>();
builder.Services.AddHttpClient<PublicRegistryClient>(client =>
{
    var baseUrl = builder.Configuration["OrganizationApi:BaseUrl"] ?? "http://organization-registry-api";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient<ManagedRegistryClient>(client =>
{
    var baseUrl = builder.Configuration["OrganizationApi:BaseUrl"] ?? "http://organization-registry-api";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(15);
}).AddHttpMessageHandler<BearerTokenHandler>();

var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();

app.UseRequestLocalization(options =>
{
    var supportedCultures = new[] { "en", "cs" };
    options.SetDefaultCulture("en");
    options.AddSupportedCultures(supportedCultures);
    options.AddSupportedUICultures(supportedCultures);
});

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
app.MapHealthChecks("/health");

app.Run();

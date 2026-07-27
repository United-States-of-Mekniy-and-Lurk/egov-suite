using GovernmentPortal.Web.Services;
using Egov.Platform.Identity;
using Egov.Platform.Localization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSingleton<ServiceCatalog>();
builder.Services.AddTransient<IPortalModule, CitizenshipPortalModule>();
builder.Services.AddTransient<IPortalModule, OrganizationPortalModule>();

var translationsPath = builder.Configuration["Translations:Path"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "..", "..", "..", "translations");
builder.Services.AddMkluRequestLocalization(builder.Configuration);
builder.Services.AddMkluJsonLocalization(translationsPath);

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
.AddCookie(options => options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest)
.AddOpenIdConnect(options =>
{
    options.Authority = builder.Configuration["Oidc:Authority"];
    options.ClientId = builder.Configuration["Oidc:ClientId"];
    options.ClientSecret = builder.Configuration["Oidc:ClientSecret"];
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
    options.RequireHttpsMetadata = builder.Configuration.GetValue("Oidc:RequireHttpsMetadata", true);
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
});
builder.Services.AddAuthorization();
builder.Services.AddMkluOidcSessionManagement();
builder.Services.AddTransient<PortalBearerTokenHandler>();
builder.Services.AddHttpClient("CitizenApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["CitizenApi:BaseUrl"] ?? "http://citizen-service-api");
    client.Timeout = TimeSpan.FromSeconds(10);
}).AddHttpMessageHandler<PortalBearerTokenHandler>();
builder.Services.AddHttpClient("OrganizationApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["OrganizationApi:BaseUrl"] ?? "http://organization-registry-api");
    client.Timeout = TimeSpan.FromSeconds(10);
}).AddHttpMessageHandler<PortalBearerTokenHandler>();

var app = builder.Build();

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseRequestLocalization();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.MapMkluOidcSessionKeepalive();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
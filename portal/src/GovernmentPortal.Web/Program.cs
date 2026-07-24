using GovernmentPortal.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSingleton<ServiceCatalog>();
builder.Services.AddSingleton<IPortalModule, CitizenshipPortalModule>();

var translationsPath = builder.Configuration["Translations:Path"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "..", "..", "..", "translations");
builder.Services.AddSingleton<IStringLocalizer>(new JsonStringLocalizer(translationsPath));
builder.Services.AddLocalization();

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

var app = builder.Build();

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

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
app.MapRazorPages();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
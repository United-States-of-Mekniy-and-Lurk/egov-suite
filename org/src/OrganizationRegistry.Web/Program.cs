using Egov.Platform.Identity;
using Egov.Platform.Localization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using OrganizationRegistry.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddMkluRequestLocalization(builder.Configuration);
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

builder.Services.AddMkluWebAuth(builder.Configuration);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireClerk", policy =>
        policy.RequireRole("organization-registry:clerk", "organization-registry:admin"));
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireRole("organization-registry:admin"));
});

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
}).AddHttpMessageHandler<MkluBearerTokenHandler>();

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

app.UseRequestLocalization();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
app.MapMkluOidcSessionKeepalive();
app.MapHealthChecks("/health");

app.Run();

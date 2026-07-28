using ElectionService.Web.Services;
using Egov.Platform.Identity;
using Egov.Platform.Localization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
var translationsPath = builder.Configuration["Translations:Path"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "..", "..", "..", "translations");
builder.Services.AddMkluRequestLocalization(builder.Configuration);
builder.Services.AddMkluJsonLocalization(translationsPath);
builder.Services.AddRazorPages()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();
builder.Services.AddHealthChecks();

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

builder.Services.AddMkluWebAuth(builder.Configuration);
builder.Services.AddAuthorization(options =>
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("election-service:admin")));

builder.Services.AddHttpClient<PublicElectionClient>(ConfigureElectionApi);
builder.Services.AddHttpClient<ManagedElectionClient>(ConfigureElectionApi)
    .AddHttpMessageHandler<MkluBearerTokenHandler>();
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

app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/health"),
    branch => branch.UseHttpsRedirection());
app.UseStaticFiles();
app.UseRouting();
app.UseRequestLocalization();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapMkluOidcSessionKeepalive();
app.MapHealthChecks("/health");

app.Run();

void ConfigureElectionApi(HttpClient client)
{
    var configuration = builder.Configuration;
    client.BaseAddress = new Uri(configuration["ElectionApi:BaseUrl"] ?? "http://election-service-api");
    client.Timeout = TimeSpan.FromSeconds(15);
}

public partial class Program;
using System.Text.Json.Serialization;
using Egov.Platform.Feeds;
using Egov.Platform.Identity;
using Microsoft.EntityFrameworkCore;
using OrganizationRegistry.Api.Feeds;
using OrganizationRegistry.Api.Services;
using OrganizationRegistry.Application.Services;
using OrganizationRegistry.Infrastructure;
using OrganizationRegistry.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
	options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOrganizationRegistryInfrastructure(builder.Configuration);
builder.Services.AddScoped<OrganizationQueryService>();
builder.Services.AddScoped<RegistrationApplicationService>();
builder.Services.AddScoped<CorrectionService>();
builder.Services.AddScoped<HistoricalOrganizationService>();
builder.Services.AddScoped<LegalFormService>();

builder.Services.AddMkluApiAuth(builder.Configuration, options =>
{
	options.ServiceName = "organization-registry";
	options.Policies["RequireClerk"] = ["organization-registry:clerk", "organization-registry:admin"];
	options.Policies["RequireAdmin"] = ["organization-registry:admin"];
});
builder.Services.AddScopedFeedProvider<NewOrganizationsFeedProvider>();

builder.Services.AddHttpClient("PersonRegistry", client => client.BaseAddress = new Uri(
	builder.Configuration["PersonRegistry:BaseUrl"] ?? "http://ego"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseMiddleware<RegistryExceptionMiddleware>();
app.UseAuthentication();
app.UseMkluPersonIdEnrichment();
app.UseAuthorization();
app.MapControllers();
app.MapRssFeeds("/feeds");
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<OrganizationRegistryDbContext>();
	await db.Database.MigrateAsync();
}

app.Run();

public partial class Program;

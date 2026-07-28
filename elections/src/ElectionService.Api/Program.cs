using System.Text.Json.Serialization;
using Egov.Platform.Feeds;
using ElectionService.Api.Feeds;
using ElectionService.Api.Middleware;
using ElectionService.Application.Services;
using ElectionService.Infrastructure;
using ElectionService.Infrastructure.Persistence;
using Egov.Platform.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddElectionInfrastructure(builder.Configuration);
builder.Services.AddMkluApiAuth(builder.Configuration, options =>
{
    options.ServiceName = "election-service";
    options.Policies["RequireAdmin"] = ["election-service:admin"];
    options.Policies["RequireCertifier"] = ["election-service:certifier"];
});
builder.Services.AddScoped<CertificationService>();
builder.Services.AddScopedFeedProvider<ElectionsFeedProvider>();
builder.Services.AddHttpClient("PersonRegistry", client => client.BaseAddress = new Uri(
    builder.Configuration["PersonRegistry:BaseUrl"] ?? "http://ego"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ElectionExceptionMiddleware>();
app.UseAuthentication();
app.UseMkluPersonIdEnrichment();
app.UseAuthorization();
app.MapControllers();
app.MapRssFeeds("/feeds");
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ElectionDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();

public partial class Program;
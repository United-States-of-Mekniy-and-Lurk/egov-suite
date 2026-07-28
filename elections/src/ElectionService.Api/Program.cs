using System.Text.Json.Serialization;
using Egov.Platform.Feeds;
using ElectionService.Api.Feeds;
using ElectionService.Api.Middleware;
using ElectionService.Application.Services;
using ElectionService.Infrastructure;
using ElectionService.Infrastructure.Persistence;
using Egov.Platform.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddElectionInfrastructure(builder.Configuration);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.Authority = builder.Configuration["Jwt:Authority"];
    options.Audience = builder.Configuration["Jwt:Audience"];
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("election-service:admin"));
    options.AddPolicy("RequireCertifier", policy => policy.RequireRole("election-service:certifier"));
});
builder.Services.AddTransient<IClaimsTransformation, KeycloakClaimsTransformation>();
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
app.UseMiddleware<PersonIdEnrichmentMiddleware>();
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
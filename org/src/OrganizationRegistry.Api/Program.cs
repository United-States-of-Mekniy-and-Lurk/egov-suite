using System.Security.Claims;
using System.Text.Json.Serialization;
using Egov.Platform.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrganizationRegistry.Api.Services;
using OrganizationRegistry.Application.Services;
using OrganizationRegistry.Infrastructure;
using OrganizationRegistry.Infrastructure.Persistence;
using Refit;

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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
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
	options.AddPolicy("RequireClerk", policy =>
		policy.RequireRole("organization-registry:clerk", "organization-registry:admin")));
builder.Services.AddTransient<IClaimsTransformation, KeycloakClaimsTransformation>();

builder.Services.AddRefitClient<IPersonRegistryApi>()
	.ConfigureHttpClient(client => client.BaseAddress = new Uri(
		builder.Configuration["PersonRegistry:BaseUrl"] ?? "http://ego"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseMiddleware<RegistryExceptionMiddleware>();
app.UseAuthentication();
app.Use(async (context, next) =>
{
	if (context.User.Identity is ClaimsIdentity identity && identity.IsAuthenticated &&
		!identity.HasClaim(claim => claim.Type == "person_id"))
	{
		var authorization = context.Request.Headers.Authorization.ToString();
		if (!string.IsNullOrWhiteSpace(authorization))
		{
			var response = await context.RequestServices.GetRequiredService<IPersonRegistryApi>()
				.GetCurrentPersonAsync(authorization, context.RequestAborted);
			if (response.IsSuccessStatusCode && response.Content != null)
				identity.AddClaim(new Claim("person_id", response.Content.Id.ToString()));
		}
	}
	await next();
});
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<OrganizationRegistryDbContext>();
	await db.Database.MigrateAsync();
}

app.Run();

public partial class Program;

using CitizenService.Application.Interfaces;
using CitizenService.Application.Services;
using CitizenService.Infrastructure.Data;
using CitizenService.Infrastructure.Http;
using CitizenService.Infrastructure.Repositories;
using CitizenService.Infrastructure.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Refit;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CitizenService API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the ******",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

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
            ValidateIssuerSigningKey = true,
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireClerk", policy =>
        policy.RequireRole("citizen-service:clerk", "citizen-service:admin"));
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireRole("citizen-service:admin"));
});

// Extract Keycloak realm roles into standard ClaimTypes.Role claims
builder.Services.AddTransient<IClaimsTransformation, KeycloakClaimsTransformation>();

builder.Services.AddDbContext<CitizenDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ICitizenRepository, CitizenRepository>();
builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<IFormRepository, FormRepository>();
builder.Services.AddScoped<IRegistryFieldRepository, RegistryFieldRepository>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();

builder.Services.AddScoped<CitizenAppService>();
builder.Services.AddScoped<ApplicationAppService>();
builder.Services.AddScoped<RegistryFieldService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentActor, CurrentActorService>();

var personRegistryBaseUrl = builder.Configuration["PersonRegistry:BaseUrl"] ?? "http://person-registry";
builder.Services.AddRefitClient<IPersonRegistryApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(personRegistryBaseUrl));
builder.Services.AddScoped<IPersonClient, PersonClient>();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(typeof(CitizenAppService).Assembly);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (context.User.Identity is ClaimsIdentity identity &&
        identity.IsAuthenticated &&
        !identity.HasClaim(claim => claim.Type == "person_id"))
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization))
        {
            var personRegistry = context.RequestServices.GetRequiredService<IPersonRegistryApi>();
            var response = await personRegistry.GetCurrentPersonAsync(
                authorization, context.RequestAborted);
            if (response.IsSuccessStatusCode && response.Content != null)
            {
                identity.AddClaim(new Claim("person_id", response.Content.Id.ToString()));
            }
        }
    }

    await next();
});
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CitizenDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();

/// <summary>
/// Extracts Keycloak realm roles from the realm_access JWT claim
/// into standard ClaimTypes.Role so [Authorize(Roles/Policy)] works.
/// </summary>
public class KeycloakClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity == null)
            return Task.FromResult(principal);

        var realmAccess = principal.FindFirst("realm_access")?.Value;
        if (realmAccess == null)
            return Task.FromResult(principal);

        try
        {
            using var doc = JsonDocument.Parse(realmAccess);
            if (doc.RootElement.TryGetProperty("roles", out var roles))
            {
                foreach (var role in roles.EnumerateArray())
                {
                    var roleName = role.GetString();
                    if (roleName != null && !identity.HasClaim(ClaimTypes.Role, roleName))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                    }
                }
            }
        }
        catch (JsonException) { }

        return Task.FromResult(principal);
    }
}


using CitizenService.Application.Interfaces;
using CitizenService.Application.Services;
using CitizenService.Infrastructure.Data;
using CitizenService.Infrastructure.Http;
using CitizenService.Infrastructure.Repositories;
using CitizenService.Infrastructure.Services;
using Egov.Platform.Identity;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Refit;
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
        Description = "JWT Authorization header",
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

builder.Services.AddMkluApiAuth(builder.Configuration, options =>
{
    options.ServiceName = "citizen-service";
    options.Policies["RequireClerk"] = ["citizen-service:clerk", "citizen-service:admin"];
    options.Policies["RequireAdmin"] = ["citizen-service:admin"];
});

builder.Services.AddDbContext<CitizenDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ICitizenRepository, CitizenRepository>();
builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<IFormRepository, FormRepository>();
builder.Services.AddScoped<IRegistryFieldRepository, RegistryFieldRepository>();
builder.Services.AddScoped<IFieldCorrectionRepository, FieldCorrectionRepository>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddSingleton<ICitizenNumberGenerator, RandomCitizenNumberGenerator>();

builder.Services.AddScoped<CitizenAppService>();
builder.Services.AddScoped<ApplicationAppService>();
builder.Services.AddScoped<DecisionDocumentService>();
builder.Services.AddScoped<RegistryFieldService>();
builder.Services.AddScoped<FieldCorrectionService>();
builder.Services.AddSingleton<IOfficialDocumentRenderer, PdfSharpOfficialDocumentRenderer>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentActor, CurrentActorService>();

var personRegistryBaseUrl = builder.Configuration["PersonRegistry:BaseUrl"] ?? "http://ego";
builder.Services.AddRefitClient<IPersonRegistryApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(personRegistryBaseUrl));
builder.Services.AddHttpClient("PersonRegistry", client =>
    client.BaseAddress = new Uri(personRegistryBaseUrl));
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
app.UseMkluPersonIdEnrichment();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CitizenDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();


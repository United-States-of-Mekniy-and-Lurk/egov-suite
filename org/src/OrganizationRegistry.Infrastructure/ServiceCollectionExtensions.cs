using Egov.Platform.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrganizationRegistry.Application.Abstractions;
using OrganizationRegistry.Infrastructure.Persistence;
using OrganizationRegistry.Infrastructure.Services;

namespace OrganizationRegistry.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrganizationRegistryInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<OrganizationRegistryDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped<IOrganizationRegistryStore, OrganizationRegistryStore>();
        services.AddScoped<IRegistrationNumberGenerator, RegistrationNumberGenerator>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentActor, CurrentActorService>();
        return services;
    }
}
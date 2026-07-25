using ElectionService.Application.Abstractions;
using ElectionService.Application.Services;
using ElectionService.Infrastructure.Http;
using ElectionService.Infrastructure.Persistence;
using ElectionService.Infrastructure.Services;
using Egov.Platform.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ElectionService.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddElectionInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ElectionDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        services.AddOptions<VotingOptions>().Bind(configuration.GetSection(VotingOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.ActiveKeyVersion) &&
                options.CredentialHashKeys.TryGetValue(options.ActiveKeyVersion, out var activeKey) && activeKey.Length >= 32,
                "Voting:ActiveKeyVersion must identify a configured credential hash key containing at least 32 characters.")
            .Validate(options => options.CredentialHashKeys.Count > 0 &&
                options.CredentialHashKeys.All(item => !string.IsNullOrWhiteSpace(item.Key) && item.Value.Length >= 32),
                "Every voting credential hash key must have a version and contain at least 32 characters.")
            .ValidateOnStart();
        services.AddHttpContextAccessor();
        services.AddScoped<IElectionStore, ElectionStore>();
        services.AddScoped<ICredentialHashService, CredentialHashService>();
        services.AddScoped<ICurrentActor, CurrentActorService>();
        services.AddScoped<PublicElectionService>();
        services.AddScoped<VotingService>();
        services.AddScoped<AdminElectionService>();
        services.AddHttpClient<IOrganizationRegistryClient, OrganizationRegistryClient>(client =>
            client.BaseAddress = new Uri(configuration["OrganizationRegistry:BaseUrl"] ?? "http://organization-registry"));
        services.AddHttpClient<ICitizenRegistryClient, CitizenRegistryClient>(client =>
            client.BaseAddress = new Uri(configuration["CitizenRegistry:BaseUrl"] ?? "http://citizen-service"));
        services.AddHttpClient<IPersonRegistryClient, PersonRegistryClient>(client =>
            client.BaseAddress = new Uri(configuration["PersonRegistry:BaseUrl"] ?? "http://ego"));
        return services;
    }
}
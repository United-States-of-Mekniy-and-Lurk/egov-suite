using Ego.Application.Abstractions;
using Ego.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ego.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<EgoDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<IPersonRepository, PersonRepository>();
        return services;
    }
}

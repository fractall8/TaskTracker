using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.DI.Modules;

namespace Persistence.DI;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabaseModule(configuration);
        services.AddRepositoriesModule();

        return services;
    }
}

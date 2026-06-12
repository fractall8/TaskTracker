using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Repositories;

namespace Persistence.DI.Modules;

internal static class RepositoriesModule
{
    public static IServiceCollection AddRepositoriesModule(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IBoardRepository, BoardRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IColumnRepository, ColumnRepository>();
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));

        return services;
    }
}
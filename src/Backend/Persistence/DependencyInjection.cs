using Application.Interfaces;
using Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Contexts;
using Persistence.Repositories;

namespace Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var postgresConnectionString = configuration.GetConnectionString(ConnectionStrings.PostgresConnection);
        
        services.AddDbContext<TaskTrackerDbContext>(options => 
            options.UseNpgsql(postgresConnectionString));
        
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IBoardRepository, BoardRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));

        return services;
    }
}
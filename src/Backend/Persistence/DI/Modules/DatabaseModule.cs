using Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Contexts;

namespace Persistence.DI.Modules;

internal static class DatabaseModule
{
    public static IServiceCollection AddDatabaseModule(this IServiceCollection services, IConfiguration configuration)
    {
        var postgresConnectionString = configuration.GetConnectionString(ConnectionStrings.PostgresConnection)
            ?? throw new InvalidOperationException("Database connection string not found");

        services.AddDbContext<TaskTrackerDbContext>(options =>
            options.UseNpgsql(postgresConnectionString));

        return services;
    }
}

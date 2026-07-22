using System.Reflection;
using Application.Behaviors;
using Application.Interfaces.Services;
using Application.Services;
using Application.Settings;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application.DI;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var executingAssembly = Assembly.GetExecutingAssembly();

        services.Configure<FileSettings>(configuration.GetSection("FileSettings"));
        services.Configure<WorkspaceSettings>(configuration.GetSection("WorkspaceSettings"));

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(executingAssembly);

            config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(WorkspaceFeatureGuardBehavior<,>));
        });

        services.AddValidatorsFromAssembly(executingAssembly);

        services.AddScoped<IBoardAccessService, BoardAccessService>();
        services.AddScoped<IWorkspaceAccessService, WorkspaceAccessService>();

        return services;
    }
}

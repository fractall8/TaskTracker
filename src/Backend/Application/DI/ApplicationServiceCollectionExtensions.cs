using System.Reflection;
using Application.Behaviors;
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
        
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(executingAssembly);
            
            config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        });
        
        services.AddValidatorsFromAssembly(executingAssembly);
        
        return services;
    }
}
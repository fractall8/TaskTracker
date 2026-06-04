using System.Reflection;
using Application.Behaviors;
using Application.Settings;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var executingAssembly = Assembly.GetExecutingAssembly();
        
        services.Configure<FileSettings>(configuration.GetSection("FileSettings"));
        
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(executingAssembly);
            
            config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });
        
        services.AddValidatorsFromAssembly(executingAssembly);
        
        return services;
    }
}
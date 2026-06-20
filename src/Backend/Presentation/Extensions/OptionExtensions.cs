using Application.Options;
using Presentation.Options;

namespace Presentation.Extensions;

public static class OptionExtensions
{
    public static IServiceCollection AddPresentationOptions(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PaginationOptions>(
            configuration.GetSection(PaginationOptions.SectionName));

        services.Configure<FrontendOptions>(
            configuration.GetSection(FrontendOptions.SectionName));

        return services;
    }
}

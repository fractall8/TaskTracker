using Application.Interfaces.Repositories;
using Application.Interfaces.UOW;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Repositories;

namespace Persistence.DI.Modules;

internal static class RepositoriesModule
{
    public static IServiceCollection AddRepositoriesModule(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IBoardRepository, BoardRepository>();
        services.AddScoped<IBoardMemberRepository, BoardMemberRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IColumnRepository, ColumnRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<IWorkspaceMemberRepository, WorkspaceMemberRepository>();
        services.AddScoped<IWorkspaceInviteRepository, WorkspaceInviteRepository>();
        services.AddScoped<IAttachmentRepository, AttachmentRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IStripeWebhookEventRepository, StripeWebhookEventRepository>();
        services.AddScoped<IBoardCallRepository, BoardCallRepository>();
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));

        return services;
    }
}

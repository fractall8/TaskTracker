using Application.Common.Interfaces;
using Application.Interfaces.Services;
using Domain.Exceptions;
using MediatR;

namespace Application.Behaviors;

public class WorkspaceFeatureGuardBehavior<TRequest, TResponse>(
    IWorkspaceEntitlementService entitlementService)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (request is not IRequireWorkspaceFeature featureRequest)
        {
            return await next(ct);
        }

        var hasFeature = await entitlementService.HasFeatureAsync(
            featureRequest.WorkspaceId,
            featureRequest.Feature,
            ct);

        if (!hasFeature)
        {
            throw new SubscriptionFeatureRequiredException(featureRequest.Feature);
        }

        return await next(ct);
    }
}

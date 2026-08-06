using System.Collections;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Behaviors;

public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var sensitive = request is ISensitivePayload;

        if (sensitive)
        {
            logger.LogInformation("Executing {RequestName}. Payload not logged.", requestName);
        }
        else
        {
            logger.LogInformation("Executing {RequestName} with parameters: {@Request}", requestName, request);
        }

        try
        {
            var response = await next(cancellationToken);

            if (sensitive)
            {
                logger.LogInformation("Completed {RequestName}. Response not logged.", requestName);
            }
            else if (response is ICollection collection)
            {
                logger.LogInformation("Completed {RequestName}. Returned {Type} with {Count} items.",
                    requestName, response.GetType().Name, collection.Count);
            }
            else
            {
                logger.LogInformation("Completed {RequestName}. Returned: {@Response}", requestName, response);
            }

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing {RequestName}", requestName);
            throw;
        }
    }
}

using Refit;

namespace Services.Extensions;

public static class ApiResponseExtensions
{
    public static async Task<T> HandleResponseAsync<T>(this IApiResponse<T> response)
    {
        if (response.IsSuccessStatusCode && response.Content != null)
        {
            return response.Content;
        }

        throw new Exception(await ExtractErrorMessageAsync(response.Error));
    }

    public static async Task HandleResponseAsync(this IApiResponse response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw new Exception(await ExtractErrorMessageAsync(response.Error));
    }

    private static async Task<string> ExtractErrorMessageAsync(ApiExceptionBase? error)
    {
        if (error == null)
        {
            return "An unknown network error occurred.";
        }

        try
        {
            if (error is ApiException apiException)
            {
                var problem = await apiException.GetContentAsAsync<ApiProblemDetails>();

                if (problem != null)
                {
                    if (problem.Errors != null && problem.Errors.Any())
                    {
                        return problem.Errors.First().Value.FirstOrDefault() ?? "Validation error.";
                    }

                    if (!string.IsNullOrWhiteSpace(problem.Detail))
                    {
                        return problem.Detail;
                    }

                    if (!string.IsNullOrWhiteSpace(problem.Title))
                    {
                        return problem.Title;
                    }
                }

                if (!string.IsNullOrWhiteSpace(apiException.Content))
                {
                    return apiException.Content;
                }
            }
        }
        catch
        {
            // Ignore
        }

        return $"An error occurred connecting to the server: {error.Message}";
    }
}

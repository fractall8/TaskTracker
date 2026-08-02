using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Domain.Exceptions;
using Infrastructure.Ai.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Ai;

internal class FaqKnowledgeSearch(
    SearchClient searchClient,
    IOptions<AzureAiSearchOptions> options,
    ILogger<FaqKnowledgeSearch> logger) : IFaqKnowledgeSearch
{
    private readonly AzureAiSearchOptions _options = options.Value;

    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(string question, CancellationToken ct = default)
    {
        var response = await SearchWithRetryAsync(question, ct);

        var chunks = new List<RetrievedChunk>();

        await foreach (var result in response.GetResultsAsync().WithCancellation(ct))
        {
            if (result.SemanticSearch?.RerankerScore is not { } rerankerScore
                || rerankerScore < _options.MinRerankerScore)
            {
                continue;
            }

            var content = GetString(result.Document, _options.ContentFieldName);

            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            chunks.Add(new RetrievedChunk(
                content,
                GetString(result.Document, _options.TitleFieldName) ?? string.Empty,
                rerankerScore));
        }

        logger.LogDebug(
            "FAQ search returned {SurvivingCount} chunk(s) at or above reranker score {MinScore}.",
            chunks.Count,
            _options.MinRerankerScore);

        return chunks;
    }

    private async Task<SearchResults<SearchDocument>> SearchWithRetryAsync(string question, CancellationToken ct)
    {
        RequestFailedException? lastFailure = null;

        for (var attempt = 1; attempt <= _options.MaxRetryAttempts; attempt++)
        {
            try
            {
                var response = await searchClient.SearchAsync<SearchDocument>(
                    question,
                    BuildSearchOptions(question),
                    ct);

                return response.Value;
            }
            catch (RequestFailedException ex) when (IsTransient(ex))
            {
                lastFailure = ex;

                logger.LogWarning(
                    ex,
                    "FAQ search attempt {Attempt}/{MaxAttempts} failed with status {Status}.",
                    attempt,
                    _options.MaxRetryAttempts,
                    ex.Status);

                if (attempt < _options.MaxRetryAttempts && _options.RetryDelayMilliseconds > 0)
                {
                    await Task.Delay(_options.RetryDelayMilliseconds * attempt, ct);
                }
            }
        }

        throw new ExternalServiceUnavailableException(
            "The assistant's knowledge base is temporarily unavailable. Please try again in a moment.",
            lastFailure!);
    }

    private SearchOptions BuildSearchOptions(string question)
    {
        var searchOptions = new SearchOptions
        {
            Size = _options.TopK,
            QueryType = SearchQueryType.Semantic,
            SemanticSearch = new SemanticSearchOptions
            {
                SemanticConfigurationName = _options.SemanticConfigurationName
            },
            VectorSearch = new VectorSearchOptions()
        };

        var vectorQuery = new VectorizableTextQuery(question)
        {
            KNearestNeighborsCount = _options.TopK
        };
        vectorQuery.Fields.Add(_options.VectorFieldName);

        searchOptions.VectorSearch.Queries.Add(vectorQuery);
        searchOptions.Select.Add(_options.ContentFieldName);
        searchOptions.Select.Add(_options.TitleFieldName);

        return searchOptions;
    }

    private static bool IsTransient(RequestFailedException ex) =>
        ex.Status is 404 or 429 or 500 or 502 or 503 or 504;

    private static string? GetString(SearchDocument document, string fieldName) =>
        document.TryGetValue(fieldName, out var value) && value is not null
            ? value as string ?? value.ToString()
            : null;
}

namespace Infrastructure.Ai;

/// <summary>
/// Retrieval half of the FAQ assistant. Internal to Infrastructure by design — Application only ever
/// sees <c>IFaqAssistantService</c>, and does not need to know the answer is retrieval-grounded.
/// </summary>
internal interface IFaqKnowledgeSearch
{
    /// <summary>
    /// Returns the chunks relevant to <paramref name="question"/>, best first. An empty result is a
    /// valid outcome meaning "nothing in the knowledge base covers this" — not an error.
    /// </summary>
    Task<IReadOnlyList<RetrievedChunk>> SearchAsync(string question, CancellationToken ct = default);
}

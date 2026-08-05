namespace Infrastructure.Ai;

internal interface IFaqKnowledgeSearch
{
    // An empty result means "nothing in the knowledge base covers this" — not an error.
    Task<IReadOnlyList<RetrievedChunk>> SearchAsync(string question, CancellationToken ct = default);
}

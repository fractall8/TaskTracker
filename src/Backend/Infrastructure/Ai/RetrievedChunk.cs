namespace Infrastructure.Ai;

/// <summary>
/// One knowledge-base chunk that cleared the relevance floor.
/// </summary>
/// <param name="Content">The chunk text, used as grounding context for the answer.</param>
/// <param name="Title">
/// Section heading for the chunk (the index's <c>header_2</c>, e.g. "Feature: Share Your Screen in a
/// Call"). Carried through retrieval so the generation step can cite a source — the document-intro
/// chunk has no heading, so this can be empty.
/// </param>
/// <param name="RerankerScore">Semantic reranker score on a 0–4 scale.</param>
internal record RetrievedChunk(string Content, string Title, double RerankerScore);

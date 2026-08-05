namespace Infrastructure.Ai;

// Title is the index's header_2; empty for the document-intro chunk. RerankerScore is on a 0–4 scale.
internal record RetrievedChunk(string Content, string Title, double RerankerScore);

namespace Infrastructure.Ai;

// Azure OpenAI rejected the request under its content management policy. A refusal, not an outage —
// surfacing it as one would tell the user the service is down when the request was simply blocked.
internal class ContentFilteredException(Exception inner)
    : Exception("The request was blocked by the content safety filter.", inner);

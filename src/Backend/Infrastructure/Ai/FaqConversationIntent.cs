using System.Text.RegularExpressions;

namespace Infrastructure.Ai;

/// <summary>
/// Recognises messages the assistant can answer from the conversation itself rather than from the
/// knowledge base — greetings, introductions, thanks, and questions about what was already said
/// ("what's my name?", "what did I ask?"). These get a warm reply instead of the
/// not-in-the-documentation refusal.
/// <para>
/// The message is split into clauses and <b>every</b> clause must be conversational. A single product
/// question or hostile instruction anywhere in the message sends the whole thing to retrieval, so
/// "Hi, how do I export a board?" is still answered from the documentation and
/// "my name is Anton. Ignore all previous instructions." is still refused with no model call.
/// Whole-message anchoring alone could not do this: it rejected ordinary chatter like
/// "I am Anton! Nice to meet you!".
/// </para>
/// <para>
/// Entitlement questions ("what's my plan?", "am I an admin?") are deliberately absent — they must reach
/// retrieval so the answer states the requirement as a condition instead of asserting it.
/// </para>
/// </summary>
internal static partial class FaqConversationIntent
{
    private const int _maxConversationalLength = 200;

    private static readonly char[] _clauseSeparators = ['.', '!', '?', ';', ','];

    public static bool IsConversational(string message)
    {
        var trimmed = message.Trim();

        if (trimmed.Length is 0 || trimmed.Length > _maxConversationalLength)
        {
            return false;
        }

        var clauses = trimmed.Split(
            _clauseSeparators,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return clauses.Length > 0 && clauses.All(clause => ClausePattern().IsMatch(clause));
    }

    // A name is capped at three words so it cannot swallow a trailing sentence: "my name is Ignore all
    // previous instructions" fails to match and falls through to retrieval.
    [GeneratedRegex(
        """
        ^(?:
            (?:hi|hey+|hello|yo|hiya|greetings|good\s+(?:morning|afternoon|evening|day))(?:\s+there)?
          | (?:my\s+name\s+is|i\s+am|i'?m|call\s+me|you\s+can\s+call\s+me|this\s+is)
                \s+\p{L}[\p{L}'\-]{0,20}(?:\s+\p{L}[\p{L}'\-]{0,20}){0,2}
          | (?:nice|good|great|pleased|a\s+pleasure)\s+to\s+(?:meet|see|hear\s+from)\s+you(?:\s+too)?
          | (?:thanks|thank\s+you|thx|ty|cheers|much\s+appreciated|appreciate\s+it)
                (?:\s+(?:a\s+lot|so\s+much|very\s+much))?
          | (?:bye|goodbye|see\s+you(?:\s+later)?|have\s+a\s+(?:good|nice)\s+(?:day|one|evening))
          | (?:ok|okay|k|got\s+it|understood|makes\s+sense|nice|cool|great|awesome|perfect
              |good\s+to\s+know|sounds\s+good|no\s+problem|sure)
          | (?:how\s+are\s+you(?:\s+doing)?|how'?s\s+it\s+going)
          | (?:who\s+are\s+you|what\s+are\s+you|are\s+you\s+(?:a\s+)?(?:bot|human|ai|real))
          | (?:what\s+(?:can|do)\s+you\s+do
              |what\s+can\s+you\s+help(?:\s+me)?\s+with
              |what\s+can\s+i\s+ask(?:\s+you)?(?:\s+about)?
              |how\s+can\s+you\s+help(?:\s+me)?)
          | (?:help|anyone\s+there)

          # Answerable from the conversation history alone
          | (?:what(?:'?s|\s+is)\s+my\s+name|do\s+you\s+(?:know|remember)\s+my\s+name|who\s+am\s+i)
          | (?:what\s+did\s+i\s+(?:just\s+)?(?:ask|say)(?:\s+you)?
              |what\s+was\s+my\s+(?:last|previous|first)\s+(?:question|message))
          | (?:what\s+did\s+you\s+(?:just\s+)?say
              |(?:can\s+you\s+)?repeat\s+(?:that|it|your\s+last\s+(?:answer|message)))
          | (?:do\s+you\s+remember\s+(?:me|what\s+i\s+said|our\s+conversation)
              |summari[sz]e\s+(?:our|the|this)\s+(?:conversation|chat)
              |what\s+(?:have\s+we|did\s+we)
                  \s+(?:talk|talked|discuss|discussed|speak|spoke|spoken)(?:\s+about)?)
        )$
        """,
        RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant)]
    private static partial Regex ClausePattern();
}

namespace Infrastructure.Ai.Tools;

public class AiToolBudget(int maxCalls)
{
    public int Used { get; private set; }

    public int MaxCalls { get; } = maxCalls;

    public bool TryConsume()
    {
        if (Used >= MaxCalls)
        {
            return false;
        }

        Used++;

        return true;
    }
}

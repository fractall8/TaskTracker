namespace WebApp.Shared.Helpers;

public static class AvatarColorHelper
{
    public static string GetColorClass(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "bg-purple";
        }

        var colors = new[] { "bg-purple", "bg-blue", "bg-teal", "bg-coral", "bg-pink" };
        int hash = GetDeterministicHashCode(name);

        return colors[Math.Abs(hash) % colors.Length];
    }

    public static string GetDotColorVar(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "var(--purple-solid)";
        }

        var colors = new[] { "var(--purple-solid)", "var(--blue-solid)", "var(--teal-solid)", "var(--coral-solid)", "var(--pink-solid)" };
        int hash = GetDeterministicHashCode(name);

        return colors[Math.Abs(hash) % colors.Length];
    }

    private static int GetDeterministicHashCode(string str)
    {
        unchecked
        {
            int hash = 5381;
            foreach (char c in str)
            {
                hash = ((hash << 5) + hash) + c;
            }

            return hash;
        }
    }
}

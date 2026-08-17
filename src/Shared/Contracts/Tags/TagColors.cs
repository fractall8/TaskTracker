namespace Contracts.Tags;

// A fixed palette rather than free hex: each name maps to a hand-picked light/dark chip pair, so a tag
// stays readable in both themes. Must stay in step with CK_Tags_Color_Palette in 0024_TagColorPalette.sql.
public static class TagColors
{
    public const string Violet = "violet";
    public const string Sky = "sky";
    public const string Teal = "teal";
    public const string Amber = "amber";
    public const string Rose = "rose";
    public const string Slate = "slate";

    public const string Default = Slate;

    public static readonly IReadOnlyList<string> All = [Violet, Sky, Teal, Amber, Rose, Slate];

    public static bool IsKnown(string? color) => color is not null && All.Contains(color);
}

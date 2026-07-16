using MudBlazor;

namespace WebApp.Theme;

public static class TaskTrackerTheme
{
    public static readonly MudTheme DefaultTheme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#5C58D6", Secondary = "#6F767E", Background = "#F9FAFB", DrawerBackground = "#F9FAFB",
            Surface = "#FFFFFF", AppbarBackground = "#F9FAFB", AppbarText = "#1A1D1F", TextPrimary = "#1A1D1F",
            TextSecondary = "#6F767E", LinesDefault = "#EFEFEF", Divider = "#EFEFEF", Info = "#5C58D6",
            Success = "#27AE60", Warning = "#F2C94C", Error = "#EB5757"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#6C63FF", Secondary = "#8C939D", Background = "#181A20", DrawerBackground = "#22242A",
            Surface = "#22242A", AppbarBackground = "#22242A", AppbarText = "#FFFFFF", TextPrimary = "#FFFFFF",
            TextSecondary = "#8C939D", LinesDefault = "#2D3038", Divider = "#2D3038", Info = "#6C63FF",
            Success = "#27AE60", Warning = "#F2C94C", Error = "#EB5757"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography { FontFamily = ["Inter", "sans-serif"] }
        }
    };
}

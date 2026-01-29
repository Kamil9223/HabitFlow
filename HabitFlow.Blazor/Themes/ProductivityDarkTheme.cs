using MudBlazor;

namespace HabitFlow.Blazor.Themes;

public static class ProductivityDarkTheme
{
    public static MudTheme Theme { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#10B981",
            Secondary = "#8B5CF6",
            Error = "#EF4444",
            Warning = "#F59E0B",
            Success = "#10B981",
            Info = "#3B82F6",
            AppbarBackground = "#171717",
            Background = "#1E1E1E",
            Surface = "#2D2D2D",
            DrawerBackground = "#171717",
            DrawerText = "#E5E5E5",
            DrawerIcon = "#A3A3A3",
            TextPrimary = "#E5E5E5",
            TextSecondary = "#A3A3A3",
            TextDisabled = "#6B6B6B",
            ActionDefault = "#E5E5E5",
            ActionDisabled = "#6B6B6B",
            ActionDisabledBackground = "#3A3A3A",
            Divider = "#3A3A3A",
            DividerLight = "#2D2D2D",
            TableLines = "#3A3A3A",
            LinesDefault = "#3A3A3A",
            LinesInputs = "#3A3A3A",
            GrayDefault = "#A3A3A3",
            GrayLight = "#6B6B6B",
            GrayLighter = "#3A3A3A",
            GrayDark = "#2D2D2D",
            GrayDarker = "#1E1E1E",
            OverlayDark = "rgba(30,30,30,0.75)",
            OverlayLight = "rgba(255,255,255,0.05)",
            HoverOpacity = 0.08,
            RippleOpacity = 0.1
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#10B981",
            Secondary = "#8B5CF6",
            Error = "#EF4444",
            Warning = "#F59E0B",
            Success = "#10B981",
            Info = "#3B82F6",
            AppbarBackground = "#171717",
            Background = "#1E1E1E",
            Surface = "#2D2D2D",
            DrawerBackground = "#171717",
            DrawerText = "#E5E5E5",
            DrawerIcon = "#A3A3A3",
            TextPrimary = "#E5E5E5",
            TextSecondary = "#A3A3A3",
            TextDisabled = "#6B6B6B",
            ActionDefault = "#E5E5E5",
            ActionDisabled = "#6B6B6B",
            ActionDisabledBackground = "#3A3A3A",
            Divider = "#3A3A3A",
            DividerLight = "#2D2D2D",
            TableLines = "#3A3A3A",
            LinesDefault = "#3A3A3A",
            LinesInputs = "#3A3A3A",
            GrayDefault = "#A3A3A3",
            GrayLight = "#6B6B6B",
            GrayLighter = "#3A3A3A",
            GrayDark = "#2D2D2D",
            GrayDarker = "#1E1E1E",
            OverlayDark = "rgba(30,30,30,0.85)",
            OverlayLight = "rgba(255,255,255,0.05)",
            HoverOpacity = 0.1,
            RippleOpacity = 0.12
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px",
            DrawerWidthLeft = "260px",
            DrawerWidthRight = "300px",
            AppbarHeight = "64px"
        },

        Shadows = new Shadow
        {
            Elevation = new[]
            {
                "none",
                "0 1px 2px 0 rgba(0,0,0,0.3)",
                "0 2px 4px 0 rgba(0,0,0,0.35)",
                "0 4px 6px 0 rgba(0,0,0,0.4)",
                "0 6px 12px 0 rgba(0,0,0,0.45)",
                "0 8px 16px 0 rgba(0,0,0,0.5)",
                "0 10px 20px 0 rgba(0,0,0,0.55)",
                "0 12px 24px 0 rgba(0,0,0,0.6)",
                "0 14px 28px 0 rgba(0,0,0,0.65)",
                "0 16px 32px 0 rgba(0,0,0,0.7)",
                "0 18px 36px 0 rgba(0,0,0,0.75)",
                "0 20px 40px 0 rgba(0,0,0,0.8)",
                "0 22px 44px 0 rgba(0,0,0,0.85)",
                "0 24px 48px 0 rgba(0,0,0,0.9)",
                "0 26px 52px 0 rgba(0,0,0,0.95)",
                "0 28px 56px 0 rgba(0,0,0,1)",
                "0 30px 60px 0 rgba(0,0,0,1)",
                "0 32px 64px 0 rgba(0,0,0,1)",
                "0 34px 68px 0 rgba(0,0,0,1)",
                "0 36px 72px 0 rgba(0,0,0,1)",
                "0 38px 76px 0 rgba(0,0,0,1)",
                "0 40px 80px 0 rgba(0,0,0,1)",
                "0 42px 84px 0 rgba(0,0,0,1)",
                "0 44px 88px 0 rgba(0,0,0,1)",
                "0 46px 92px 0 rgba(0,0,0,1)",
                "0 48px 96px 0 rgba(0,0,0,1)"
            }
        },
        ZIndex = new ZIndex
        {
            Drawer = 1100,
            AppBar = 1200,
            Dialog = 1300,
            Popover = 1400,
            Snackbar = 1500,
            Tooltip = 1600
        }
    };
}

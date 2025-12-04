using AppColetor.Services.Interfaces;

namespace AppColetor.Services.Implementations;

public class ThemeService : IThemeService
{
    private const string ThemeKey = "app_theme";

    public AppTheme CurrentTheme => Application.Current.RequestedTheme;

    public void SetTheme(AppTheme theme)
    {
        Application.Current.UserAppTheme = theme;
        Preferences.Set(ThemeKey, (int)theme);
    }

    public void ToggleTheme()
    {
        var newTheme = CurrentTheme == AppTheme.Dark
            ? AppTheme.Light
            : AppTheme.Dark;
        SetTheme(newTheme);
    }

    public void LoadSavedTheme()
    {
        var savedTheme = Preferences.Get(ThemeKey, (int)AppTheme.Unspecified);
        if (savedTheme != (int)AppTheme.Unspecified)
        {
            Application.Current.UserAppTheme = (AppTheme)savedTheme;
        }
    }
}

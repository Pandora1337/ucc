using Microsoft.JSInterop;
using ucc.Data;

namespace ucc.Services;

public class ThemeService(IJSRuntime jSRuntime, LocalStorage localStorage)
{
    private IJSRuntime JS = jSRuntime;
    private LocalStorage LS = localStorage;

    public Themes Theme { get; private set; } = Themes.Auto;
    public enum Themes
    {
        Auto,
        Light,
        Dark,
    }

    public async Task InitializeAsync()
    {
        Theme = await LS.Get<Themes>("theme");
        await ChangeTheme(Theme);
    }

    public event Action<Themes>? OnThemeChange;

    public bool IsLight => Theme == Themes.Light;
    public bool IsDark => Theme == Themes.Dark;
    public bool IsAuto => Theme == Themes.Auto;

    public async Task SelectTheme(Themes theme)
    {
        if (Theme == theme)
            return;

        Theme = theme;
        await ChangeTheme(theme);
    }

    private async Task ChangeTheme(Themes theme)
    {
        Themes privTheme = theme;
        if (privTheme == Themes.Auto)
        {
            // If not matched or no preference,
            // Default is light
            bool isDark = await JS.InvokeAsync<bool>("eval", "window.matchMedia('(prefers-color-scheme: dark)').matches");
            privTheme = isDark ? Themes.Dark : Themes.Light;
        }

        string themeStr = privTheme == Themes.Light ? "light" : "dark";
        await JS.InvokeVoidAsync("eval", $"document.documentElement.setAttribute('data-bs-theme', '{themeStr}')");
        await LS.Set("theme", theme);
        OnThemeChange?.Invoke(theme);
    }
}
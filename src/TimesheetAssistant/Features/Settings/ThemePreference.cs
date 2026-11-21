namespace TimesheetAssistant.Features.Settings;

public sealed class ThemePreference
{
    private const string Key = "theme";

    public bool IsLight { get; private set; }

    public event Action? Changed;

    public ThemePreference()
    {
        try
        {
            IsLight = Preferences.Default.Get(Key, "dark") == "light";
        }
        catch (Exception)
        {
            IsLight = false;
        }
    }

    public bool Toggle()
    {
        IsLight = !IsLight;
        Changed?.Invoke();

        try
        {
            Preferences.Default.Set(Key, IsLight ? "light" : "dark");

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

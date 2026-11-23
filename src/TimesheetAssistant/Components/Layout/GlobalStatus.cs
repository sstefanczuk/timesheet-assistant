namespace TimesheetAssistant.Components.Layout;

public sealed class GlobalStatus
{
    public string Message { get; private set; } = "Ready";
    public event Action? Changed;

    public void Show(string message)
    {
        Message = message;
        Changed?.Invoke();
    }

    public void Clear()
    {
        Message = string.Empty;
        Changed?.Invoke();
    }
}

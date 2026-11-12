namespace TimesheetAssistant;

public partial class App : Application
{
	private readonly Features.Workdays.StorageInitialization _storage;

	public App(Features.Workdays.StorageInitialization storage)
	{
		_storage = storage;
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new MainPage()) { Title = "TimesheetAssistant" };
		window.Destroying += (_, _) => _storage.Dispose();

		return window;
	}
}

using Microsoft.Extensions.Logging;

using TimesheetAssistant.Core.Features.Workdays;
using TimesheetAssistant.Features.Workdays;
using TimesheetAssistant.Features.Settings;

namespace TimesheetAssistant;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();
		builder.Services.AddSingleton<ThemePreference>();
		builder.Services.AddSingleton<StorageInitialization>();
		builder.Services.AddSingleton<IWorkEntryStore>(services =>
			services.GetRequiredService<StorageInitialization>().Store
			?? throw new InvalidOperationException("Local storage is unavailable."));
		builder.Services.AddScoped(services => new WorkdayService(
			services.GetRequiredService<IWorkEntryStore>(), () => DateOnly.FromDateTime(DateTime.Now)));
		builder.Services.AddScoped(services => new WorkEntryHistoryService(
			services.GetRequiredService<IWorkEntryStore>(), () => DateOnly.FromDateTime(DateTime.Now)));

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}

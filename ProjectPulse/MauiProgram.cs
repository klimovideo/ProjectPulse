using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using ProjectPulse.Services;
using ProjectPulse.Database;
using ProjectPulse.ViewModels;
using ProjectPulse.Views;
using ProjectPulse.Helpers;
using Syncfusion.Maui.Core.Hosting;
using ProjectPulse.Models;
using Microsoft.EntityFrameworkCore;

namespace ProjectPulse;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureSyncfusionCore()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
			});

		builder.Services.AddDbContext<ProjectPulseContext>(options =>
            options.UseSqlite(
                // в MAUI путь к файлу обычно так:
                $"Data Source={Path.Combine(FileSystem.AppDataDirectory, "projectpulse.db")}"
            )
        );

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Register services
		builder.Services.AddSingleton<DatabaseService>(DatabaseService.Instance);
		builder.Services.AddSingleton<AuthService>();
		builder.Services.AddSingleton<NotificationService>();
		builder.Services.AddSingleton<ProjectService>();
		builder.Services.AddSingleton<TaskService>();
		builder.Services.AddSingleton<CalendarService>();
		builder.Services.AddSingleton<AIService>();

		// Register ViewModels
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<RegisterViewModel>();
		builder.Services.AddTransient<DashboardViewModel>();
		builder.Services.AddTransient<ProjectsViewModel>();
		builder.Services.AddTransient<ProjectDetailViewModel>();
		builder.Services.AddTransient<TasksViewModel>();
		builder.Services.AddTransient<TaskDetailViewModel>();
		builder.Services.AddTransient<ProfileViewModel>();
		builder.Services.AddTransient<NotificationsViewModel>();
		builder.Services.AddTransient<TeamPulseViewModel>();
		builder.Services.AddTransient<CalendarViewModel>();

		// Register Views
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<RegisterPage>();
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<ProjectsPage>();
		builder.Services.AddTransient<ProjectDetailPage>();
		builder.Services.AddTransient<TasksPage>();
		builder.Services.AddTransient<TaskDetailPage>();
		builder.Services.AddTransient<ProfilePage>();
		builder.Services.AddTransient<NotificationsPage>();
		builder.Services.AddTransient<TeamPulsePage>();
		builder.Services.AddTransient<CalendarPage>();

		return builder.Build();
	}
}

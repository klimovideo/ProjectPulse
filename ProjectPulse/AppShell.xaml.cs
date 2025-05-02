using ProjectPulse.Views;

namespace ProjectPulse;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		
		// Register routes for navigation
		Routing.RegisterRoute(nameof(ProjectDetailPage), typeof(ProjectDetailPage));
		Routing.RegisterRoute(nameof(TaskDetailPage), typeof(TaskDetailPage));
		Routing.RegisterRoute(nameof(TeamPulsePage), typeof(TeamPulsePage));
		Routing.RegisterRoute(nameof(NotificationsPage), typeof(NotificationsPage));
	}
}

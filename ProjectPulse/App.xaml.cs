using ProjectPulse.Database;

namespace ProjectPulse;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		
		// Initialize database
		InitializeDatabase();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
	
	private async void InitializeDatabase()
	{
		var dbService = DatabaseService.Instance;
		await dbService.InitializeAsync();
	}
}
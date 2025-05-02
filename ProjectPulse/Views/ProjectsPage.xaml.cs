using ProjectPulse.ViewModels;

namespace ProjectPulse.Views;

public partial class ProjectsPage : ContentPage
{
    private readonly ProjectsViewModel _viewModel;
    
    public ProjectsPage(ProjectsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnAppearing();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnDisappearing();
    }
}

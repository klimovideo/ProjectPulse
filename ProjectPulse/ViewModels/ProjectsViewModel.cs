using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectPulse.Models;
using ProjectPulse.Services;
using ProjectPulse.Views;
using System.Collections.ObjectModel;
using Task = System.Threading.Tasks.Task;

namespace ProjectPulse.ViewModels
{
    public partial class ProjectsViewModel : BaseViewModel
    {
        private readonly ProjectService _projectService;
        private readonly AuthService _authService;

        [ObservableProperty]
        private ObservableCollection<Project> projects;
        
        [ObservableProperty]
        private ObservableCollection<Project> activeProjects;
        
        [ObservableProperty]
        private ObservableCollection<Project> completedProjects;
        
        [ObservableProperty]
        private ObservableCollection<Project> archivedProjects;
        
        [ObservableProperty]
        private string searchText;
        
        [ObservableProperty]
        private bool isFiltering;
        
        [ObservableProperty]
        private bool showActive;
        
        [ObservableProperty]
        private bool showCompleted;
        
        [ObservableProperty]
        private bool showArchived;
        
        [ObservableProperty]
        private bool hasActiveProjects;
        
        [ObservableProperty]
        private bool hasCompletedProjects;
        
        [ObservableProperty]
        private bool hasArchivedProjects;
        
        public ProjectsViewModel(ProjectService projectService, AuthService authService)
        {
            _projectService = projectService;
            _authService = authService;
            
            Title = "Проекты";
            Projects = new ObservableCollection<Project>();
            ActiveProjects = new ObservableCollection<Project>();
            CompletedProjects = new ObservableCollection<Project>();
            ArchivedProjects = new ObservableCollection<Project>();
            SearchText = string.Empty;
            
            ShowActive = true;
            ShowCompleted = true;
            ShowArchived = false;
        }
        
        public override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadProjects();
        }

        [RelayCommand]
        public async Task LoadProjects()
        {
            if (IsBusy)
                return;
                
            try
            {
                IsBusy = true;
                ClearError();
                
                var currentUser = this._authService.CurrentUser;
                if (currentUser == null)
                    return;
                    
                var allProjects = await _projectService.GetProjectsByUserAsync(currentUser.Id);
                
                Projects.Clear();
                ActiveProjects.Clear();
                CompletedProjects.Clear();
                ArchivedProjects.Clear();
                
                // Filter and categorize projects
                foreach (var project in allProjects)
                {
                    Projects.Add(project);
                    
                    switch (project.Status)
                    {
                        case ProjectStatus.New:
                        case ProjectStatus.InProgress:
                        case ProjectStatus.OnHold:
                            ActiveProjects.Add(project);
                            break;
                        case ProjectStatus.Completed:
                            CompletedProjects.Add(project);
                            break;
                        case ProjectStatus.Archived:
                            ArchivedProjects.Add(project);
                            break;
                    }
                }
                
                HasActiveProjects = ActiveProjects.Any();
                HasCompletedProjects = CompletedProjects.Any();
                HasArchivedProjects = ArchivedProjects.Any();
                
                // Apply search filter if text exists
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    ApplySearchFilter();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при загрузке проектов: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        public async Task CreateProject()
        {
            await Shell.Current.GoToAsync($"{nameof(ProjectDetailPage)}?IsNew=true");
        }

        [RelayCommand]
        public async Task ViewProjectDetails(Project project)
        {
            if (project == null)
                return;
                
            var parameters = new ShellNavigationQueryParameters
            {
                { "ProjectId", project.Id }
            };
            
            await Shell.Current.GoToAsync($"{nameof(ProjectDetailPage)}", parameters);
        }

        [RelayCommand]
        public void ToggleFilterOptions()
        {
            IsFiltering = !IsFiltering;
        }

        [RelayCommand]
        public async Task ApplyFilters()
        {
            await LoadProjects();
            IsFiltering = false;
        }

        [RelayCommand]
        public void ApplySearchFilter()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                // Reset to original lists
                LoadProjectsCommand.Execute(null);
                return;
            }
            
            var searchLower = SearchText.ToLower();
            
            // Filter active projects
            if (ShowActive)
            {
                var filteredActive = ActiveProjects.Where(p => 
                    p.Name.ToLower().Contains(searchLower) || 
                    p.Description?.ToLower().Contains(searchLower) == true).ToList();
                    
                ActiveProjects.Clear();
                foreach (var project in filteredActive)
                {
                    ActiveProjects.Add(project);
                }
                
                HasActiveProjects = ActiveProjects.Any();
            }
            
            // Filter completed projects
            if (ShowCompleted)
            {
                var filteredCompleted = CompletedProjects.Where(p => 
                    p.Name.ToLower().Contains(searchLower) || 
                    p.Description?.ToLower().Contains(searchLower) == true).ToList();
                    
                CompletedProjects.Clear();
                foreach (var project in filteredCompleted)
                {
                    CompletedProjects.Add(project);
                }
                
                HasCompletedProjects = CompletedProjects.Any();
            }
            
            // Filter archived projects
            if (ShowArchived)
            {
                var filteredArchived = ArchivedProjects.Where(p => 
                    p.Name.ToLower().Contains(searchLower) || 
                    p.Description?.ToLower().Contains(searchLower) == true).ToList();
                    
                ArchivedProjects.Clear();
                foreach (var project in filteredArchived)
                {
                    ArchivedProjects.Add(project);
                }
                
                HasArchivedProjects = ArchivedProjects.Any();
            }
        }

        [RelayCommand]
        public async Task Refresh()
        {
            IsRefreshing = true;
            await LoadProjects();
        }
        
        public override void OnDisappearing()
        {
            base.OnDisappearing();
            // Reset filtering state
            ShowActive = true;
            ShowCompleted = true;
            ShowArchived = false;
            IsFiltering = false;
            SearchText = string.Empty;
        }
        
        [RelayCommand]
        private void ResetFilters()
        {
            // Reset filtering state
            ShowActive = true;
            ShowCompleted = true;
            ShowArchived = false;
            IsFiltering = false;
            SearchText = string.Empty;
        }
    }
}

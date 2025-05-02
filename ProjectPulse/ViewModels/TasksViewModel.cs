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
    public partial class TasksViewModel : BaseViewModel
    {
        private readonly TaskService _taskService;
        private readonly ProjectService _projectService;
        private readonly AuthService _authService;

        [ObservableProperty]
        private ObservableCollection<ProjectTask> tasks;
        
        [ObservableProperty]
        private ObservableCollection<ProjectTask> pendingTasks;
        
        [ObservableProperty]
        private ObservableCollection<ProjectTask> inProgressTasks;
        
        [ObservableProperty]
        private ObservableCollection<ProjectTask> completedTasks;
        
        [ObservableProperty]
        private string searchText;
        
        [ObservableProperty]
        private bool isFiltering;
        
        [ObservableProperty]
        private bool showPending;
        
        [ObservableProperty]
        private bool showInProgress;
        
        [ObservableProperty]
        private bool showCompleted;
        
        [ObservableProperty]
        private Guid? selectedProjectId;
        
        [ObservableProperty]
        private ObservableCollection<Project> userProjects;
        
        [ObservableProperty]
        private Project selectedProject;
        
        [ObservableProperty]
        private bool hasPendingTasks;
        
        [ObservableProperty]
        private bool hasInProgressTasks;
        
        [ObservableProperty]
        private bool hasCompletedTasks;
        
        public TasksViewModel(TaskService taskService, ProjectService projectService, AuthService authService)
        {
            _taskService = taskService;
            _projectService = projectService;
            _authService = authService;
            
            Title = "Задачи";
            Tasks = new ObservableCollection<ProjectTask>();
            PendingTasks = new ObservableCollection<ProjectTask>();
            InProgressTasks = new ObservableCollection<ProjectTask>();
            CompletedTasks = new ObservableCollection<ProjectTask>();
            UserProjects = new ObservableCollection<Project>();
            SearchText = string.Empty;
            
            ShowPending = true;
            ShowInProgress = true;
            ShowCompleted = true;
        }
        
        public override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadUserProjects();
            await LoadTasks();
        }

        [RelayCommand]
        public async Task LoadUserProjects()
        {
            if (IsBusy)
                return;
                
            try
            {
                var currentUser = AuthService.CurrentUser;
                if (currentUser == null)
                    return;
                    
                var projects = await _projectService.GetProjectsByUserAsync(currentUser.Id);
                
                UserProjects.Clear();
                foreach (var project in projects)
                {
                    UserProjects.Add(project);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при загрузке проектов: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task LoadTasks()
        {
            if (IsBusy)
                return;
                
            try
            {
                IsBusy = true;
                ClearError();
                
                var currentUser = AuthService.CurrentUser;
                if (currentUser == null)
                    return;
                    
                var allTasks = SelectedProjectId.HasValue
                    ? await _taskService.GetTasksByProjectAsync(SelectedProjectId.Value)
                    : await _taskService.GetTasksByUserAsync(currentUser.Id);
                
                Tasks.Clear();
                PendingTasks.Clear();
                InProgressTasks.Clear();
                CompletedTasks.Clear();
                
                // Filter and categorize tasks
                foreach (var task in allTasks)
                {
                    Tasks.Add(task);
                    
                    switch (task.Status)
                    {
                        case ProjectStatus.New:
                        case ProjectStatus.Planned:
                            PendingTasks.Add(task);
                            break;
                        case ProjectStatus.InProgress:
                        case ProjectStatus.OnHold:
                        case ProjectStatus.Blocked:
                            InProgressTasks.Add(task);
                            break;
                        case ProjectStatus.Completed:
                        case ProjectStatus.Cancelled:
                            CompletedTasks.Add(task);
                            break;
                    }
                }
                
                HasPendingTasks = PendingTasks.Any();
                HasInProgressTasks = InProgressTasks.Any();
                HasCompletedTasks = CompletedTasks.Any();
                
                // Apply search filter if text exists
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    ApplySearchFilter();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при загрузке задач: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        public async Task CreateTask()
        {
            await Shell.Current.GoToAsync($"{nameof(TaskDetailPage)}?IsNew=true");
        }

        [RelayCommand]
        public async Task ViewTaskDetails(ProjectTask task)
        {
            if (task == null)
                return;
                
            var parameters = new ShellNavigationQueryParameters
            {
                { "TaskId", task.Id }
            };
            
            await Shell.Current.GoToAsync($"{nameof(TaskDetailPage)}", parameters);
        }

        [RelayCommand]
        public void ToggleFilterOptions()
        {
            IsFiltering = !IsFiltering;
        }

        [RelayCommand]
        public async Task ApplyFilters()
        {
            await LoadTasks();
            IsFiltering = false;
        }

        [RelayCommand]
        public void ApplySearchFilter()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                // Reset to original lists
                LoadTasksCommand.Execute(null);
                return;
            }
            
            var searchLower = SearchText.ToLower();
            
            // Filter pending tasks
            if (ShowPending)
            {
                var filteredPending = PendingTasks.Where(t => 
                    t.Title.ToLower().Contains(searchLower) || 
                    t.Description?.ToLower().Contains(searchLower) == true).ToList();
                    
                PendingTasks.Clear();
                foreach (var task in filteredPending)
                {
                    PendingTasks.Add(task);
                }
                
                HasPendingTasks = PendingTasks.Any();
            }
            
            // Filter in-progress tasks
            if (ShowInProgress)
            {
                var filteredInProgress = InProgressTasks.Where(t => 
                    t.Title.ToLower().Contains(searchLower) || 
                    t.Description?.ToLower().Contains(searchLower) == true).ToList();
                    
                InProgressTasks.Clear();
                foreach (var task in filteredInProgress)
                {
                    InProgressTasks.Add(task);
                }
                
                HasInProgressTasks = InProgressTasks.Any();
            }
            
            // Filter completed tasks
            if (ShowCompleted)
            {
                var filteredCompleted = CompletedTasks.Where(t => 
                    t.Title.ToLower().Contains(searchLower) || 
                    t.Description?.ToLower().Contains(searchLower) == true).ToList();
                    
                CompletedTasks.Clear();
                foreach (var task in filteredCompleted)
                {
                    CompletedTasks.Add(task);
                }
                
                HasCompletedTasks = CompletedTasks.Any();
            }
        }

        [RelayCommand]
        public async Task Refresh()
        {
            IsRefreshing = true;
            await LoadTasks();
        }
        
        [RelayCommand]
        public async Task FilterByProject(Project project)
        {
            SelectedProject = project;
            SelectedProjectId = project?.Id;
            await LoadTasks();
        }
        
        [RelayCommand]
        public async Task ClearProjectFilter()
        {
            SelectedProject = null;
            SelectedProjectId = null;
            await LoadTasks();
        }
        
        public override void OnDisappearing()
        {
            base.OnDisappearing();
            // Reset filtering state
            ShowPending = true;
            ShowInProgress = true;
            ShowCompleted = true;
            IsFiltering = false;
            SearchText = string.Empty;
            SelectedProject = null;
            SelectedProjectId = null;
        }
    }
}

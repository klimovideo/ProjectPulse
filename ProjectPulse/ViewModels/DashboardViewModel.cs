using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using ProjectPulse.Models;
using ProjectPulse.Services;
using ProjectPulse.Views;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace ProjectPulse.ViewModels
{
    public partial class DashboardViewModel : BaseViewModel
    {
        private readonly AuthService _authService;
        private readonly ProjectService _projectService;
        private readonly TaskService _taskService;
        private readonly NotificationService _notificationService;
        private readonly AIService _aiService;
        private readonly INavigation _navigation;

        [ObservableProperty]
        private string welcomeMessage;

        [ObservableProperty]
        private int unreadNotifications;

        [ObservableProperty]
        private ObservableCollection<Project> recentProjects;

        [ObservableProperty]
        private List<ProjectTask> _upcomingTasks;

        [ObservableProperty]
        private List<ProjectTask> _overdueTasks;

        [ObservableProperty]
        private int _totalTasks;

        [ObservableProperty]
        private int _completedTasks;

        [ObservableProperty]
        private string aiRecommendation;

        [ObservableProperty]
        private double completionRate;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private bool hasUpcomingTasks;

        [ObservableProperty]
        private bool hasOverdueTasks;

        [ObservableProperty]
        private bool hasRecentProjects;

        public DashboardViewModel(
            AuthService authService,
            ProjectService projectService,
            TaskService taskService,
            NotificationService notificationService,
            AIService aiService,
            INavigation navigation)
        {
            _authService = authService;
            _projectService = projectService;
            _taskService = taskService;
            _notificationService = notificationService;
            _aiService = aiService;
            _navigation = navigation;

            Title = "Дашборд";
            RecentProjects = new ObservableCollection<Project>();
        }

        public override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadDashboardDataAsync();
        }

        private async System.Threading.Tasks.Task LoadDashboardDataAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                ClearError();

                // Set welcome message
                var currentUser = AuthService.CurrentUser;
                if (currentUser != null)
                {
                    WelcomeMessage = $"Добро пожаловать, {currentUser.FirstName}!";
                }

                // Get unread notifications count
                UnreadNotifications = await _notificationService.GetUnreadNotificationCountAsync(currentUser.Id);

                // Load recent projects
                var projects = await _projectService.GetProjectsByUserAsync(currentUser.Id);
                var sortedProjects = projects
                    .OrderByDescending(p => p.ModifiedAt)
                    .Take(5)
                    .ToList();

                RecentProjects.Clear();
                foreach (var project in sortedProjects)
                {
                    RecentProjects.Add(project);
                }
                HasRecentProjects = RecentProjects.Any();

                // Get upcoming tasks (due in next 7 days)
                var userId = _authService.CurrentUser?.Id ?? 0;
                if (userId > 0)
                {
                    UpcomingTasks = await _taskService.GetTasksDueSoonAsync(userId, 7);
                    OverdueTasks = await _taskService.GetOverdueTasksAsync(userId);
                    TotalTasks = await _taskService.GetTaskCountAsync(userId);
                    CompletedTasks = await _taskService.GetCompletedTaskCountAsync(userId);
                }
                HasUpcomingTasks = UpcomingTasks.Any();
                HasOverdueTasks = OverdueTasks.Any();

                // Calculate completion rate
                CompletionRate = TotalTasks > 0 ? (double)CompletedTasks / TotalTasks * 100 : 0;

                // Get AI recommendation for optimal task scheduling
                var tasks = await _taskService.GetTasksByUserAsync(currentUser.Id);
                if (tasks.Any())
                {
                    var optimizedTasks = await _aiService.GetRecommendedTaskOrderAsync(currentUser.Id);
                    if (optimizedTasks.Any())
                    {
                        var nextTask = optimizedTasks.FirstOrDefault();
                        if (nextTask != null)
                        {
                            AiRecommendation = $"Рекомендуемая задача: {nextTask.Title}";
                        }
                    }
                }
                else
                {
                    AiRecommendation = "У вас пока нет задач. Создайте новую задачу или проект.";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private async System.Threading.Tasks.Task ViewProjectDetailsAsync(Project project)
        {
            if (IsBusy || project == null)
                return;

            try
            {
                IsBusy = true;
                var parameters = new ShellNavigationQueryParameters
                {
                    { "ProjectId", project.Id }
                };

                await Shell.Current.GoToAsync($"{nameof(ProjectDetailPage)}", parameters);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error navigating to project details: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async System.Threading.Tasks.Task ViewTaskDetailsAsync(ProjectTask task)
        {
            if (IsBusy || task == null)
                return;

            try
            {
                IsBusy = true;
                await _navigation.PushAsync(new TaskDetailPage(task));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error navigating to task details: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async System.Threading.Tasks.Task ViewNotificationsAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                await Shell.Current.GoToAsync(nameof(NotificationsPage));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error navigating to notifications: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async System.Threading.Tasks.Task RefreshAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                IsRefreshing = true;
                await LoadDashboardDataAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing dashboard: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private async System.Threading.Tasks.Task CreateNewProjectAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                await Shell.Current.GoToAsync($"{nameof(ProjectDetailPage)}?IsNew=true");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error navigating to create project: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async System.Threading.Tasks.Task CreateNewTaskAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                await _navigation.PushAsync(new TasksPage());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error navigating to create task: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async System.Threading.Tasks.Task LogoutAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                await _authService.LogoutAsync();
                await Shell.Current.GoToAsync("//LoginPage");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error logging out: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}

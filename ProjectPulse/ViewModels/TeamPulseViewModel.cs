using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectPulse.Models;
using ProjectPulse.Services;
using System.Collections.ObjectModel;
using Task = System.Threading.Tasks.Task;

namespace ProjectPulse.ViewModels
{
    public partial class TeamPulseViewModel : BaseViewModel
    {
        private readonly ProjectService _projectService;
        private readonly TaskService _taskService;
        private readonly AuthService _authService;
        
        [ObservableProperty]
        private ObservableCollection<User> teamMembers;
        
        [ObservableProperty]
        private ObservableCollection<TeamMemberStats> teamStats;
        
        [ObservableProperty]
        private Project selectedProject;
        
        [ObservableProperty]
        private ObservableCollection<Project> userProjects;
        
        [ObservableProperty]
        private bool hasTeamMembers;
        
        [ObservableProperty]
        private bool hasProjects;
        
        public TeamPulseViewModel(ProjectService projectService, TaskService taskService, AuthService authService)
        {
            _projectService = projectService;
            _taskService = taskService;
            _authService = authService;
            
            Title = "Команда";
            TeamMembers = new ObservableCollection<User>();
            TeamStats = new ObservableCollection<TeamMemberStats>();
            UserProjects = new ObservableCollection<Project>();
        }
        
        public override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadUserProjects();
            
            if (SelectedProject != null)
            {
                await LoadTeamMembers();
            }
        }
        
        [RelayCommand]
        public async Task LoadUserProjects()
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
                    
                var projects = await _projectService.GetProjectsByUserAsync(currentUser.Id);
                
                UserProjects.Clear();
                foreach (var project in projects)
                {
                    UserProjects.Add(project);
                }
                
                HasProjects = UserProjects.Any();
                
                if (HasProjects && SelectedProject == null)
                {
                    SelectedProject = UserProjects.First();
                    await LoadTeamMembers();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при загрузке проектов: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        [RelayCommand]
        public async Task LoadTeamMembers()
        {
            if (IsBusy || SelectedProject == null)
                return;
                
            try
            {
                IsBusy = true;
                ClearError();
                
                var members = await _projectService.GetProjectTeamMembersAsync(SelectedProject.Id);
                
                TeamMembers.Clear();
                TeamStats.Clear();
                
                foreach (var member in members)
                {
                    TeamMembers.Add(member);
                    
                    // Get stats for this team member
                    var memberTasks = await _taskService.GetTasksByUserAndProjectAsync(member.Id, SelectedProject.Id);
                    
                    int totalTasks = memberTasks.Count;
                    int completedTasks = memberTasks.Count(t => t.Status == ProjectStatus.Completed);
                    int inProgressTasks = memberTasks.Count(t => t.Status == ProjectStatus.InProgress);
                    int pendingTasks = memberTasks.Count(t => t.Status == ProjectStatus.New || t.Status == ProjectStatus.Planned);
                    int overdueTasks = memberTasks.Count(t => t.DueDate < DateTime.Today && t.Status != ProjectStatus.Completed);
                    
                    var stats = new TeamMemberStats
                    {
                        User = member,
                        TotalTasks = totalTasks,
                        CompletedTasks = completedTasks,
                        InProgressTasks = inProgressTasks,
                        PendingTasks = pendingTasks,
                        OverdueTasks = overdueTasks,
                        CompletionRate = totalTasks > 0 ? (double)completedTasks / totalTasks : 0
                    };
                    
                    TeamStats.Add(stats);
                }
                
                HasTeamMembers = TeamMembers.Any();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при загрузке команды: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                IsRefreshing = false;
            }
        }
        
        [RelayCommand]
        public async Task SelectProject(Project project)
        {
            if (project == null || project.Id == SelectedProject?.Id)
                return;
                
            SelectedProject = project;
            await LoadTeamMembers();
        }
        
        [RelayCommand]
        public async Task Refresh()
        {
            IsRefreshing = true;
            await LoadTeamMembers();
        }
    }
    
    public class TeamMemberStats
    {
        public User User { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int PendingTasks { get; set; }
        public int OverdueTasks { get; set; }
        public double CompletionRate { get; set; }
    }
}

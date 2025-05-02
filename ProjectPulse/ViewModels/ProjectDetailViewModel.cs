using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectPulse.Models;
using ProjectPulse.Services;
using Task = System.Threading.Tasks.Task;

namespace ProjectPulse.ViewModels
{
    public partial class ProjectDetailViewModel : BaseViewModel
    {
        private readonly ProjectService _projectService;
        private readonly AuthService _authService;
        
        [ObservableProperty]
        private Project project;
        
        [ObservableProperty]
        private bool isNew;
        
        [ObservableProperty]
        private bool isEditing;
        
        [ObservableProperty]
        private string projectName;
        
        [ObservableProperty]
        private string projectDescription;
        
        [ObservableProperty]
        private DateTime startDate;
        
        [ObservableProperty]
        private DateTime? endDate;
        
        [ObservableProperty]
        private ProjectStatus status;
        
        [ObservableProperty]
        private ProjectPriority priority;
        
        public ProjectDetailViewModel(ProjectService projectService, AuthService authService)
        {
            _projectService = projectService;
            _authService = authService;
            
            Title = "Проект";
            Project = new Project();
            StartDate = DateTime.Today;
            Status = ProjectStatus.New;
            Priority = ProjectPriority.Medium;
        }
        
        public override async void OnAppearing()
        {
            base.OnAppearing();
            
            if (!IsNew && Project.Id != Guid.Empty)
            {
                await LoadProject(Project.Id);
            }
        }
        
        [RelayCommand]
        public async Task LoadProject(Guid projectId)
        {
            if (IsBusy)
                return;
                
            try
            {
                IsBusy = true;
                ClearError();
                
                Project = await _projectService.GetProjectByIdAsync(projectId);
                
                if (Project != null)
                {
                    ProjectName = Project.Name;
                    ProjectDescription = Project.Description;
                    StartDate = Project.StartDate;
                    EndDate = Project.EndDate;
                    Status = Project.Status;
                    Priority = Project.Priority;
                    
                    Title = Project.Name;
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при загрузке проекта: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        [RelayCommand]
        public async Task SaveProject()
        {
            if (IsBusy)
                return;
                
            if (string.IsNullOrWhiteSpace(ProjectName))
            {
                ShowError("Название проекта не может быть пустым");
                return;
            }
            
            try
            {
                IsBusy = true;
                ClearError();
                
                var currentUser = AuthService.CurrentUser;
                if (currentUser == null)
                {
                    ShowError("Пользователь не авторизован");
                    return;
                }
                
                if (IsNew)
                {
                    Project = new Project
                    {
                        Id = Guid.NewGuid(),
                        Name = ProjectName,
                        Description = ProjectDescription,
                        StartDate = StartDate,
                        EndDate = EndDate,
                        Status = Status,
                        Priority = Priority,
                        CreatedBy = currentUser.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    
                    await _projectService.AddProjectAsync(Project);
                }
                else
                {
                    Project.Name = ProjectName;
                    Project.Description = ProjectDescription;
                    Project.StartDate = StartDate;
                    Project.EndDate = EndDate;
                    Project.Status = Status;
                    Project.Priority = Priority;
                    Project.UpdatedAt = DateTime.UtcNow;
                    
                    await _projectService.UpdateProjectAsync(Project);
                }
                
                IsEditing = false;
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при сохранении проекта: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        [RelayCommand]
        public async Task DeleteProject()
        {
            if (IsBusy || IsNew)
                return;
                
            try
            {
                IsBusy = true;
                ClearError();
                
                bool confirmed = await Shell.Current.DisplayAlert(
                    "Удаление проекта", 
                    $"Вы уверены, что хотите удалить проект '{Project.Name}'?", 
                    "Да", "Нет");
                    
                if (confirmed)
                {
                    await _projectService.DeleteProjectAsync(Project.Id);
                    await Shell.Current.GoToAsync("..");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при удалении проекта: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        [RelayCommand]
        public void ToggleEditMode()
        {
            IsEditing = !IsEditing;
        }
        
        [RelayCommand]
        public async Task CancelEdit()
        {
            if (IsNew)
            {
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                IsEditing = false;
                // Reload original values
                if (Project != null)
                {
                    ProjectName = Project.Name;
                    ProjectDescription = Project.Description;
                    StartDate = Project.StartDate;
                    EndDate = Project.EndDate;
                    Status = Project.Status;
                    Priority = Project.Priority;
                }
            }
        }
    }
}

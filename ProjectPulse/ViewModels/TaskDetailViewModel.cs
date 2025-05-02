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
    public partial class TaskDetailViewModel : BaseViewModel
    {
        private readonly TaskService _taskService;
        private readonly ProjectService _projectService;
        private readonly AuthService _authService;
        
        [ObservableProperty]
        private ProjectTask task;
        
        [ObservableProperty]
        private bool isNew;
        
        [ObservableProperty]
        private bool isEditing;
        
        [ObservableProperty]
        private string taskTitle;
        
        [ObservableProperty]
        private string taskDescription;
        
        [ObservableProperty]
        private DateTime dueDate;
        
        [ObservableProperty]
        private ProjectPulse.Models.ProjectStatus status;
        
        [ObservableProperty]
        private TaskPriority priority;
        
        [ObservableProperty]
        private ObservableCollection<Project> userProjects;
        
        [ObservableProperty]
        private Project selectedProject;
        
        [ObservableProperty]
        private int estimatedHours;
        
        [ObservableProperty]
        private int actualHours;
        
        public TaskDetailViewModel(TaskService taskService, ProjectService projectService, AuthService authService)
        {
            _taskService = taskService;
            _projectService = projectService;
            _authService = authService;
            
            Title = "Задача";
            Task = new ProjectTask();
            DueDate = DateTime.Today.AddDays(7);
            Status = ProjectStatus.New;
            Priority = TaskPriority.Medium;
            UserProjects = new ObservableCollection<Project>();
            EstimatedHours = 0;
            ActualHours = 0;
        }
        
        public override async void OnAppearing()
        {
            base.OnAppearing();
            
            await LoadUserProjects();
            
            if (!IsNew && Task.Id != Guid.Empty)
            {
                await LoadTask(Task.Id);
            }
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
                
                if (Task?.ProjectId != null && Task.ProjectId != Guid.Empty)
                {
                    SelectedProject = UserProjects.FirstOrDefault(p => p.Id == Task.ProjectId);
                }
                else if (UserProjects.Any())
                {
                    SelectedProject = UserProjects.First();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при загрузке проектов: {ex.Message}");
            }
        }
        
        [RelayCommand]
        public async Task LoadTask(Guid taskId)
        {
            if (IsBusy)
                return;
                
            try
            {
                IsBusy = true;
                ClearError();
                
                Task = await _taskService.GetTaskByIdAsync(taskId);
                
                if (Task != null)
                {
                    TaskTitle = Task.Title;
                    TaskDescription = Task.Description;
                    DueDate = Task.DueDate;
                    Status = Task.Status;
                    Priority = Task.Priority;
                    EstimatedHours = Task.EstimatedHours;
                    ActualHours = Task.ActualHours;
                    
                    if (Task.ProjectId != Guid.Empty)
                    {
                        SelectedProject = UserProjects.FirstOrDefault(p => p.Id == Task.ProjectId);
                    }
                    
                    Title = Task.Title;
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при загрузке задачи: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        [RelayCommand]
        public async Task SaveTask()
        {
            if (IsBusy)
                return;
                
            if (string.IsNullOrWhiteSpace(TaskTitle))
            {
                ShowError("Название задачи не может быть пустым");
                return;
            }
            
            if (SelectedProject == null)
            {
                ShowError("Необходимо выбрать проект");
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
                    Task = new ProjectTask
                    {
                        Id = Guid.NewGuid(),
                        Title = TaskTitle,
                        Description = TaskDescription,
                        ProjectId = SelectedProject.Id,
                        DueDate = DueDate,
                        Status = Status,
                        Priority = Priority,
                        EstimatedHours = EstimatedHours,
                        ActualHours = ActualHours,
                        AssignedTo = currentUser.Id,
                        CreatedBy = currentUser.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    
                    await _taskService.AddTaskAsync(Task);
                }
                else
                {
                    Task.Title = TaskTitle;
                    Task.Description = TaskDescription;
                    Task.ProjectId = SelectedProject.Id;
                    Task.DueDate = DueDate;
                    Task.Status = Status;
                    Task.Priority = Priority;
                    Task.EstimatedHours = EstimatedHours;
                    Task.ActualHours = ActualHours;
                    Task.UpdatedAt = DateTime.UtcNow;
                    
                    await _taskService.UpdateTaskAsync(Task);
                }
                
                IsEditing = false;
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при сохранении задачи: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        [RelayCommand]
        public async Task DeleteTask()
        {
            if (IsBusy || IsNew)
                return;
                
            try
            {
                IsBusy = true;
                ClearError();
                
                bool confirmed = await Shell.Current.DisplayAlert(
                    "Удаление задачи", 
                    $"Вы уверены, что хотите удалить задачу '{Task.Title}'?", 
                    "Да", "Нет");
                    
                if (confirmed)
                {
                    await _taskService.DeleteTaskAsync(Task.Id);
                    await Shell.Current.GoToAsync("..");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при удалении задачи: {ex.Message}");
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
                if (Task != null)
                {
                    TaskTitle = Task.Title;
                    TaskDescription = Task.Description;
                    DueDate = Task.DueDate;
                    Status = Task.Status;
                    Priority = Task.Priority;
                    EstimatedHours = Task.EstimatedHours;
                    ActualHours = Task.ActualHours;
                    
                    if (Task.ProjectId != Guid.Empty)
                    {
                        SelectedProject = UserProjects.FirstOrDefault(p => p.Id == Task.ProjectId);
                    }
                }
            }
        }
        
        [RelayCommand]
        public async Task CompleteTask()
        {
            if (IsBusy || IsNew || Task.Status == ProjectStatus.Completed)
                return;
                
            try
            {
                IsBusy = true;
                ClearError();
                
                Task.Status = ProjectStatus.Completed;
                Task.UpdatedAt = DateTime.UtcNow;
                
                await _taskService.UpdateTaskAsync(Task);
                
                Status = Task.Status;
                IsEditing = false;
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при завершении задачи: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}

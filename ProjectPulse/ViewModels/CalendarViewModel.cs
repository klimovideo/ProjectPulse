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
    public partial class CalendarViewModel : BaseViewModel
    {
        private readonly CalendarService _calendarService;
        private readonly TaskService _taskService;
        private readonly ProjectService _projectService;
        private readonly AuthService _authService;
        
        [ObservableProperty]
        private DateTime selectedDate;
        
        [ObservableProperty]
        private ObservableCollection<CalendarEvent> events;
        
        [ObservableProperty]
        private ObservableCollection<CalendarEvent> selectedDateEvents;
        
        [ObservableProperty]
        private bool hasSelectedDateEvents;
        
        [ObservableProperty]
        private int currentMonth;
        
        [ObservableProperty]
        private int currentYear;
        
        [ObservableProperty]
        private string monthYearDisplay;
        
        public CalendarViewModel(CalendarService calendarService, TaskService taskService, 
            ProjectService projectService, AuthService authService)
        {
            _calendarService = calendarService;
            _taskService = taskService;
            _projectService = projectService;
            _authService = authService;
            
            Title = "Календарь";
            SelectedDate = DateTime.Today;
            Events = new ObservableCollection<CalendarEvent>();
            SelectedDateEvents = new ObservableCollection<CalendarEvent>();
            
            CurrentMonth = DateTime.Today.Month;
            CurrentYear = DateTime.Today.Year;
            UpdateMonthYearDisplay();
        }
        
        public override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadEvents();
        }
        
        private void UpdateMonthYearDisplay()
        {
            string[] monthNames = new string[] 
            {
                "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь",
                "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь"
            };
            
            MonthYearDisplay = $"{monthNames[CurrentMonth - 1]} {CurrentYear}";
        }
        
        [RelayCommand]
        public async Task LoadEvents()
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
                
                // Get start and end dates for the current month view
                var startDate = new DateTime(CurrentYear, CurrentMonth, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);
                
                // Load tasks for the date range
                var tasks = await _taskService.GetTasksByUserAndDateRangeAsync(
                    currentUser.Id, startDate, endDate);
                
                // Load projects for the date range
                var projects = await _projectService.GetProjectsByUserAndDateRangeAsync(
                    currentUser.Id, startDate, endDate);
                
                // Convert to calendar events
                Events.Clear();
                
                // Add task events
                foreach (var task in tasks)
                {
                    var taskEvent = new CalendarEvent
                    {
                        Id = task.Id,
                        Title = task.Title,
                        Description = task.Description,
                        StartDate = task.DueDate,
                        EndDate = task.DueDate,
                        IsAllDay = true,
                        EventType = CalendarEventType.Task,
                        Color = GetProjectStatusColor(task.Status)
                    };
                    
                    Events.Add(taskEvent);
                }
                
                // Add project events
                foreach (var project in projects)
                {
                    // Add project start date
                    var startEvent = new CalendarEvent
                    {
                        Id = project.Id,
                        Title = $"Начало проекта: {project.Name}",
                        Description = project.Description,
                        StartDate = project.StartDate,
                        EndDate = project.StartDate,
                        IsAllDay = true,
                        EventType = CalendarEventType.ProjectStart,
                        Color = "#4CAF50" // Green
                    };
                    
                    Events.Add(startEvent);
                    
                    // Add project end date if set
                    if (project.EndDate.HasValue)
                    {
                        var endEvent = new CalendarEvent
                        {
                            Id = project.Id,
                            Title = $"Завершение проекта: {project.Name}",
                            Description = project.Description,
                            StartDate = project.EndDate.Value,
                            EndDate = project.EndDate.Value,
                            IsAllDay = true,
                            EventType = CalendarEventType.ProjectEnd,
                            Color = "#F44336" // Red
                        };
                        
                        Events.Add(endEvent);
                    }
                }
                
                // Update selected date events
                UpdateSelectedDateEvents();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при загрузке событий: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                IsRefreshing = false;
            }
        }
        
        [RelayCommand]
        public void DateSelected(DateTime date)
        {
            SelectedDate = date;
            UpdateSelectedDateEvents();
        }
        
        private void UpdateSelectedDateEvents()
        {
            SelectedDateEvents.Clear();
            
            var dateEvents = Events.Where(e => 
                e.StartDate.Date <= SelectedDate.Date && 
                e.EndDate.Date >= SelectedDate.Date).ToList();
                
            foreach (var evt in dateEvents)
            {
                SelectedDateEvents.Add(evt);
            }
            
            HasSelectedDateEvents = SelectedDateEvents.Any();
        }
        
        [RelayCommand]
        public async Task ViewEventDetails(CalendarEvent calendarEvent)
        {
            if (calendarEvent == null)
                return;
                
            switch (calendarEvent.EventType)
            {
                case CalendarEventType.Task:
                    var taskParameters = new ShellNavigationQueryParameters
                    {
                        { "TaskId", calendarEvent.Id }
                    };
                    await Shell.Current.GoToAsync($"{nameof(TaskDetailPage)}", taskParameters);
                    break;
                    
                case CalendarEventType.ProjectStart:
                case CalendarEventType.ProjectEnd:
                    var projectParameters = new ShellNavigationQueryParameters
                    {
                        { "ProjectId", calendarEvent.Id }
                    };
                    await Shell.Current.GoToAsync($"{nameof(ProjectDetailPage)}", projectParameters);
                    break;
            }
        }
        
        [RelayCommand]
        public async Task NextMonth()
        {
            CurrentMonth++;
            if (CurrentMonth > 12)
            {
                CurrentMonth = 1;
                CurrentYear++;
            }
            
            UpdateMonthYearDisplay();
            await LoadEvents();
        }
        
        [RelayCommand]
        public async Task PreviousMonth()
        {
            CurrentMonth--;
            if (CurrentMonth < 1)
            {
                CurrentMonth = 12;
                CurrentYear--;
            }
            
            UpdateMonthYearDisplay();
            await LoadEvents();
        }
        
        [RelayCommand]
        public async Task GoToToday()
        {
            SelectedDate = DateTime.Today;
            CurrentMonth = DateTime.Today.Month;
            CurrentYear = DateTime.Today.Year;
            
            UpdateMonthYearDisplay();
            await LoadEvents();
        }
        
        [RelayCommand]
        public async Task Refresh()
        {
            IsRefreshing = true;
            await LoadEvents();
        }
        
        [RelayCommand]
        public async Task CreateTask()
        {
            var parameters = new ShellNavigationQueryParameters
            {
                { "IsNew", true },
                { "DueDate", SelectedDate }
            };
            
            await Shell.Current.GoToAsync($"{nameof(TaskDetailPage)}", parameters);
        }
        
        private string GetProjectStatusColor(ProjectPulse.Models.ProjectStatus status)
        {
            return status switch
            {
                ProjectStatus.New => "#2196F3",      // Blue
                ProjectStatus.Planned => "#9C27B0",  // Purple
                ProjectStatus.InProgress => "#FF9800", // Orange
                ProjectStatus.OnHold => "#FFC107",   // Amber
                ProjectStatus.Blocked => "#F44336",  // Red
                ProjectStatus.Completed => "#4CAF50", // Green
                ProjectStatus.Cancelled => "#9E9E9E", // Grey
                _ => "#2196F3"                   // Default Blue
            };
        }
    }
    
    public enum CalendarEventType
    {
        Task,
        ProjectStart,
        ProjectEnd
    }
    
    public class CalendarEvent
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsAllDay { get; set; }
        public CalendarEventType EventType { get; set; }
        public string Color { get; set; }
    }
}

using ProjectPulse.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using System.Linq;
using ProjectPulse.Database;
using Microsoft.Maui.Devices;
using System.Diagnostics;

namespace ProjectPulse.Services
{
    public class CalendarService
    {
        private readonly DatabaseService _database;
        private readonly TaskService _taskService;
        private readonly ProjectService _projectService;
        private const string GoogleCalendarApiBase = "https://www.googleapis.com/calendar/v3/";
        private const string OutlookCalendarApiBase = "https://graph.microsoft.com/v1.0/me/calendar/";

        public enum CalendarProvider
        {
            Google,
            Outlook,
            Local
        }

        public CalendarService(DatabaseService database, TaskService taskService, ProjectService projectService)
        {
            _database = database;
            _taskService = taskService;
            _projectService = projectService;
        }

        public async Task<List<CalendarEvent>> GetTaskCalendarEventsAsync(int userId)
        {
            try
            {
                var tasks = await _taskService.GetTasksByUserAsync(userId);
                var events = new List<CalendarEvent>();

                foreach (var task in tasks)
                {
                    if (task.DueDate.HasValue)
                    {
                        events.Add(new CalendarEvent
                        {
                            Id = task.Id.ToString(),
                            Title = task.Title ?? string.Empty,
                            Start = task.DueDate.Value,
                            End = task.DueDate.Value,
                            Description = task.Description ?? string.Empty,
                            IsAllDay = false,
                            Type = CalendarEventType.Task,
                            Color = task.TaskPulseColor ?? "#007AFF"
                        });
                    }

                    if (task.SubTasks != null)
                    {
                        foreach (var subTask in task.SubTasks)
                        {
                            if (subTask.DueDate.HasValue)
                            {
                                events.Add(new CalendarEvent
                                {
                                    Id = subTask.Id.ToString(),
                                    Title = subTask.Title ?? string.Empty,
                                    Start = subTask.DueDate.Value,
                                    End = subTask.DueDate.Value,
                                    Description = subTask.Description ?? string.Empty,
                                    IsAllDay = false,
                                    Type = CalendarEventType.Task,
                                    Color = subTask.TaskPulseColor ?? "#007AFF"
                                });
                            }
                        }
                    }
                }

                return events;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting task calendar events: {ex.Message}");
                return new List<CalendarEvent>();
            }
        }

        public async Task<List<CalendarEvent>> GetProjectCalendarEventsAsync(int userId)
        {
            try
            {
                var projects = await _projectService.GetProjectsByUserAsync(userId);
                var events = new List<CalendarEvent>();

                foreach (var project in projects)
                {
                    if (project.EndDate.HasValue)
                    {
                        events.Add(new CalendarEvent
                        {
                            Id = $"p-{project.Id}",
                            Title = project.Name ?? string.Empty,
                            Start = project.EndDate.Value,
                            End = project.EndDate.Value,
                            Description = project.Description ?? string.Empty,
                            IsAllDay = true,
                            Type = CalendarEventType.Project,
                            Color = "#5D3FD3" // Purple color for project events
                        });
                    }
                }

                return events;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting project calendar events: {ex.Message}");
                return new List<CalendarEvent>();
            }
        }

        public async Task<List<CalendarEvent>> GetAllCalendarEventsAsync(int userId)
        {
            var taskEvents = await GetTaskCalendarEventsAsync(userId);
            var projectEvents = await GetProjectCalendarEventsAsync(userId);

            // Combine both lists
            var allEvents = new List<CalendarEvent>();
            allEvents.AddRange(taskEvents);
            allEvents.AddRange(projectEvents);

            return allEvents;
        }

        public async Task<bool> SyncWithExternalCalendarAsync(CalendarProvider provider, string accessToken, List<CalendarEvent> events)
        {
            // In a real implementation, this would connect to Google or Outlook APIs
            // This is a simplified version for demonstration
            try
            {
                switch (provider)
                {
                    case CalendarProvider.Google:
                        foreach (var calEvent in events)
                        {
                            await SyncEventWithGoogleCalendarAsync(accessToken, calEvent);
                        }
                        break;
                    case CalendarProvider.Outlook:
                        foreach (var calEvent in events)
                        {
                            await SyncEventWithOutlookCalendarAsync(accessToken, calEvent);
                        }
                        break;
                    case CalendarProvider.Local:
                        // For local calendar, just return true as we already have the events locally
                        return true;
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task<bool> SyncEventWithGoogleCalendarAsync(string accessToken, CalendarEvent calEvent)
        {
            // This is a simplified example - a real implementation would use the Google Calendar API
            // to create or update events.
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var googleEvent = new
            {
                summary = calEvent.Title,
                description = calEvent.Description,
                start = new
                {
                    dateTime = calEvent.Start.ToString("o"),
                    timeZone = TimeZoneInfo.Local.Id
                },
                end = new
                {
                    dateTime = calEvent.End.ToString("o"),
                    timeZone = TimeZoneInfo.Local.Id
                },
                colorId = ConvertColorToGoogleColorId(calEvent.Color)
            };

            var content = new StringContent(JsonConvert.SerializeObject(googleEvent), System.Text.Encoding.UTF8, "application/json");

            // In a real implementation, this would be a REST API call to the Google Calendar API
            // var response = await httpClient.PostAsync($"{GoogleCalendarApiBase}calendars/primary/events", content);
            // return response.IsSuccessStatusCode;

            // For demonstration, just return true
            return true;
        }

        private async Task<bool> SyncEventWithOutlookCalendarAsync(string accessToken, CalendarEvent calEvent)
        {
            // This is a simplified example - a real implementation would use the Microsoft Graph API
            // to create or update events.
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var outlookEvent = new
            {
                subject = calEvent.Title,
                body = new
                {
                    contentType = "HTML",
                    content = calEvent.Description
                },
                start = new
                {
                    dateTime = calEvent.Start.ToString("o"),
                    timeZone = TimeZoneInfo.Local.Id
                },
                end = new
                {
                    dateTime = calEvent.End.ToString("o"),
                    timeZone = TimeZoneInfo.Local.Id
                },
                isAllDay = calEvent.IsAllDay
            };

            var content = new StringContent(JsonConvert.SerializeObject(outlookEvent), System.Text.Encoding.UTF8, "application/json");

            // In a real implementation, this would be a REST API call to the Microsoft Graph API
            // var response = await httpClient.PostAsync($"{OutlookCalendarApiBase}events", content);
            // return response.IsSuccessStatusCode;

            // For demonstration, just return true
            return true;
        }

        private string ConvertColorToGoogleColorId(string hexColor)
        {
            // Google Calendar uses predefined color IDs (1-11)
            // This is a simplified conversion from hex colors to Google color IDs
            switch (hexColor.ToLower())
            {
                case "#dc3545": // Red
                    return "11";
                case "#fd7e14": // Orange
                    return "6";
                case "#ffc107": // Yellow
                    return "5";
                case "#28a745": // Green
                    return "10";
                case "#17a2b8": // Blue
                    return "7";
                case "#5d3fd3": // Purple
                    return "3";
                default:
                    return "1"; // Default blue
            }
        }

        public async Task<bool> AddEventAsync(CalendarEvent eventToAdd)
        {
            try
            {
                await _database.SaveItemAsync(eventToAdd);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateEventAsync(CalendarEvent eventToUpdate)
        {
            try
            {
                await _database.SaveItemAsync(eventToUpdate);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteEventAsync(int eventId)
        {
            try
            {
                var eventToDelete = await _database.GetItemAsync<CalendarEvent>(eventId);
                if (eventToDelete != null)
                {
                    await _database.DeleteItemAsync(eventToDelete);
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public class CalendarEvent
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public bool IsAllDay { get; set; }
        public CalendarEventType Type { get; set; }
        public string Color { get; set; }
    }

    public enum CalendarEventType
    {
        Task,
        Project,
        Meeting,
        Reminder
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ProjectPulse.Models;
using System.Diagnostics;

namespace ProjectPulse.Services
{
    public class CalendarService
    {
        private readonly ProjectPulseContext _db;
        private readonly HttpClient _http;

        private const string GoogleCalendarApiBase  = "https://www.googleapis.com/calendar/v3/";
        private const string OutlookCalendarApiBase = "https://graph.microsoft.com/v1.0/me/calendar/";

        public enum CalendarProvider
        {
            Google,
            Outlook,
            Local
        }

        public CalendarService(ProjectPulseContext dbContext, HttpClient httpClient)
        {
            _db   = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _http = httpClient;
        }

        public async Task<List<CalendarEvent>> GetTaskCalendarEventsAsync(int userId)
        {
            var tasks = await _db.ProjectTasks
                                 .Where(t => t.AssignedTo == userId)
                                 .Include(t => t.SubTasks)
                                 .ToListAsync();

            var events = new List<CalendarEvent>();
            foreach (var task in tasks)
            {
                // основная задача
                events.Add(new CalendarEvent
                {
                    ExternalId  = task.Id.ToString(),
                    Title       = task.Title,
                    Description = task.Description ?? string.Empty,
                    Start       = task.DueDate,
                    End         = task.DueDate,
                    IsAllDay    = false,
                    Type        = CalendarEventType.Task,
                    Color       = task.Color
                });

                // подзадачи (если в модели SubTask есть поле DueDate и TaskPulseColor)
                foreach (var st in task.SubTasks.Where(st => st.CompletedDate == null))
                {
                    // если SubTask у вас не хранит DueDate и TaskPulseColor — уберите этот блок
                }
            }

            return events;
        }

        public async Task<List<CalendarEvent>> GetProjectCalendarEventsAsync(int userId)
        {
            var projects = await _db.UserProjects
                                    .Where(up => up.UserId == userId)
                                    .Select(up => up.Project)
                                    .Where(p => p.EndDate.HasValue)
                                    .ToListAsync();

            return projects.Select(p => new CalendarEvent
            {
                ExternalId  = $"p-{p.Id}",
                Title       = p.Name,
                Description = p.Description ?? string.Empty,
                Start       = p.EndDate.Value,
                End         = p.EndDate.Value,
                IsAllDay    = true,
                Type        = CalendarEventType.Project,
                Color       = "#5D3FD3"
            }).ToList();
        }

        public async Task<List<CalendarEvent>> GetAllCalendarEventsAsync(int userId)
        {
            var taskEvents    = await GetTaskCalendarEventsAsync(userId);
            var projectEvents = await GetProjectCalendarEventsAsync(userId);
            return taskEvents.Concat(projectEvents).ToList();
        }

        public async Task<bool> SyncWithExternalCalendarAsync(
            CalendarProvider provider,
            string accessToken,
            List<CalendarEvent> events)
        {
            try
            {
                switch (provider)
                {
                    case CalendarProvider.Google:
                        foreach (var ev in events)
                            await SyncEventWithGoogleCalendarAsync(accessToken, ev);
                        break;

                    case CalendarProvider.Outlook:
                        foreach (var ev in events)
                            await SyncEventWithOutlookCalendarAsync(accessToken, ev);
                        break;

                    case CalendarProvider.Local:
                        return true;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CalendarService] SyncWithExternalCalendarAsync: {ex}");
                return false;
            }
        }

        private async Task<bool> SyncEventWithGoogleCalendarAsync(string token, CalendarEvent ev)
        {
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var googleEvent = new
            {
                summary     = ev.Title,
                description = ev.Description,
                start = new { dateTime = ev.Start.ToString("o"), timeZone = TimeZoneInfo.Local.Id },
                end   = new { dateTime = ev.End.ToString("o"),   timeZone = TimeZoneInfo.Local.Id },
                colorId = ConvertColorToGoogleColorId(ev.Color)
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(googleEvent),
                Encoding.UTF8,
                "application/json"
            );

            // реальный вызов:
            // var resp = await _http.PostAsync($"{GoogleCalendarApiBase}calendars/primary/events", content);
            // return resp.IsSuccessStatusCode;

            return true;
        }

        private async Task<bool> SyncEventWithOutlookCalendarAsync(string token, CalendarEvent ev)
        {
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var outlookEvent = new
            {
                subject = ev.Title,
                body    = new { contentType = "HTML", content = ev.Description },
                start   = new { dateTime = ev.Start.ToString("o"), timeZone = TimeZoneInfo.Local.Id },
                end     = new { dateTime = ev.End.ToString("o"),   timeZone = TimeZoneInfo.Local.Id },
                isAllDay = ev.IsAllDay
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(outlookEvent),
                Encoding.UTF8,
                "application/json"
            );

            // реальный вызов:
            // var resp = await _http.PostAsync($"{OutlookCalendarApiBase}events", content);
            // return resp.IsSuccessStatusCode;

            return true;
        }

        private string ConvertColorToGoogleColorId(string hex)
        {
            return hex.ToLower() switch
            {
                "#dc3545" => "11",
                "#fd7e14" => "6",
                "#ffc107" => "5",
                "#28a745" => "10",
                "#17a2b8" => "7",
                "#5d3fd3" => "3",
                _         => "1"
            };
        }

        // — CRUD для локальных событий —

        public async Task<bool> AddEventAsync(CalendarEvent ev)
        {
            try
            {
                await _db.CalendarEvents.AddAsync(ev);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CalendarService] AddEventAsync: {ex}");
                return false;
            }
        }

        public async Task<bool> UpdateEventAsync(CalendarEvent ev)
        {
            try
            {
                _db.CalendarEvents.Update(ev);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CalendarService] UpdateEventAsync: {ex}");
                return false;
            }
        }

        public async Task<bool> DeleteEventAsync(int id)
        {
            try
            {
                var ev = await _db.CalendarEvents.FindAsync(id);
                if (ev == null) return false;
                _db.CalendarEvents.Remove(ev);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CalendarService] DeleteEventAsync: {ex}");
                return false;
            }
        }
    }
}

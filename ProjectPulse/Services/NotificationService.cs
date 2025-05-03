using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectPulse.Models;

namespace ProjectPulse.Services
{
    /// <summary>
    /// Сервис оповещений, использующий Entity Framework Core вместо локального SQLite‑слоя.
    /// </summary>
    public class NotificationService
    {
        private readonly ProjectPulseContext _db;

        public NotificationService(ProjectPulseContext dbContext)
        {
            _db = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <summary>
        /// Возвращает список уведомлений пользователя, при необходимости фильтруя только непрочитанные.
        /// </summary>
        public async Task<List<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false)
        {
            IQueryable<Notification> query = _db.Notifications
                                                 .Where(n => n.UserId == userId)
                                                 .OrderByDescending(n => n.CreatedAt);

            if (unreadOnly)
                query = query.Where(n => n.ReadAt == null);

            return await query.AsNoTracking().ToListAsync();
        }

        /// <summary>
        /// Подсчитывает непрочитанные уведомления пользователя.
        /// </summary>
        public async Task<int> GetUnreadNotificationCountAsync(int userId)
        {
            return await _db.Notifications
                             .Where(n => n.UserId == userId && n.ReadAt == null)
                             .CountAsync();
        }

        /// <summary>
        /// Создаёт новое уведомление и сохраняет его в базе.
        /// </summary>
        public async Task<bool> CreateNotificationAsync(
            int userId,
            string title,
            string message,
            NotificationType type,
            int? relatedEntityId = null)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    RelatedEntityId = relatedEntityId,
                    CreatedAt = DateTime.Now
                };

                await _db.Notifications.AddAsync(notification);
                await _db.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Помечает отдельное уведомление как прочитанное.
        /// </summary>
        public async Task<bool> MarkNotificationAsReadAsync(int notificationId)
        {
            var notification = await _db.Notifications.FindAsync(notificationId);
            if (notification == null || notification.ReadAt.HasValue)
                return false;

            notification.ReadAt = DateTime.Now;
            await _db.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Помечает все непрочитанные уведомления пользователя как прочитанные.
        /// </summary>
        public async Task<bool> MarkAllNotificationsAsReadAsync(int userId)
        {
            var notifications = await _db.Notifications
                                         .Where(n => n.UserId == userId && n.ReadAt == null)
                                         .ToListAsync();

            if (notifications.Count == 0)
                return false;

            foreach (var n in notifications)
                n.ReadAt = DateTime.Now;

            await _db.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Удаляет одно уведомление.
        /// </summary>
        public async Task<bool> DeleteNotificationAsync(int notificationId)
        {
            var notification = await _db.Notifications.FindAsync(notificationId);
            if (notification == null)
                return false;

            _db.Notifications.Remove(notification);
            await _db.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Полностью очищает историю уведомлений пользователя.
        /// </summary>
        public async Task<bool> DeleteAllUserNotificationsAsync(int userId)
        {
            var notifications = await _db.Notifications
                                         .Where(n => n.UserId == userId)
                                         .ToListAsync();

            if (notifications.Count == 0)
                return false;

            _db.Notifications.RemoveRange(notifications);
            await _db.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Проверяет задачи, срок выполнения которых истекает в течение суток, создаёт уведомления и возвращает найденные задачи.
        /// </summary>
        public async Task<List<ProjectTask>> CheckForDueTasksAsync(int userId)
        {
            var tomorrow = DateTime.Now.AddDays(1);
            var dueTasks = await _db.ProjectTasks
                                     .Where(t => t.AssignedTo == userId &&
                                                 t.DueDate <= tomorrow &&
                                                 t.Status != ProjectStatus.Completed &&
                                                 t.Status != ProjectStatus.Cancelled)
                                     .ToListAsync();

            foreach (var task in dueTasks)
            {
                await CreateNotificationAsync(
                    userId,
                    "Task Due Soon",
                    $"Task '{task.Title}' is due on {task.DueDate:g}",
                    NotificationType.TaskDueSoon,
                    task.Id);
            }

            return dueTasks;
        }

        /// <summary>
        /// Проверяет просроченные задачи, создаёт уведомления и возвращает найденные задачи.
        /// </summary>
        public async Task<List<ProjectTask>> CheckForOverdueTasksAsync(int userId)
        {
            var now = DateTime.Now;
            var overdueTasks = await _db.ProjectTasks
                                         .Where(t => t.AssignedTo == userId &&
                                                     t.DueDate < now &&
                                                     t.Status != ProjectStatus.Completed &&
                                                     t.Status != ProjectStatus.Cancelled)
                                         .ToListAsync();

            foreach (var task in overdueTasks)
            {
                await CreateNotificationAsync(
                    userId,
                    "Task Overdue",
                    $"Task '{task.Title}' was due on {task.DueDate:g}",
                    NotificationType.TaskOverdue,
                    task.Id);
            }

            return overdueTasks;
        }

        /// <summary>
        /// Создаёт уведомление опроса TeamPulse для каждого участника проекта.
        /// </summary>
        public async Task CreateTeamPulseSurveyNotificationAsync(int projectId)
        {
            var project = await _db.Projects.FindAsync(projectId);
            if (project == null)
                return;

            var userIds = await _db.UserProjects
                                   .Where(up => up.ProjectId == projectId)
                                   .Select(up => up.UserId)
                                   .Distinct()
                                   .ToListAsync();

            foreach (var uid in userIds)
            {
                await CreateNotificationAsync(
                    uid,
                    "Team Pulse Survey",
                    $"Please share your mood for project '{project.Name}'",
                    NotificationType.TeamPulseSurvey,
                    projectId);
            }
        }
    }
}
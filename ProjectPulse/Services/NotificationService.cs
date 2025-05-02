using ProjectPulse.Models;
using ProjectPulse.Database;
using System;
using System.Collections.Generic;
using Task = System.Threading.Tasks.Task;
using System.Linq;

namespace ProjectPulse.Services
{
    public class NotificationService
    {
        private readonly DatabaseService _database;

        public NotificationService()
        {
            _database = DatabaseService.Instance;
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false)
        {
            if (unreadOnly)
            {
                return await _database.GetItemsAsync<Notification>(
                    "SELECT * FROM Notifications WHERE UserId = ? AND ReadAt IS NULL ORDER BY CreatedAt DESC", 
                    userId);
            }
            else
            {
                return await _database.GetItemsAsync<Notification>(
                    "SELECT * FROM Notifications WHERE UserId = ? ORDER BY CreatedAt DESC", 
                    userId);
            }
        }

        public async Task<int> GetUnreadNotificationCountAsync(int userId)
        {
            var notifications = await _database.GetItemsAsync<Notification>(
                "SELECT COUNT(*) FROM Notifications WHERE UserId = ? AND ReadAt IS NULL", 
                userId);
                
            return notifications.Count;
        }

        public async Task<bool> CreateNotificationAsync(int userId, string title, string message, 
            NotificationType type, int relatedEntityId)
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
                
                await _database.SaveItemAsync(notification);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> MarkNotificationAsReadAsync(int notificationId)
        {
            try
            {
                var notification = await _database.GetItemAsync<Notification>(notificationId);
                if (notification == null)
                {
                    return false;
                }
                
                notification.ReadAt = DateTime.Now;
                await _database.SaveItemAsync(notification);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> MarkAllNotificationsAsReadAsync(int userId)
        {
            try
            {
                var notifications = await _database.GetItemsAsync<Notification>(
                    "SELECT * FROM Notifications WHERE UserId = ? AND ReadAt IS NULL", 
                    userId);
                    
                foreach (var notification in notifications)
                {
                    notification.ReadAt = DateTime.Now;
                    await _database.SaveItemAsync(notification);
                }
                
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteNotificationAsync(int notificationId)
        {
            try
            {
                var notification = await _database.GetItemAsync<Notification>(notificationId);
                if (notification == null)
                {
                    return false;
                }
                
                await _database.DeleteItemAsync(notification);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteAllUserNotificationsAsync(int userId)
        {
            try
            {
                var notifications = await _database.GetItemsAsync<Notification>(
                    "SELECT * FROM Notifications WHERE UserId = ?", 
                    userId);
                    
                foreach (var notification in notifications)
                {
                    await _database.DeleteItemAsync(notification);
                }
                
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<ProjectTask>> CheckForDueTasksAsync(Guid userId)
        {
            // Get tasks due within the next 24 hours
            var tomorrow = DateTime.Now.AddDays(1);
            var dueTasks = await _database.GetItemsAsync<ProjectTask>(
                "SELECT * FROM Tasks WHERE AssignedTo = ? AND DueDate <= ? AND Status != ? AND Status != ?", 
                userId, tomorrow, (int)ProjectStatus.Completed, (int)ProjectStatus.Cancelled);

            foreach (var task in dueTasks)
            {
                // Create notification for each due task
                await CreateNotificationAsync(
                    userId,
                    "Task Due Soon",
                    $"Task '{task.Title}' is due on {task.DueDate:g}",
                    NotificationType.TaskDueSoon,
                    task.Id);
            }
            
            return dueTasks;
        }

        public async Task<List<ProjectTask>> CheckForOverdueTasksAsync(Guid userId)
        {
            // Get overdue tasks
            var now = DateTime.Now;
            var overdueTasks = await _database.GetItemsAsync<ProjectTask>(
                "SELECT * FROM Tasks WHERE AssignedTo = ? AND DueDate < ? AND Status != ? AND Status != ?", 
                userId, now, (int)ProjectStatus.Completed, (int)ProjectStatus.Cancelled);

            foreach (var task in overdueTasks)
            {
                // Create notification for each overdue task
                await CreateNotificationAsync(
                    userId,
                    "Task Overdue",
                    $"Task '{task.Title}' was due on {task.DueDate:g}",
                    NotificationType.TaskOverdue,
                    task.Id);
            }
            
            return overdueTasks;
        }

        public async Task CreateTeamPulseSurveyNotificationAsync(int projectId)
        {
            // Get all team members for this project
            var userProjects = await _database.GetItemsAsync<UserProject>(
                "SELECT * FROM UserProjects WHERE ProjectId = ?", projectId);
                
            var project = await _database.GetItemAsync<Project>(projectId);
            if (project == null)
                return;
                
            foreach (var userProject in userProjects)
            {
                await CreateNotificationAsync(
                    userProject.UserId,
                    "Team Pulse Survey",
                    $"Please share your mood for project '{project.Name}'",
                    NotificationType.TeamPulseSurvey,
                    projectId);
            }
        }
    }
}

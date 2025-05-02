
using ProjectPulse.Database;
using ProjectPulse.Models;

namespace ProjectPulse.Services
{
    public class TaskService
    {
        private readonly DatabaseService _database;
        private readonly NotificationService _notificationService;

        public TaskService(NotificationService notificationService)
        {
            _database = DatabaseService.Instance;
            _notificationService = notificationService;
        }

        public async Task<List<ProjectTask>> GetTasksByProjectAsync(Guid projectId)
        {
            var tasks = await _database.GetItemsAsync<ProjectTask>("SELECT * FROM Tasks WHERE ProjectId = ?", projectId);
            
            foreach (var task in tasks)
            {
                // Load assignee
                var users = await _database.GetItemsAsync<User>("SELECT * FROM Users WHERE Id = ?", task.AssignedTo);
                if (users.Count > 0)
                {
                    task.AssignedTo = users[0];
                }
                
                // Load subtasks
                task.SubTasks = await _database.GetItemsAsync<SubTask>("SELECT * FROM SubTasks WHERE TaskId = ?", task.Id);
                
                // Load comments
                task.Comments = await _database.GetItemsAsync<Comment>("SELECT * FROM Comments WHERE TaskId = ?", task.Id);
                
                // Load comment authors
                foreach (var comment in task.Comments)
                {
                    var authors = await _database.GetItemsAsync<User>("SELECT * FROM Users WHERE Id = ?", comment.UserId);
                    if (authors.Count > 0)
                    {
                        comment.Author = authors[0];
                    }
                }
                
                // Calculate completion percentage from subtasks
                if (task.SubTasks.Count > 0)
                {
                    int completedSubtasks = task.SubTasks.Count(st => st.IsCompleted);
                    task.CompletionPercentage = (double)completedSubtasks / task.SubTasks.Count * 100;
                }
                
                // Calculate urgency score
                task.UrgencyScore = CalculateUrgencyScore(task);
            }
            
            return tasks;
        }

        public async Task<List<ProjectTask>> GetTasksDueSoonAsync(Guid userId, int days)
        {
            var startDate = DateTime.Now;
            var endDate = startDate.AddDays(days);
            return await _database.GetItemsAsync<ProjectTask>(
                "SELECT * FROM Tasks WHERE AssignedTo = ? AND DueDate BETWEEN ? AND ? AND Status != ?", 
                userId, startDate, endDate, ProjectStatus.Completed);
        }

        public async Task<List<ProjectTask>> GetOverdueTasksAsync(Guid userId)
        {
            return await _database.GetItemsAsync<ProjectTask>(
                "SELECT * FROM Tasks WHERE AssignedTo = ? AND DueDate < ? AND Status != ?", 
                userId, DateTime.Now, ProjectStatus.Completed);
        }

        public async Task<int> GetTaskCountAsync(Guid userId)
        {
            var tasks = await _database.GetItemsAsync<ProjectTask>(
                "SELECT * FROM Tasks WHERE AssignedTo = ? AND Status != ?", 
                userId, ProjectStatus.Completed);
            return tasks.Count;
        }

        public async Task<int> GetCompletedTaskCountAsync(Guid userId)
        {
            var tasks = await _database.GetItemsAsync<ProjectTask>(
                "SELECT * FROM Tasks WHERE AssignedTo = ? AND Status = ?", 
                userId, ProjectStatus.Completed);
            return tasks.Count;
        }

        public async Task<List<ProjectTask>> GetTasksByUserAsync(Guid userId)
        {
            return await _database.GetItemsAsync<ProjectTask>(
                "SELECT * FROM Tasks WHERE AssignedTo = ? AND Status != ? ORDER BY DueDate", 
                userId, ProjectStatus.Completed);
        }

        public async Task<ProjectTask> CreateTaskAsync(ProjectTask task)
        {
            try
            {
                task.CreatedAt = DateTime.Now;
                await _database.SaveItemAsync(task);
                
                // Send notification to assignee
                if (_notificationService != null && task.AssignedTo != task.CreatedBy)
                {
                    await _notificationService.CreateNotificationAsync(
                        task.AssignedTo,
                        "Task Assigned",
                        $"You have been assigned a new task: {task.Title}",
                        NotificationType.TaskAssigned,
                        task.Id);
                }
                
                return task;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> UpdateTaskAsync(ProjectTask task)
        {
            try
            {
                // Get the original task to check for status/assignee changes
                var originalTask = await _database.GetItemAsync<ProjectTask>(task.Id);
                bool statusChanged = originalTask.Status != task.Status;
                bool assigneeChanged = originalTask.AssignedTo != task.AssignedTo;
                
                // Update task
                await _database.SaveItemAsync(task);
                
                // Send notifications
                if (_notificationService != null)
                {
                    // Status change notification
                    if (statusChanged)
                    {
                        await _notificationService.CreateNotificationAsync(
                            originalTask.CreatedBy,
                            "Task Status Changed",
                            $"Task '{task.Title}' status changed to {task.Status}",
                            NotificationType.StatusChanged,
                            task.Id);
                            
                        // If completed, send notification to creator
                        if (task.Status == ProjectStatus.Completed && originalTask.CreatedBy != task.AssignedTo)
                        {
                            await _notificationService.CreateNotificationAsync(
                                originalTask.CreatedBy,
                                "Task Completed",
                                $"Task '{task.Title}' has been completed",
                                NotificationType.StatusChanged,
                                task.Id);
                        }
                    }
                    
                    // Assignee change notification
                    if (assigneeChanged)
                    {
                        await _notificationService.CreateNotificationAsync(
                            task.AssignedTo,
                            "Task Assigned",
                            $"You have been assigned to task: {task.Title}",
                            NotificationType.TaskAssigned,
                            task.Id);
                    }
                }
                
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteTaskAsync(Guid taskId)
        {
            try
            {
                var task = await _database.GetItemAsync<ProjectTask>(taskId);
                if (task == null)
                {
                    return false;
                }
                
                // Delete subtasks
                await _database.DeleteItemAsync<SubTask>("DELETE FROM SubTasks WHERE TaskId = ?", taskId);
                
                // Delete comments
                await _database.DeleteItemAsync<Comment>("DELETE FROM Comments WHERE TaskId = ?", taskId);
                
                // Delete task
                await _database.DeleteItemAsync(task);
                
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private double CalculateUrgencyScore(ProjectTask task)
        {
            if (task.Status == ProjectStatus.Completed)
                return 0;
                
            // Calculate days left
            double daysLeft = (task.DueDate - DateTime.Now).TotalDays;
            
            // Base urgency score on days left
            double urgencyScore = 0;
            
            if (daysLeft < 0) // Overdue tasks
            {
                urgencyScore = 100; // Maximum urgency for overdue tasks
            }
            else if (daysLeft < 1) // Due today
            {
                urgencyScore = 90;
            }
            else if (daysLeft < 2) // Due tomorrow
            {
                urgencyScore = 75;
            }
            else if (daysLeft < 3) // Due in 3 days
            {
                urgencyScore = 60;
            }
            else if (daysLeft < 7) // Due this week
            {
                urgencyScore = 40;
            }
            else if (daysLeft < 14) // Due next week
            {
                urgencyScore = 25;
            }
            else // Due in more than 2 weeks
            {
                urgencyScore = 10;
            }
            
            // Adjust based on priority
            switch (task.Priority)
            {
                case TaskPriority.Urgent:
                    urgencyScore = Math.Min(urgencyScore + 40, 100);
                    break;
                case TaskPriority.High:
                    urgencyScore = Math.Min(urgencyScore + 20, 100);
                    break;
                case TaskPriority.Medium:
                    // No adjustment
                    break;
                case TaskPriority.Low:
                    urgencyScore = Math.Max(urgencyScore - 10, 0);
                    break;
            }
            
            return urgencyScore;
        }
    }
}

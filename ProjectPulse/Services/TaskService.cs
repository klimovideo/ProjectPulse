// --------------------------- TaskService ---------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectPulse.Models;

namespace ProjectPulse.Services;

public class TaskService
{
    private readonly ProjectPulseContext _db;
    private readonly NotificationService _notificationService;

    public TaskService(ProjectPulseContext dbContext, NotificationService notificationService)
    {
        _db = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    public async Task<List<ProjectTask>> GetTasksByProjectAsync(int projectId)
    {
        var tasks = await _db.ProjectTasks
                             .Where(t => t.ProjectId == projectId)
                             .Include(t => t.AssignedUser)
                             .Include(t => t.SubTasks)
                             .Include(t => t.Comments)
                                 .ThenInclude(c => c.User)
                             .ToListAsync();

        foreach (var task in tasks)
        {
            task.CompletionPercentage = task.SubTasks.Count == 0
                ? 0
                : (double)task.SubTasks.Count(st => st.IsCompleted) / task.SubTasks.Count * 100;

            task.UrgencyScore = CalculateUrgencyScore(task);
        }

        return tasks;
    }

    public Task<List<ProjectTask>> GetTasksDueSoonAsync(int userId, int days)
    {
        var now = DateTime.Now;
        var till = now.AddDays(days);

        return _db.ProjectTasks
                  .Where(t => t.AssignedTo == userId
                           && t.DueDate >= now
                           && t.DueDate <= till
                           && t.Status != ProjectStatus.Completed)
                  .Include(t => t.Project)
                  .AsNoTracking()
                  .ToListAsync();
    }

    public Task<List<ProjectTask>> GetOverdueTasksAsync(int userId) =>
        _db.ProjectTasks
           .Where(t => t.AssignedTo == userId
                    && t.DueDate < DateTime.Now
                    && t.Status != ProjectStatus.Completed)
           .Include(t => t.Project)
           .AsNoTracking()
           .ToListAsync();

    public Task<int> GetTaskCountAsync(int userId) =>
        _db.ProjectTasks
           .CountAsync(t => t.AssignedTo == userId && t.Status != ProjectStatus.Completed);

    public Task<int> GetCompletedTaskCountAsync(int userId) =>
        _db.ProjectTasks
           .CountAsync(t => t.AssignedTo == userId && t.Status == ProjectStatus.Completed);

    public Task<List<ProjectTask>> GetTasksByUserAsync(int userId) =>
        _db.ProjectTasks
           .Where(t => t.AssignedTo == userId && t.Status != ProjectStatus.Completed)
           .OrderBy(t => t.DueDate)
           .Include(t => t.Project)
           .AsNoTracking()
           .ToListAsync();

    public async Task<ProjectTask?> CreateTaskAsync(ProjectTask task)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            task.CreatedAt = DateTime.Now;
            await _db.ProjectTasks.AddAsync(task);
            await _db.SaveChangesAsync();

            if (task.AssignedTo != task.CreatedBy)
            {
                await _notificationService.CreateNotificationAsync(
                    task.AssignedTo,
                    "Task Assigned",
                    $"You have been assigned a new task: {task.Title}",
                    NotificationType.TaskAssigned,
                    task.Id);
            }

            await tx.CommitAsync();
            return task;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TaskService] CreateTaskAsync: {ex}");
            await tx.RollbackAsync();
            return null;
        }
    }

    public async Task<bool> UpdateTaskAsync(ProjectTask task)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var original = await _db.ProjectTasks.AsNoTracking().FirstAsync(t => t.Id == task.Id);

            bool statusChanged = original.Status != task.Status;
            bool assigneeChanged = original.AssignedTo != task.AssignedTo;

            _db.ProjectTasks.Update(task);
            await _db.SaveChangesAsync();

            if (statusChanged)
            {
                await _notificationService.CreateNotificationAsync(
                    original.CreatedBy,
                    "Task Status Changed",
                    $"Task '{task.Title}' status changed to {task.Status}",
                    NotificationType.System,
                    task.Id);

                if (task.Status == ProjectStatus.Completed && original.CreatedBy != task.AssignedTo)
                {
                    await _notificationService.CreateNotificationAsync(
                        original.CreatedBy,
                        "Task Completed",
                        $"Task '{task.Title}' has been completed",
                        NotificationType.System,
                        task.Id);
                }
            }

            if (assigneeChanged)
            {
                await _notificationService.CreateNotificationAsync(
                    task.AssignedTo,
                    "Task Assigned",
                    $"You have been assigned to task: {task.Title}",
                    NotificationType.TaskAssigned,
                    task.Id);
            }

            await tx.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TaskService] UpdateTaskAsync: {ex}");
            await tx.RollbackAsync();
            return false;
        }
    }

    public async Task<bool> DeleteTaskAsync(int taskId)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var task = await _db.ProjectTasks.FindAsync(taskId);
            if (task is null) return false;

            _db.ProjectTasks.Remove(task);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TaskService] DeleteTaskAsync: {ex}");
            await tx.RollbackAsync();
            return false;
        }
    }

    // --------------------------- helpers -----------------------------------
    private double CalculateUrgencyScore(ProjectTask task)
    {
        if (task.Status == ProjectStatus.Completed) return 0;

        double daysLeft = (task.DueDate - DateTime.Now).TotalDays;
        double score = daysLeft switch
        {
            < 0 => 100,
            < 1 => 90,
            < 2 => 75,
            < 3 => 60,
            < 7 => 40,
            < 14 => 25,
            _ => 10
        };

        score = task.Priority switch
        {
            TaskPriority.Urgent => Math.Min(score + 40, 100),
            TaskPriority.High => Math.Min(score + 20, 100),
            TaskPriority.Low => Math.Max(score - 10, 0),
            _ => score
        };

        return score;
    }
}

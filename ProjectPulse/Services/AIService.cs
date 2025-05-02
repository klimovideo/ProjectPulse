using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using ProjectPulse.Models;

namespace ProjectPulse.Services
{
    public class AIService
    {
        private readonly ProjectPulseContext _db;

        public AIService(ProjectPulseContext dbContext)
        {
            _db = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<string> GenerateTaskDescriptionAsync(ProjectTask task)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));

            try
            {
                var description = $"{task.Title} - {task.Priority} priority task";

                if (task.DueDate != default)
                    description += $" - Due: {task.DueDate:d}";

                var project = await _db.Projects
                                       .Where(p => p.Id == task.ProjectId)
                                       .Select(p => new { p.Name })
                                       .FirstOrDefaultAsync();
                if (project != null)
                    description += $" - Project: {project.Name}";

                return description;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AIService] GenerateTaskDescriptionAsync: {ex}");
                throw;
            }
        }

        public async Task<string> GenerateTaskRecommendationsAsync(ProjectTask task)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));

            try
            {
                var relatedTasks = await _db.ProjectTasks
                                            .Where(t => t.ProjectId == task.ProjectId)
                                            .ToListAsync();

                var recs = new List<string>();
                if (task.Priority == TaskPriority.Low)
                    recs.Add("Consider delegating this task");

                if ((task.DueDate - DateTime.Now).TotalDays < 2)
                    recs.Add("Start working on this task immediately");

                return recs.Count > 0
                    ? "Recommendations:\n- " + string.Join("\n- ", recs)
                    : "No special recommendations.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AIService] GenerateTaskRecommendationsAsync: {ex}");
                throw;
            }
        }

        public async Task<string> GenerateProjectSummaryAsync(Project project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));

            try
            {
                var tasksCount = await _db.ProjectTasks
                                         .CountAsync(t => t.ProjectId == project.Id);

                var completedCount = await _db.ProjectTasks
                                            .CountAsync(t => t.ProjectId == project.Id && t.Status == ProjectStatus.Completed);

                var summary = $"Project: {project.Name}\n" +
                              $"Status: {project.Status}\n" +
                              $"Total Tasks: {tasksCount}, Completed: {completedCount}\n";

                if (project.EndDate.HasValue)
                    summary += $"Due Date: {project.EndDate:d}\n";

                return summary;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AIService] GenerateProjectSummaryAsync: {ex}");
                throw;
            }
        }

        public async Task<string> GenerateTaskOptimizationAsync(Project project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));

            try
            {
                var tasks = await _db.ProjectTasks
                                     .Where(t => t.ProjectId == project.Id)
                                     .ToListAsync();

                var suggestions = new List<string>();
                var overdue = tasks
                    .Where(t => t.DueDate < DateTime.Now && t.Status != ProjectStatus.Completed)
                    .ToList();
                if (overdue.Any())
                    suggestions.Add("Address overdue tasks immediately");

                return suggestions.Count > 0
                    ? "Task Optimization Suggestions:\n- " + string.Join("\n- ", suggestions)
                    : "No optimization suggestions at this time.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AIService] GenerateTaskOptimizationAsync: {ex}");
                throw;
            }
        }

        public async Task<List<ProjectTask>> GetRecommendedTaskOrderAsync(int userId)
        {
            try
            {
                var tasks = await _db.ProjectTasks
                                     .Where(t => t.AssignedTo == userId)
                                     .Include(t => t.SubTasks)
                                     .ToListAsync();

                if (!tasks.Any())
                    return new List<ProjectTask>();

                return OrderTasksByPriority(tasks);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AIService] GetRecommendedTaskOrderAsync: {ex}");
                return new List<ProjectTask>();
            }
        }

        private List<ProjectTask> OrderTasksByPriority(List<ProjectTask> tasks)
        {
            var completed = tasks.Where(t => t.Status == ProjectStatus.Completed).ToList();
            var pending   = tasks.Where(t => t.Status != ProjectStatus.Completed).ToList();

            var ordered = pending
                .OrderByDescending(CalculateTaskPriorityScore)
                .ToList();

            ordered.AddRange(completed);
            return ordered;
        }

        private double CalculateTaskPriorityScore(ProjectTask t)
        {
            double score = 0;

            var daysLeft = (t.DueDate - DateTime.Now).TotalDays;
            if (daysLeft < 0)        score += 50;
            else if (daysLeft < 1)   score += 40;
            else if (daysLeft < 2)   score += 30;
            else if (daysLeft < 7)   score += 20;
            else if (daysLeft < 14)  score += 10;

            score += t.Priority switch
            {
                TaskPriority.High   => 50,
                TaskPriority.Medium => 15,
                TaskPriority.Low    => 5,
                _                   => 0
            };

            if (t.Status == ProjectStatus.InProgress)
                score += 10;

            score += t.SubTasks?.Count ?? 0;
            return score;
        }

        public async Task<string> GetTaskCompletionPredictionAsync(int taskId)
        {
            try
            {
                var task = await _db.ProjectTasks.FindAsync(taskId);
                if (task == null) return "Task not found";

                return SimulateTaskPrediction(task);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AIService] GetTaskCompletionPredictionAsync: {ex}");
                return "Error predicting task completion";
            }
        }

        private string SimulateTaskPrediction(ProjectTask t)
        {
            var daysLeft = (t.DueDate - DateTime.Now).Days;
            double prob  = 0.8;

            prob += t.Priority switch
            {
                TaskPriority.High => 0.15,
                TaskPriority.Low  => -0.1,
                _                 => 0
            };

            if (daysLeft < 0)        prob -= 0.2;
            else if (daysLeft < 3)   prob += 0.1;

            prob = Math.Clamp(prob, 0, 1);

            return $"Task completion probability: {prob * 100:F1}%\n" +
                   $"Estimated completion time: {CalculateEstimatedHours(t)} hours";
        }

        private int CalculateEstimatedHours(ProjectTask t)
        {
            int hours = 4;
            hours += t.Priority switch
            {
                TaskPriority.High => 2,
                TaskPriority.Low  => -1,
                _                 => 0
            };

            hours += t.SubTasks?.Count ?? 0;
            return Math.Max(1, hours);
        }
    }
}

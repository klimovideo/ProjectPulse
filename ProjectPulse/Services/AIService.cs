using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectPulse.Models;
using ProjectPulse.Services;
using Microsoft.Maui.ApplicationModel;
using System.Diagnostics;

namespace ProjectPulse.Services
{
    public class AIService
    {
        private readonly TaskService _taskService;
        private readonly ProjectService _projectService;

        public AIService(TaskService taskService, ProjectService projectService)
        {
            _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
            _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
        }

        public async Task<string> GenerateTaskDescriptionAsync(ProjectTask task)
        {
            try
            {
                if (task == null)
                    throw new ArgumentNullException(nameof(task));

                // Generate description based on task properties
                string description = $"{task.Title} - {task.Priority} priority task";
                
                if (task.DueDate.HasValue)
                    description += $" - Due: {task.DueDate.Value.ToString("d")}";

                if (task.ProjectId.HasValue)
                {
                    var project = await _projectService.GetProjectByIdAsync(task.ProjectId.Value);
                    if (project != null)
                        description += $" - Project: {project.Name}";
                }

                return description;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error generating task description: {ex.Message}");
                throw;
            }
        }

        public async Task<string> GenerateTaskRecommendationsAsync(ProjectTask task)
        {
            try
            {
                if (task == null)
                    throw new ArgumentNullException(nameof(task));

                // Get related tasks for context
                var relatedTasks = await _taskService.GetTasksByProjectAsync(task.ProjectId.Value);

                // Generate recommendations based on task properties and related tasks
                string recommendations = "Recommendations:\n";
                
                if (task.Priority == ProjectPulse.Models.TaskPriority.Low)
                    recommendations += "- Consider delegating this task\n";
                
                if (task.DueDate.HasValue && task.DueDate.Value < DateTime.Now.AddDays(2))
                    recommendations += "- Start working on this task immediately\n";

                return recommendations;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error generating task recommendations: {ex.Message}");
                throw;
            }
        }

        public async Task<string> GenerateProjectSummaryAsync(ProjectPulse.Models.Project project)
        {
            try
            {
                if (project == null)
                    throw new ArgumentNullException(nameof(project));

                // Get project tasks for context
                var tasks = await _taskService.GetTasksByProjectAsync(project.Id);

                // Generate summary based on project properties and tasks
                string summary = $"Project: {project.Name}\n";
                summary += $"Status: {project.Status}\n";
                
                if (project.EndDate.HasValue)
                    summary += $"Due Date: {project.EndDate.Value.ToString("d")}\n";

                return summary;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error generating project summary: {ex.Message}");
                throw;
            }
        }

        public async Task<string> GenerateTaskOptimizationAsync(ProjectPulse.Models.Project project)
        {
            try
            {
                if (project == null)
                    throw new ArgumentNullException(nameof(project));

                // Get project tasks
                var tasks = await _taskService.GetTasksByProjectAsync(project.Id);

                // Generate optimization suggestions
                string optimization = "Task Optimization Suggestions:\n";
                
                // Basic optimization logic
                var overdueTasks = tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value < DateTime.Now && t.Status != ProjectPulse.Models.Status.Completed);
                if (overdueTasks.Any())
                    optimization += "- Address overdue tasks immediately\n";

                return optimization;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error generating task optimization: {ex.Message}");
                throw;
            }
        }

        public async Task<List<ProjectTask>> GetRecommendedTaskOrderAsync(int userId)
        {
            try
            {
                var tasks = await _taskService.GetTasksByUserAsync(userId);
                if (tasks == null || !tasks.Any())
                    return new List<ProjectTask>();

                return OrderTasksByPriority(tasks);
            }
            catch (Exception)
            {
                return new List<ProjectTask>();
            }
        }

        private List<ProjectTask> OrderTasksByPriority(List<ProjectTask> tasks)
        {
            // First, separate completed tasks
            var completedTasks = tasks.Where(t => t.Status == ProjectStatus.Completed).ToList();
            var pendingTasks = tasks.Where(t => t.Status != ProjectStatus.Completed).ToList();
            
            // Order pending tasks by a priority score
            var orderedTasks = pendingTasks.OrderByDescending(t => CalculateTaskPriorityScore(t)).ToList();
            
            // Completed tasks go to the end
            orderedTasks.AddRange(completedTasks);
            return orderedTasks;
        }

        private double CalculateTaskPriorityScore(ProjectTask task)
        {
            double score = 0;
            
            // Factor 1: Due date urgency
            if (task.DueDate.HasValue)
            {
                int daysLeft = (int)(task.DueDate.Value - DateTime.Now).TotalDays;
                if (daysLeft < 0) score += 50; // Overdue
                else if (daysLeft < 1) score += 40; // Due today
                else if (daysLeft < 2) score += 30; // Due tomorrow
                else if (daysLeft < 7) score += 20; // Due this week
                else if (daysLeft < 14) score += 10; // Due next week
            }
            
            // Factor 2: Priority level
            switch (task.Priority)
            {
                case TaskPriority.High:
                    score += 50;
                    break;
                case TaskPriority.Medium:
                    score += 15;
                    break;
                case TaskPriority.Low:
                    score += 5;
                    break;
            }
            
            // Factor 3: Task status (in progress tasks get priority)
            if (task.Status == ProjectStatus.InProgress)
                score += 10;
                
            // Factor 4: Complexity (approximated by number of subtasks)
            score += task.SubTasks?.Count ?? 0;
            
            return score;
        }

        public async Task<string> GetTaskCompletionPredictionAsync(int taskId)
        {
            try
            {
                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null)
                    return "Task not found";

                return SimulateTaskPrediction(task);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting task completion prediction: {ex.Message}");
                return "Error predicting task completion";
            }
        }

        private string SimulateTaskPrediction(ProjectTask task)
        {
            // Calculate days left
            int daysLeft = (int)(task.DueDate.Value - DateTime.Now).TotalDays;
            
            // Calculate completion probability
            double completionProbability = 0.8; // Base probability
            
            // Adjust probability based on factors
            switch (task.Priority)
            {
                case TaskPriority.High:
                    completionProbability += 0.15;
                    break;
                case TaskPriority.Low:
                    completionProbability -= 0.1;
                    break;
            }
            
            if (daysLeft < 0)
                completionProbability -= 0.2;
            else if (daysLeft < 3)
                completionProbability += 0.1;
            
            completionProbability = Math.Max(0, Math.Min(1, completionProbability));
            
            return $"Task completion probability: {completionProbability * 100:F1}%\n" +
                   $"Estimated completion time: {CalculateEstimatedHours(task)} hours";
        }

        private int CalculateEstimatedHours(ProjectTask task)
        {
            int baseHours = 4; // Default baseline
            
            // Adjust for priority
            switch (task.Priority)
            {
                case TaskPriority.High:
                    baseHours += 2;
                    break;
                case TaskPriority.Low:
                    baseHours = Math.Max(1, baseHours - 1);
                    break;
            }
            
            // Adjust for complexity
            baseHours += task.SubTasks?.Count ?? 0;
            
            return Math.Max(1, baseHours);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectPulse.Models
{
    public class ProjectTask
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(100)] public string Title { get; set; }
        public string Description { get; set; }

        public int ProjectId  { get; set; }
        public int AssignedTo { get; set; }
        public int CreatedBy  { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public DateTime DueDate   { get; set; }
        public DateTime? CompletedDate { get; set; }

        public TaskPriority  Priority { get; set; } = TaskPriority.Medium;
        public ProjectStatus Status   { get; set; } = ProjectStatus.New;

        // Planning & gamification
        public int EstimatedHours { get; set; }
        public int ActualHours    { get; set; }
        public double CompletionPercentage { get; set; }
        public double UrgencyScore   { get; set; }
        public int    PointsValue    { get; set; }
        public bool   IsRewarded     { get; set; }

        //-- Навигация
        public Project Project       { get; set; }
        public User    AssignedUser  { get; set; }
        public User    Creator       { get; set; }

        public ICollection<SubTask> SubTasks { get; set; } = new();
        public ICollection<Comment> Comments { get; set; } = new();
    }

    public class SubTask
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(100)] public string Title { get; set; }
        public int ProjectTaskId { get; set; }

        public bool IsCompleted    { get; set; }
        public DateTime? CompletedDate { get; set; }
        public DateTime  CreatedAt     { get; set; } = DateTime.Now;

        public ProjectTask ProjectTask { get; set; }
    }

    public enum TaskPriority { Low, Medium, High, Urgent }
    {
        Low,
        Medium,
        High,
        Urgent
    }
}

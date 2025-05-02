using SQLite;
using System;
using System.Collections.Generic;

namespace ProjectPulse.Models
{
    [Table("Tasks")]
    public class ProjectTask
    {
        [PrimaryKey]
        public Guid Id { get; set; }
        [MaxLength(100)]
        public string Title { get; set; }
        public string Description { get; set; }
        [Indexed]
        public Guid ProjectId { get; set; }
        [Indexed]
        public Guid AssignedTo { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public TaskPriority Priority { get; set; }
        public ProjectStatus Status { get; set; }
        public int EstimatedHours { get; set; }
        public int ActualHours { get; set; }
        
        // Task pulse - urgency indicators
        public double CompletionPercentage { get; set; }
        public double UrgencyScore { get; set; } // 0-100, calculated based on due date and priority

        // Fields for gamification
        public int PointsValue { get; set; }
        public bool IsRewarded { get; set; }

        // Properties used in UI but not stored in DB
        [Ignore]
        public List<SubTask> SubTasks { get; set; }
        [Ignore]
        public List<Comment> Comments { get; set; }
        [Ignore]
        public User AssignedToUser { get; set; }
        [Ignore]
        public string TaskPulseColor => CalculateTaskPulseColor();

        public ProjectTask()
        {
            Status = ProjectStatus.New;
            Priority = TaskPriority.Medium;
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
            EstimatedHours = 0;
            ActualHours = 0;
            SubTasks = new List<SubTask>();
            Comments = new List<Comment>();
        }

        private string CalculateTaskPulseColor()
        {
            // Calculate days left
            double daysLeft = (DueDate - DateTime.Now).TotalDays;
            
            // Calculate color based on a combination of urgency and priority
            if (Status == ProjectStatus.Completed)
                return "#28a745"; // Green for completed tasks
                
            if (Status == ProjectStatus.Cancelled)
                return "#6c757d"; // Gray for cancelled tasks
                
            if (daysLeft < 0)
                return "#dc3545"; // Red for overdue tasks
                
            if (daysLeft < 2 || Priority == TaskPriority.Urgent)
                return "#fd7e14"; // Orange for urgent tasks
                
            if (daysLeft < 5 || Priority == TaskPriority.High)
                return "#ffc107"; // Yellow for high priority
                
            return "#17a2b8"; // Blue for normal tasks
        }
    }

    [Table("SubTasks")]
    public class SubTask
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [MaxLength(100)]
        public string Title { get; set; }
        [Indexed]
        public int TaskId { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedDate { get; set; }
        public DateTime CreatedAt { get; set; }

        public SubTask()
        {
            CreatedAt = DateTime.Now;
        }
    }

    [Table("Comments")]
    public class Comment
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Indexed]
        public int TaskId { get; set; }
        public int ProjectId { get; set; } // 0 if it's a task comment
        public int UserId { get; set; }
        public string Content { get; set; }
        public string? AttachmentPath { get; set; }
        public DateTime CreatedAt { get; set; }
        
        [Ignore]
        public User Author { get; set; }

        public Comment()
        {
            CreatedAt = DateTime.Now;
        }
    }

    public enum ProjectStatus
    {
        New,
        Planned,
        InProgress,
        OnHold,
        Blocked,
        Completed,
        Cancelled
    }

    public enum TaskPriority
    {
        Low,
        Medium,
        High,
        Urgent
    }
}

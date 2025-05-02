using SQLite;
using System;

namespace ProjectPulse.Models
{
    [Table("Notifications")]
    public class Notification
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Indexed]
        public int UserId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public NotificationType Type { get; set; }
        public int RelatedEntityId { get; set; } // ID of the related entity (Task, Project, etc.)
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public bool IsRead => ReadAt.HasValue;
        
        public Notification()
        {
            CreatedAt = DateTime.Now;
        }
    }

    [Table("TeamPulse")]
    public class TeamPulse
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Indexed]
        public int UserId { get; set; }
        [Indexed]
        public int ProjectId { get; set; }
        public int MoodRating { get; set; } // 1-5 scale
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        
        [Ignore]
        public User User { get; set; }
        
        public TeamPulse()
        {
            CreatedAt = DateTime.Now;
        }
    }

    public enum NotificationType
    {
        TaskAssigned,
        ProjectStatusChanged,
        TaskDueSoon,
        TaskOverdue,
        ProjectCreated,
        ProjectCompleted,
        Comment,
        TeamPulseSurvey,
        System
    }
}

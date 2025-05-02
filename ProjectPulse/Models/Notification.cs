using System;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectPulse.Models
{
    public class Notification
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int UserId { get; set; }

        [Required] public string Title   { get; set; }
        [Required] public string Message { get; set; }

        public NotificationType Type { get; set; }
        public int? RelatedEntityId  { get; set; }
        public DateTime CreatedAt    { get; set; } = DateTime.Now;
        public DateTime? ReadAt      { get; set; }

        public bool IsRead => ReadAt.HasValue;

        public User User { get; set; }
    }

    public enum NotificationType
    {
        TaskAssigned, ProjectStatusChanged, TaskDueSoon, TaskOverdue,
        ProjectCreated, ProjectCompleted, Comment, TeamPulseSurvey, System
    }

}

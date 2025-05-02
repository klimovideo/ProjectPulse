using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectPulse.Models
{
    public enum CalendarEventType
    {
        Task,
        Project,
        Meeting,
        Reminder
    }

    public class CalendarEvent
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }                 // локальный PK

        [MaxLength(100)]
        public string ExternalId { get; set; }      // оригинальный Id (task.Id.ToString() или "p-{project.Id}")

        [Required, MaxLength(200)]
        public string Title { get; set; }

        public string Description { get; set; }

        public DateTime Start { get; set; }
        public DateTime End   { get; set; }

        public bool IsAllDay { get; set; }
        public CalendarEventType Type { get; set; }

        [MaxLength(20)]
        public string Color { get; set; }
    }
}

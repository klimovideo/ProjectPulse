using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectPulse.Models
{
    public class User
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required, MaxLength(200)]
        public string FullName { get; set; }

        public string AvatarPath { get; set; }

        public bool UseBiometricAuth { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLogin { get; set; }

        /// <summary>
        /// JWT-токен, сгенерированный при логине.
        /// Не сохраняется в БД.
        /// </summary>
        [NotMapped]
        public string? AuthToken { get; set; }

        // Навигационные свойства

        public ICollection<Notification> Notifications   { get; set; } = new List<Notification>();
        public ICollection<UserRole>     Roles           { get; set; } = new List<UserRole>();
        public ICollection<ProjectTask>  AssignedTasks   { get; set; } = new List<ProjectTask>();
        public ICollection<ProjectTask>  CreatedTasks    { get; set; } = new List<ProjectTask>();
        public ICollection<Project>      ProjectsOwned   { get; set; } = new List<Project>();
        public ICollection<UserProject>  UserProjects    { get; set; } = new List<UserProject>();
        public ICollection<Comment>      Comments        { get; set; } = new List<Comment>();
        public ICollection<TeamPulse>    TeamPulses      { get; set; } = new List<TeamPulse>();
    }
}

using System;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectPulse.Models
{
    public class Project
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required, MaxLength(100)] public string Name { get; set; }
        public string Description  { get; set; }
        public DateTime StartDate  { get; set; }
        public DateTime? EndDate   { get; set; }
        public ProjectStatus Status   { get; set; } = ProjectStatus.New;
        public ProjectPriority Priority{ get; set; } = ProjectPriority.Medium;
        public int  CreatedBy     { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        //-- Навигация
        public User Creator { get; set; }
        public ICollection<ProjectTask> Tasks        { get; set; } = new();
        public ICollection<UserProject> UserProjects { get; set; } = new();
        public ICollection<TeamPulse>   TeamPulses   { get; set; } = new();
    }

    [Table("UserProjects")]
    public class UserProject
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int UserId   { get; set; }
        public int ProjectId{ get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.Now;

        public User    User    { get; set; }
        public Project Project { get; set; }
    }

    public enum ProjectPriority { Low, Medium, High, Critical }
    public enum ProjectStatus  { New, Planned, InProgress, OnHold, Blocked, Completed, Cancelled }
}

using SQLite;
using System;
using System.Collections.Generic;

namespace ProjectPulse.Models
{
    [Table("Projects")]
    public class Project
    {
        [PrimaryKey]
        public Guid Id { get; set; }
        [MaxLength(100)]
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ProjectStatus Status { get; set; }
        public ProjectPriority Priority { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Additional properties
        [Ignore]
        public double CompletionPercentage { get; set; }
        [Ignore]
        public int TotalTasks { get; set; }
        [Ignore]
        public int CompletedTasks { get; set; }
        [Ignore]
        public List<User> TeamMembers { get; set; }

        public Project()
        {
            Id = Guid.NewGuid();
            Status = ProjectStatus.New;
            Priority = ProjectPriority.Medium;
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
            TeamMembers = new List<User>();
        }
    }

    [Table("UserProjects")]
    public class UserProject
    {
        [PrimaryKey]
        public Guid Id { get; set; }
        [Indexed]
        public Guid UserId { get; set; }
        [Indexed]
        public Guid ProjectId { get; set; }
        public DateTime JoinedAt { get; set; }

        public UserProject()
        {
            Id = Guid.NewGuid();
            JoinedAt = DateTime.Now;
        }
    }

    
    public enum ProjectPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
}

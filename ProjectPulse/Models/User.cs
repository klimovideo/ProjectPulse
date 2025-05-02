using SQLite;
using System;
using System.Collections.Generic;

namespace ProjectPulse.Models
{
    [Table("Users")]
    public class User
    {
        [PrimaryKey]
        public Guid Id { get; set; }
        [MaxLength(100)]
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string FullName { get; set; }
        public string AvatarPath { get; set; }
        public bool UseBiometricAuth { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        
        // Additional properties for syncing with server
        [Ignore]
        public string AuthToken { get; set; }
        [Ignore]
        public bool IsSynced { get; set; }

        public User()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
        }
    }

    [Table("UserRoles")]
    public class UserRole
    {
        [PrimaryKey]
        public Guid Id { get; set; }
        [Indexed]
        public Guid UserId { get; set; }
        public RoleType Role { get; set; }
        
        public UserRole()
        {
            Id = Guid.NewGuid();
        }
    }

    public enum RoleType
    {
        Administrator,
        ProjectManager,
        Employee
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectPulse.Models;

public class UserRole
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int UserId  { get; set; }
        public RoleType Role { get; set; }

        public User User { get; set; }
    }

    public enum RoleType { Administrator, ProjectManager, Employee }

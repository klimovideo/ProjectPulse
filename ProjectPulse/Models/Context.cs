using System;
using Microsoft.EntityFrameworkCore;

namespace ProjectPulse.Models;

public class ProjectPulseContext : DbContext
{
    // ------------- DbSet'ы
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<UserProject> UserProjects => Set<UserProject>();
    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();
    public DbSet<SubTask> SubTasks => Set<SubTask>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<TeamPulse> TeamPulses => Set<TeamPulse>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();

    public ProjectPulseContext(DbContextOptions<ProjectPulseContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ---------- Индексы (пример через Fluent API, если атрибута [Index] нет)
        b.Entity<Notification>().HasIndex(x => x.UserId);
        b.Entity<TeamPulse>().HasIndex(x => new { x.UserId, x.ProjectId });
        b.Entity<ProjectTask>().HasIndex(x => new { x.ProjectId, x.AssignedTo });
        b.Entity<UserProject>().HasIndex(x => new { x.UserId, x.ProjectId }).IsUnique();

        // ---------- Связи & каскады -------------------------------------

        // UserRole
        b.Entity<UserRole>()
         .HasOne(r => r.User)
         .WithMany(u => u.Roles)
         .HasForeignKey(r => r.UserId)
         .OnDelete(DeleteBehavior.Cascade);

        // Project
        b.Entity<Project>()
         .HasOne(p => p.Creator)
         .WithMany(u => u.ProjectsOwned)
         .HasForeignKey(p => p.CreatedBy)
         .OnDelete(DeleteBehavior.Restrict);

        // UserProject (многие-ко-многим без join-table)
        b.Entity<UserProject>()
         .HasOne(up => up.User)
         .WithMany(u => u.UserProjects)
         .HasForeignKey(up => up.UserId)
         .OnDelete(DeleteBehavior.Cascade);

        b.Entity<UserProject>()
         .HasOne(up => up.Project)
         .WithMany(p => p.UserProjects)
         .HasForeignKey(up => up.ProjectId)
         .OnDelete(DeleteBehavior.Cascade);

        // ProjectTask
        b.Entity<ProjectTask>()
         .HasOne(t => t.Project)
         .WithMany(p => p.Tasks)
         .HasForeignKey(t => t.ProjectId)
         .OnDelete(DeleteBehavior.Cascade);

        b.Entity<ProjectTask>()
         .HasOne(t => t.AssignedUser)
         .WithMany(u => u.AssignedTasks)
         .HasForeignKey(t => t.AssignedTo)
         .OnDelete(DeleteBehavior.Restrict);

        b.Entity<ProjectTask>()
         .HasOne(t => t.Creator)
         .WithMany(u => u.CreatedTasks)
         .HasForeignKey(t => t.CreatedBy)
         .OnDelete(DeleteBehavior.Restrict);

        // SubTask
        b.Entity<SubTask>()
         .HasOne(st => st.ProjectTask)
         .WithMany(t => t.SubTasks)
         .HasForeignKey(st => st.ProjectTaskId)
         .OnDelete(DeleteBehavior.Cascade);

        // Comment
        b.Entity<Comment>()
         .HasOne(c => c.ProjectTask)
         .WithMany(t => t.Comments)
         .HasForeignKey(c => c.ProjectTaskId)
         .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Comment>()
         .HasOne(c => c.User)
         .WithMany(u => u.Comments)
         .HasForeignKey(c => c.UserId)
         .OnDelete(DeleteBehavior.Cascade);

        // Notification
        b.Entity<Notification>()
         .HasOne(n => n.User)
         .WithMany(u => u.Notifications)
         .HasForeignKey(n => n.UserId)
         .OnDelete(DeleteBehavior.Cascade);

        // TeamPulse
        b.Entity<TeamPulse>()
         .HasOne(tp => tp.User)
         .WithMany(u => u.TeamPulses)
         .HasForeignKey(tp => tp.UserId)
         .OnDelete(DeleteBehavior.Restrict);

        b.Entity<TeamPulse>()
         .HasOne(tp => tp.Project)
         .WithMany(p => p.TeamPulses)
         .HasForeignKey(tp => tp.ProjectId)
         .OnDelete(DeleteBehavior.Cascade);

        b.Entity<CalendarEvent>()
        .HasIndex(e => e.ExternalId)
        .IsUnique(false);
    }
}

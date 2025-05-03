using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectPulse.Models;

namespace ProjectPulse.Services;

public class ProjectService
{
    private readonly ProjectPulseContext _db;
    private readonly NotificationService _notificationService;

    public ProjectService(ProjectPulseContext dbContext, NotificationService notificationService)
    {
        _db = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    public async Task<List<Project>> GetAllProjectsAsync()
    {
        var projects = await _db.Projects
                                .Include(p => p.UserProjects)
                                    .ThenInclude(up => up.User)
                                .Include(p => p.Tasks)
                                .ToListAsync();

        projects.ForEach(EnrichProjectMetrics);
        return projects;
    }

    public Task<List<Project>> GetProjectsByStatusAsync(ProjectStatus status) =>
        _db.Projects
           .Where(p => p.Status == status)
           .Include(p => p.UserProjects)
               .ThenInclude(up => up.User)
           .Include(p => p.Tasks)
           .AsNoTracking()
           .ToListAsync();

    public async Task<List<Project>> GetProjectsByUserAsync(int userId)
    {
        var ids = await _db.Projects
                           .Where(p => p.CreatedBy == userId)
                           .Select(p => p.Id)
                           .Union(
                               _db.UserProjects
                                  .Where(up => up.UserId == userId)
                                  .Select(up => up.ProjectId))
                           .ToListAsync();           // материализуем выборку

        var projects = await _db.Projects
                                .Where(p => ids.Contains(p.Id))
                                .Include(p => p.UserProjects)
                                    .ThenInclude(up => up.User)
                                .Include(p => p.Tasks)
                                .ToListAsync();

        projects.ForEach(EnrichProjectMetrics);
        return projects;
    }

    public async Task<Project?> GetProjectByIdAsync(int projectId)
    {
        var project = await _db.Projects
                               .Include(p => p.UserProjects)
                                   .ThenInclude(up => up.User)
                               .Include(p => p.Tasks)
                               .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project is not null) EnrichProjectMetrics(project);
        return project;
    }

    public async Task<bool> CreateProjectAsync(Project project, IEnumerable<int> teamMemberIds)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.Now;
            project.CreatedAt = now;
            project.UpdatedAt = now;

            await _db.Projects.AddAsync(project);
            await _db.SaveChangesAsync();

            foreach (var uid in teamMemberIds)
            {
                await _db.UserProjects.AddAsync(new UserProject
                {
                    UserId = uid,
                    ProjectId = project.Id,
                    JoinedAt = now
                });

                await _notificationService.CreateNotificationAsync(
                    uid,
                    "Added to Project",
                    $"You have been added to project: {project.Name}",
                    NotificationType.ProjectCreated,
                    project.Id);
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ProjectService] CreateProjectAsync: {ex}");
            await tx.RollbackAsync();
            return false;
        }
    }

    public async Task<bool> UpdateProjectAsync(Project project, IEnumerable<int> teamMemberIds)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var original = await _db.Projects.AsNoTracking().FirstAsync(p => p.Id == project.Id);
            bool statusChanged = original.Status != project.Status;

            project.UpdatedAt = DateTime.Now;
            _db.Projects.Update(project);

            var existingMembers = await _db.UserProjects
                                           .Where(up => up.ProjectId == project.Id)
                                           .ToListAsync();

            _db.UserProjects.RemoveRange(existingMembers);

            foreach (var uid in teamMemberIds)
            {
                await _db.UserProjects.AddAsync(new UserProject
                {
                    UserId = uid,
                    ProjectId = project.Id,
                    JoinedAt = DateTime.Now
                });

                bool isNew = existingMembers.All(em => em.UserId != uid);
                if (isNew)
                {
                    await _notificationService.CreateNotificationAsync(
                        uid,
                        "Added to Project",
                        $"You have been added to project: {project.Name}",
                        NotificationType.ProjectCreated,
                        project.Id);
                }
            }

            if (statusChanged)
            {
                foreach (var uid in teamMemberIds)
                {
                    await _notificationService.CreateNotificationAsync(
                        uid,
                        "Project Status Changed",
                        $"Project '{project.Name}' status changed to {project.Status}",
                        NotificationType.ProjectStatusChanged,
                        project.Id);
                }
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ProjectService] UpdateProjectAsync: {ex}");
            await tx.RollbackAsync();
            return false;
        }
    }

    public async Task<bool> DeleteProjectAsync(int projectId)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var project = await _db.Projects.FindAsync(projectId);
            if (project is null) return false;

            var projectComments = _db.Comments.Where(c => c.ProjectId == projectId);
            _db.Comments.RemoveRange(projectComments);

            _db.Projects.Remove(project);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ProjectService] DeleteProjectAsync: {ex}");
            await tx.RollbackAsync();
            return false;
        }
    }

    public async Task<List<TeamPulse>> GetTeamPulseForProjectAsync(int projectId) =>
        await _db.TeamPulses
                 .Where(tp => tp.ProjectId == projectId)
                 .Include(tp => tp.User)
                 .OrderByDescending(tp => tp.CreatedAt)
                 .AsNoTracking()
                 .ToListAsync();

    public async Task<double> GetAverageTeamPulseAsync(int projectId)
    {
        var sevenDaysAgo = DateTime.Now.AddDays(-7);
        var recent = await _db.TeamPulses
                              .Where(tp => tp.ProjectId == projectId && tp.CreatedAt >= sevenDaysAgo)
                              .ToListAsync();

        return recent.Count == 0 ? 0 : recent.Average(tp => tp.MoodRating);
    }

    public async Task<bool> AddTeamPulseEntryAsync(TeamPulse entry)
    {
        try
        {
            entry.CreatedAt = DateTime.Now;
            await _db.TeamPulses.AddAsync(entry);
            await _db.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ------------------- локальные вспомогательные методы -------------------
    private static void EnrichProjectMetrics(Project p)
    {
        var taskSet = p.Tasks ?? Array.Empty<ProjectTask>();
        p.TotalTasks = taskSet.Count;
        p.CompletedTasks = taskSet.Count(t => t.Status == ProjectStatus.Completed);
        p.CompletionPercentage = p.TotalTasks == 0
            ? 0
            : (double)p.CompletedTasks / p.TotalTasks * 100;
    }

}

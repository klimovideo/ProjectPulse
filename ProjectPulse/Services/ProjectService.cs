using ProjectPulse.Models;
using ProjectPulse.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace ProjectPulse.Services
{
    public class ProjectService
    {
        private readonly DatabaseService _database;
        private readonly NotificationService _notificationService;

        public ProjectService(NotificationService? notificationService = null)
        {
            _database = DatabaseService.Instance;
            _notificationService = notificationService;
        }

        public async Task<List<Project>> GetAllProjectsAsync()
        {
            var projects = await _database.GetAllAsync<Project>();
            
            // Load project managers
            foreach (var project in projects)
            {
                var users = await _database.GetItemsAsync<User>("SELECT * FROM Users WHERE Id = ?", project.CreatedBy);
                if (users.Count > 0)
                {
                    // Load team members
                    var userProjects = await _database.GetItemsAsync<UserProject>("SELECT * FROM UserProjects WHERE ProjectId = ?", project.Id);
                    project.TeamMembers = new List<User>();
                    
                    foreach (var userProject in userProjects)
                    {
                        var teamMembers = await _database.GetItemsAsync<User>("SELECT * FROM Users WHERE Id = ?", userProject.UserId);
                        if (teamMembers.Count > 0)
                        {
                            project.TeamMembers.Add(teamMembers[0]);
                        }
                    }
                    
                    // Calculate completion
                    var tasks = await _database.GetItemsAsync<ProjectTask>("SELECT * FROM Tasks WHERE ProjectId = ?", project.Id);
                    project.TotalTasks = tasks.Count;
                    project.CompletedTasks = tasks.Count(t => t.Status == ProjectStatus.Completed);
                    project.CompletionPercentage = project.TotalTasks > 0 
                        ? (double)project.CompletedTasks / project.TotalTasks * 100 
                        : 0;
                }
            }
            
            return projects;
        }

        public async Task<List<Project>> GetProjectsByStatusAsync(ProjectStatus status)
        {
            return await _database.GetItemsAsync<Project>("SELECT * FROM Projects WHERE Status = ?", (int)status);
        }

        public async Task<List<Project>> GetProjectsByUserAsync(int userId)
        {
            // Get projects where user is a manager
            var managerProjects = await _database.GetItemsAsync<Project>("SELECT * FROM Projects WHERE ManagerId = ?", userId);
            
            // Get projects where user is a team member
            var userProjects = await _database.GetItemsAsync<UserProject>("SELECT * FROM UserProjects WHERE UserId = ?", userId);
            var teamMemberProjectIds = userProjects.Select(up => up.ProjectId);
            
            if (teamMemberProjectIds.Any())
            {
                var placeholders = string.Join(",", teamMemberProjectIds.Select(p => "?"));
                var memberProjects = await _database.GetItemsAsync<Project>($"SELECT * FROM Projects WHERE Id IN ({placeholders})", 
                    teamMemberProjectIds.Cast<object>().ToArray());
                
                // Combine the lists
                managerProjects.AddRange(memberProjects);
            }
            
            return managerProjects.Distinct().ToList();
        }

        public async Task<Project> GetProjectByIdAsync(int projectId)
        {
            var project = await _database.GetItemAsync<Project>(projectId);
            if (project != null)
            {
                // Load team members
                var userProjects = await _database.GetItemsAsync<UserProject>("SELECT * FROM UserProjects WHERE ProjectId = ?", project.Id);
                project.TeamMembers = new List<User>();
                
                foreach (var userProject in userProjects)
                {
                    var teamMembers = await _database.GetItemsAsync<User>("SELECT * FROM Users WHERE Id = ?", userProject.UserId);
                    if (teamMembers.Count > 0)
                    {
                        project.TeamMembers.Add(teamMembers[0]);
                    }
                }
                
                // Calculate completion
                var tasks = await _database.GetItemsAsync<Models.Task>("SELECT * FROM Tasks WHERE ProjectId = ?", project.Id);
                project.TotalTasks = tasks.Count;
                project.CompletedTasks = tasks.Count(t => t.Status == Models.Status.Completed);
                project.CompletionPercentage = project.TotalTasks > 0 
                    ? (double)project.CompletedTasks / project.TotalTasks * 100 
                    : 0;
            }
            
            return project;
        }

        public async Task<bool> CreateProjectAsync(Project project, List<int> teamMemberIds)
        {
            try
            {
                // Save the project
                project.CreatedAt = DateTime.Now;
                project.ModifiedAt = DateTime.Now;
                await _database.SaveItemAsync(project);
                
                // Add team members
                foreach (var userId in teamMemberIds)
                {
                    var userProject = new UserProject
                    {
                        UserId = userId,
                        ProjectId = project.Id,
                        JoinedAt = DateTime.Now
                    };
                    await _database.SaveItemAsync(userProject);
                    
                    // Send notification to team member
                    if (_notificationService != null)
                    {
                        await _notificationService.CreateNotificationAsync(
                            userId,
                            "Added to Project",
                            $"You have been added to project: {project.Name}",
                            NotificationType.ProjectCreated,
                            project.Id);
                    }
                }
                
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateProjectAsync(Project project, List<int> teamMemberIds)
        {
            try
            {
                // Get the original project to check for status change
                var originalProject = await _database.GetItemAsync<Project>(project.Id);
                bool statusChanged = originalProject.Status != project.Status;
                
                // Update project
                project.ModifiedAt = DateTime.Now;
                await _database.SaveItemAsync(project);
                
                // Update team members
                // First remove existing team members
                var existingMembers = await _database.GetItemsAsync<UserProject>("SELECT * FROM UserProjects WHERE ProjectId = ?", project.Id);
                foreach (var member in existingMembers)
                {
                    await _database.DeleteItemAsync(member);
                }
                
                // Add new team members
                foreach (var userId in teamMemberIds)
                {
                    var userProject = new UserProject
                    {
                        UserId = userId,
                        ProjectId = project.Id,
                        JoinedAt = DateTime.Now
                    };
                    await _database.SaveItemAsync(userProject);
                    
                    // Send notification if user is newly added
                    if (_notificationService != null && !existingMembers.Any(m => m.UserId == userId))
                    {
                        await _notificationService.CreateNotificationAsync(
                            userId,
                            "Added to Project",
                            $"You have been added to project: {project.Name}",
                            NotificationType.ProjectCreated,
                            project.Id);
                    }
                }
                
                // If status changed, notify team members
                if (statusChanged && _notificationService != null)
                {
                    foreach (var userId in teamMemberIds)
                    {
                        await _notificationService.CreateNotificationAsync(
                            userId,
                            "Project Status Changed",
                            $"Project '{project.Name}' status changed to {project.Status}",
                            NotificationType.ProjectStatusChanged,
                            project.Id);
                    }
                }
                
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteProjectAsync(int projectId)
        {
            try
            {
                var project = await _database.GetItemAsync<Project>(projectId);
                if (project == null)
                {
                    return false;
                }
                
                // Delete all related tasks
                var tasks = await _database.GetItemsAsync<Models.Task>("SELECT * FROM Tasks WHERE ProjectId = ?", projectId);
                foreach (var task in tasks)
                {
                    // Delete subtasks
                    await _database.GetItemsAsync<SubTask>("DELETE FROM SubTasks WHERE TaskId = ?", task.Id);
                    
                    // Delete task comments
                    await _database.GetItemsAsync<Comment>("DELETE FROM Comments WHERE TaskId = ?", task.Id);
                    
                    // Delete task
                    await _database.DeleteItemAsync(task);
                }
                
                // Delete project comments
                await _database.GetItemsAsync<Comment>("DELETE FROM Comments WHERE ProjectId = ?", projectId);
                
                // Delete team pulse entries
                await _database.GetItemsAsync<TeamPulse>("DELETE FROM TeamPulse WHERE ProjectId = ?", projectId);
                
                // Delete user-project relations
                await _database.GetItemsAsync<UserProject>("DELETE FROM UserProjects WHERE ProjectId = ?", projectId);
                
                // Delete project
                await _database.DeleteItemAsync(project);
                
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<TeamPulse>> GetTeamPulseForProjectAsync(int projectId)
        {
            var pulseEntries = await _database.GetItemsAsync<TeamPulse>(
                "SELECT * FROM TeamPulse WHERE ProjectId = ? ORDER BY CreatedAt DESC", projectId);
            
            // Load user information
            foreach (var entry in pulseEntries)
            {
                var users = await _database.GetItemsAsync<User>("SELECT * FROM Users WHERE Id = ?", entry.UserId);
                if (users.Count > 0)
                {
                    entry.User = users[0];
                }
            }
            
            return pulseEntries;
        }

        public async Task<double> GetAverageTeamPulseAsync(int projectId)
        {
            var recentPulses = await _database.GetItemsAsync<TeamPulse>(
                "SELECT * FROM TeamPulse WHERE ProjectId = ? AND CreatedAt >= ?", 
                projectId, DateTime.Now.AddDays(-7)); // Last 7 days
            
            if (recentPulses.Count == 0)
                return 0;
                
            return recentPulses.Average(p => p.MoodRating);
        }

        public async Task<bool> AddTeamPulseEntryAsync(TeamPulse pulseEntry)
        {
            try
            {
                pulseEntry.CreatedAt = DateTime.Now;
                await _database.SaveItemAsync(pulseEntry);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

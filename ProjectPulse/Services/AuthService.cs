using System;
using System.Threading.Tasks;
using ProjectPulse.Models;
using ProjectPulse.Database;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using BCrypt.Net;

namespace ProjectPulse.Services
{
    public class AuthService
    {
        private readonly DatabaseService _database;
        private User? _currentUser;
        private const string SecretKey = "ProjectPulseSecretKey2025"; // In production, this should be stored securely

        public AuthService(DatabaseService database)
        {
            _database = database;
        }

        public User? CurrentUser => _currentUser;

        public async Task<User?> LoginAsync(string username, string password)
        {
            try
            {
                var users = await _database.GetItemsAsync<User>("SELECT * FROM Users WHERE Username = ? OR Email = ?", username, username);
                if (users.Count == 0)
                {
                    return null;
                }

                var user = users[0];
                if (BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    user.LastLogin = DateTime.Now;
                    await _database.SaveItemAsync(user);
                    
                    // Generate token
                    user.AuthToken = GenerateJwtToken(user);
                    
                    _currentUser = user;
                    return user;
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error logging in: {ex.Message}");
                throw;
            }
        }

        public async Task<User?> RegisterUserAsync(string username, string email, string password, string firstName, string lastName)
        {
            try
            {
                // Check if user already exists
                var existingUsers = await _database.GetItemsAsync<User>("SELECT * FROM Users WHERE Username = ? OR Email = ?", username, email);
                if (existingUsers.Count > 0)
                {
                    return null;
                }

                // Create new user
                var user = new User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    FirstName = firstName,
                    LastName = lastName,
                    CreatedAt = DateTime.Now
                };

                await _database.SaveItemAsync(user);

                // By default, new user gets Employee role
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    Role = RoleType.Employee
                };
                await _database.SaveItemAsync(userRole);

                _currentUser = user;
                return user;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error registering user: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> CheckBiometricAvailabilityAsync(string username)
        {
            try
            {
                var users = await _database.GetItemsAsync<User>("SELECT * FROM Users WHERE Username = ? OR Email = ?", username, username);
                if (users.Count == 0)
                {
                    return false;
                }

                return users[0].UseBiometricAuth;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> LoginWithBiometricAsync(string username)
        {
            try
            {
                var users = await _database.GetItemsAsync<User>("SELECT * FROM Users WHERE Username = ? OR Email = ? AND UseBiometricAuth = 1", 
                    username, username);
                
                if (users.Count == 0)
                    return false;
                
                var user = users[0];
                user.LastLogin = DateTime.Now;
                await _database.SaveItemAsync(user);
                
                // Generate token
                user.AuthToken = GenerateJwtToken(user);
                _currentUser = user;
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> LogoutAsync()
        {
            try
            {
                _currentUser = null;
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error logging out: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> EnableBiometricAuthAsync(int userId)
        {
            try
            {
                var user = await _database.GetItemAsync<User>(userId);
                if (user == null)
                    return false;

                user.UseBiometricAuth = true;
                await _database.SaveItemAsync(user);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<RoleType> GetUserRoleAsync(int userId)
        {
            try
            {
                var userRoles = await _database.GetItemsAsync<UserRole>("SELECT * FROM UserRoles WHERE UserId = ?", userId);
                return userRoles.Count > 0 ? userRoles[0].Role : RoleType.Employee;
            }
            catch
            {
                return RoleType.Employee;
            }
        }

        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("username", user.Username)
            };

            var token = new JwtSecurityToken(
                issuer: "ProjectPulse",
                audience: "MobileApp",
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

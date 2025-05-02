using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectPulse.Models;        // User, UserRole, RoleType
using System.Diagnostics;
using BCrypt.Net;
namespace ProjectPulse.Services
{
    public class AuthService
    {
        private readonly ProjectPulseContext _db;
        private User? _currentUser;

        // В продакшене хранить в безопасном хранилище
        private const string SecretKey = "ProjectPulseSecretKey2025";

        public AuthService(ProjectPulseContext dbContext)
        {
            _db = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public User? CurrentUser => _currentUser;

        public async Task<User?> LoginAsync(string email, string password)
        {
            try
            {
                var user = await _db.Users
                                    .FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                    return null;

                if (!BCrypt.Verify(password, user.PasswordHash))
                    return null;

                user.LastLogin = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                // Сохраняем токен в свойстве (не в БД)
                user.AuthToken = GenerateJwtToken(user);
                _currentUser = user;
                return user;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AuthService] LoginAsync: {ex}");
                throw;
            }
        }

        public async Task<User?> RegisterUserAsync(
            string email,
            string password,
            string fullName,
            string? avatarPath = null)
        {
            try
            {
                // Проверяем существующий e-mail
                if (await _db.Users.AnyAsync(u => u.Email == email))
                    return null;

                var now = DateTime.UtcNow;
                var user = new User
                {
                    Email            = email,
                    PasswordHash     = BCrypt.Net.BCrypt.HashPassword(password),
                    FullName         = fullName,
                    AvatarPath       = avatarPath ?? string.Empty,
                    UseBiometricAuth = false,
                    CreatedAt        = now,
                    UpdatedAt        = now,
                };

                await _db.Users.AddAsync(user);
                await _db.SaveChangesAsync();

                // Назначаем роль Employee
                var role = new UserRole
                {
                    UserId = user.Id,
                    Role   = RoleType.Employee
                };
                await _db.UserRoles.AddAsync(role);
                await _db.SaveChangesAsync();

                _currentUser = user;
                return user;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AuthService] RegisterUserAsync: {ex}");
                throw;
            }
        }

        public async Task<bool> CheckBiometricAvailabilityAsync(string email)
        {
            try
            {
                return await _db.Users
                                .Where(u => u.Email == email)
                                .Select(u => u.UseBiometricAuth)
                                .FirstOrDefaultAsync();
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> LoginWithBiometricAsync(string email)
        {
            try
            {
                var user = await _db.Users
                                    .FirstOrDefaultAsync(u => u.Email == email && u.UseBiometricAuth);
                if (user == null)
                    return false;

                user.LastLogin = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                user.AuthToken = GenerateJwtToken(user);
                _currentUser = user;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Task<bool> LogoutAsync()
        {
            _currentUser = null;
            return Task.FromResult(true);
        }

        public async Task<bool> EnableBiometricAuthAsync(int userId)
        {
            try
            {
                var user = await _db.Users.FindAsync(userId);
                if (user == null)
                    return false;

                user.UseBiometricAuth = true;
                user.UpdatedAt        = DateTime.UtcNow;
                await _db.SaveChangesAsync();
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
                var role = await _db.UserRoles
                                    .Where(r => r.UserId == userId)
                                    .Select(r => r.Role)
                                    .FirstOrDefaultAsync();
                return role == default ? RoleType.Employee : role;
            }
            catch
            {
                return RoleType.Employee;
            }
        }

        private string GenerateJwtToken(User user)
        {
            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
                new Claim("fullName",                    user.FullName)
            };

            var token = new JwtSecurityToken(
                issuer:   "ProjectPulse",
                audience: "MobileApp",
                claims:   claims,
                expires:  DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

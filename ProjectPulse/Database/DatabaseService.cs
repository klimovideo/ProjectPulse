using SQLite;
using ProjectPulse.Models;
using System.Collections.Generic;

namespace ProjectPulse.Database
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _database;
        private static DatabaseService _instance;
        public static string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, "projectpulse.db3");

        private DatabaseService()
        {
        }

        public static DatabaseService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DatabaseService();
                }
                return _instance;
            }
        }

        public async System.Threading.Tasks.Task InitializeAsync()
        {
            if (_database != null)
            {
                return;
            }

            _database = new SQLiteAsyncConnection(DatabasePath);
            await _database.CreateTableAsync<User>();
            await _database.CreateTableAsync<Project>();
            await _database.CreateTableAsync<ProjectTask>();
            await _database.CreateTableAsync<SubTask>();
            await _database.CreateTableAsync<Comment>();
            await _database.CreateTableAsync<Notification>();
            await _database.CreateTableAsync<TeamPulse>();
            await _database.CreateTableAsync<UserRole>();
            await _database.CreateTableAsync<UserProject>();
        }

        // Generic methods for database operations
        public async Task<List<T>> GetAllAsync<T>() where T : new()
        {
            await InitializeAsync();
            return await _database.Table<T>().ToListAsync();
        }

        public async Task<List<T>> GetItemsAsync<T>(string query, params object[] args) where T : new()
        {
            await InitializeAsync();
            return await _database.QueryAsync<T>(query, args);
        }

        public async Task<T> GetItemAsync<T>(int id) where T : class, new()
        {
            await InitializeAsync();
            return await _database.GetAsync<T>(id);
        }

        public async Task<int> SaveItemAsync<T>(T item)
        {
            await InitializeAsync();
            if (typeof(T).GetProperty("Id")?.GetValue(item) is int id && id != 0)
            {
                return await _database.UpdateAsync(item);
            }
            else
            {
                return await _database.InsertAsync(item);
            }
        }

        public async Task<int> DeleteItemAsync<T>(T item)
        {
            await InitializeAsync();
            return await _database.DeleteAsync(item);
        }

        public async Task<int> DeleteAllAsync<T>() where T : new()
        {
            await InitializeAsync();
            return await _database.DeleteAllAsync<T>();
        }
    }
}

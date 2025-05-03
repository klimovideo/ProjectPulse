using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectPulse.Services;
using System;
using System.Threading.Tasks;

namespace ProjectPulse.ViewModels
{
    public partial class RegisterViewModel : BaseViewModel
    {
        private readonly AuthService _authService;

        [ObservableProperty]
        private string username;

        [ObservableProperty]
        private string email;

        [ObservableProperty]
        private string password;

        [ObservableProperty]
        private string confirmPassword;

        [ObservableProperty]
        private string firstName;

        [ObservableProperty]
        private string lastName;

        [ObservableProperty]
        private bool useBiometricAuth;

        public RegisterViewModel(AuthService authService)
        {
            _authService = authService;
            Title = "Регистрация";
            Username = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;
        }

        [RelayCommand]
        public async Task Register()
        {
            if (string.IsNullOrWhiteSpace(Username) || 
                string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password) ||
                string.IsNullOrWhiteSpace(ConfirmPassword) ||
                string.IsNullOrWhiteSpace(FirstName) ||
                string.IsNullOrWhiteSpace(LastName))
            {
                ShowError("Пожалуйста, заполните все поля");
                return;
            }

            if (Password != ConfirmPassword)
            {
                ShowError("Пароли не совпадают");
                return;
            }

            if (!IsValidEmail(Email))
            {
                ShowError("Введите корректный email");
                return;
            }

            try
            {
                IsBusy = true;
                ClearError();

                var result = await _authService.RegisterUserAsync(Username, Email, Password, firstName + " " + lastName);
                if (result != null)
                {
                    // If registration successful, attempt to login
                    var loginResult = await _authService.LoginAsync(Username, Password);
                    
                    if (loginResult != null)
                    {
                        // If biometric auth is enabled, configure it
                        if (UseBiometricAuth)
                        {
                            await _authService.EnableBiometricAuthAsync(_authService.CurrentUser.Id);
                            
                            // Save username to secure storage
                            await SecureStorage.SetAsync("username", Username);
                        }
                        
                        // Navigate to main app
                        await Shell.Current.GoToAsync("//DashboardPage");
                    }
                    else
                    {
                        // If login fails, go back to login page
                        await Shell.Current.GoToAsync("..");
                    }
                }
                else
                {
                    ShowError("Не удалось зарегистрироваться. Возможно, пользователь с таким именем или email уже существует.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка регистрации: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task Back()
        {
            await Shell.Current.GoToAsync("..");
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}

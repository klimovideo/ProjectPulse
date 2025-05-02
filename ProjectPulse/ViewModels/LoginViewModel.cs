using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectPulse.Services;
using ProjectPulse.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage; // Added for Preferences
using System.Diagnostics;

namespace ProjectPulse.ViewModels
{
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly AuthService _authService;

        [ObservableProperty]
        private string username;

        [ObservableProperty]
        private string password;

        [ObservableProperty]
        private bool isRememberMe;

        public LoginViewModel(AuthService authService)
        {
            _authService = authService;
            Title = "Вход в систему";
        }

        [RelayCommand]
        public async Task Login()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                ClearError();

                if (string.IsNullOrWhiteSpace(Username))
                {
                    ShowError("Пожалуйста, введите имя пользователя");
                    return;
                }

                if (string.IsNullOrWhiteSpace(Password))
                {
                    ShowError("Пожалуйста, введите пароль");
                    return;
                }

                var user = await _authService.LoginAsync(Username, Password);
                if (user != null)
                {
                    if (IsRememberMe)
                    {
                        Preferences.Set("RememberMe", true);
                        Preferences.Set("Username", Username);
                    }
                    else
                    {
                        Preferences.Set("RememberMe", false);
                        Preferences.Remove("Username");
                    }

                    await Shell.Current.GoToAsync("//DashboardPage");
                }
                else
                {
                    ShowError("Неверное имя пользователя или пароль");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error logging in: {ex.Message}");
                ShowError("Произошла ошибка при входе в систему");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task Register()
        {
            await Shell.Current.GoToAsync("//RegisterPage");
        }

        [RelayCommand]
        public async Task ForgotPassword()
        {
            await Shell.Current.GoToAsync("//ForgotPasswordPage");
        }

        [RelayCommand]
        public void LoadSavedCredentials()
        {
            try
            {
                var rememberMe = Preferences.Get("RememberMe", false);
                if (rememberMe)
                {
                    Username = Preferences.Get("Username", string.Empty);
                    IsRememberMe = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading saved credentials: {ex.Message}");
            }
        }

        [RelayCommand]
        public void ToggleRememberMe()
        {
            IsRememberMe = !IsRememberMe;
        }

        public override void OnAppearing()
        {
            base.OnAppearing();
            
            // Check for saved credentials
            LoadSavedCredentials();
        }
    }
}

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectPulse.Models;
using ProjectPulse.Services;
using ProjectPulse.Views;
using Task = System.Threading.Tasks.Task;

namespace ProjectPulse.ViewModels
{
    public partial class ProfileViewModel : BaseViewModel
    {
        private readonly AuthService _authService;
        
        [ObservableProperty]
        private User currentUser;
        
        [ObservableProperty]
        private bool isEditing;
        
        [ObservableProperty]
        private string fullName;
        
        [ObservableProperty]
        private string email;
        
        [ObservableProperty]
        private string currentPassword;
        
        [ObservableProperty]
        private string newPassword;
        
        [ObservableProperty]
        private string confirmPassword;
        
        [ObservableProperty]
        private bool isChangingPassword;
        
        public ProfileViewModel(AuthService authService)
        {
            _authService = authService;
            
            Title = "Профиль";
            CurrentUser = AuthService.CurrentUser;
            IsEditing = false;
            IsChangingPassword = false;
        }
        
        public override void OnAppearing()
        {
            base.OnAppearing();
            
            CurrentUser = AuthService.CurrentUser;
            
            if (CurrentUser != null)
            {
                FullName = CurrentUser.FullName;
                Email = CurrentUser.Email;
            }
        }
        
        [RelayCommand]
        public void ToggleEditMode()
        {
            IsEditing = !IsEditing;
            IsChangingPassword = false;
            
            if (!IsEditing)
            {
                // Reset fields if cancelling edit
                FullName = CurrentUser?.FullName ?? string.Empty;
                Email = CurrentUser?.Email ?? string.Empty;
                CurrentPassword = string.Empty;
                NewPassword = string.Empty;
                ConfirmPassword = string.Empty;
            }
        }
        
        [RelayCommand]
        public void TogglePasswordChange()
        {
            IsChangingPassword = !IsChangingPassword;
            
            if (!IsChangingPassword)
            {
                // Clear password fields
                CurrentPassword = string.Empty;
                NewPassword = string.Empty;
                ConfirmPassword = string.Empty;
            }
        }
        
        [RelayCommand]
        public async Task SaveProfile()
        {
            if (IsBusy || CurrentUser == null)
                return;
                
            if (string.IsNullOrWhiteSpace(FullName))
            {
                ShowError("Имя не может быть пустым");
                return;
            }
            
            if (string.IsNullOrWhiteSpace(Email) || !Email.Contains("@"))
            {
                ShowError("Введите корректный email");
                return;
            }
            
            if (IsChangingPassword)
            {
                if (string.IsNullOrWhiteSpace(CurrentPassword))
                {
                    ShowError("Введите текущий пароль");
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
                {
                    ShowError("Новый пароль должен содержать не менее 6 символов");
                    return;
                }
                
                if (NewPassword != ConfirmPassword)
                {
                    ShowError("Пароли не совпадают");
                    return;
                }
            }
            
            try
            {
                IsBusy = true;
                ClearError();
                
                // Update basic profile info
                CurrentUser.FullName = FullName;
                
                // Only update email if it changed
                if (Email != CurrentUser.Email)
                {
                    // Check if email is already in use
                    var existingUser = await _authService.GetUserByEmailAsync(Email);
                    if (existingUser != null && existingUser.Id != CurrentUser.Id)
                    {
                        ShowError("Этот email уже используется");
                        return;
                    }
                    
                    CurrentUser.Email = Email;
                }
                
                // Update password if requested
                if (IsChangingPassword)
                {
                    var passwordVerified = await _authService.VerifyPasswordAsync(CurrentUser.Email, CurrentPassword);
                    if (!passwordVerified)
                    {
                        ShowError("Неверный текущий пароль");
                        return;
                    }
                    
                    await _authService.ChangePasswordAsync(CurrentUser.Id, NewPassword);
                }
                
                // Save user changes
                await _authService.UpdateUserAsync(CurrentUser);
                
                // Update the current user in the service
                AuthService.CurrentUser = CurrentUser;
                
                IsEditing = false;
                IsChangingPassword = false;
                
                // Clear sensitive fields
                CurrentPassword = string.Empty;
                NewPassword = string.Empty;
                ConfirmPassword = string.Empty;
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при сохранении профиля: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        [RelayCommand]
        public async Task Logout()
        {
            if (IsBusy)
                return;
                
            try
            {
                IsBusy = true;
                
                bool confirmed = await Shell.Current.DisplayAlert(
                    "Выход", 
                    "Вы уверены, что хотите выйти из аккаунта?", 
                    "Да", "Нет");
                    
                if (confirmed)
                {
                    await _authService.LogoutAsync();
                    await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при выходе из аккаунта: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}

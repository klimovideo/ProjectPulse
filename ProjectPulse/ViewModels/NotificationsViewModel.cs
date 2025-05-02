using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectPulse.Models;
using ProjectPulse.Services;
using System.Collections.ObjectModel;
using Task = System.Threading.Tasks.Task;

namespace ProjectPulse.ViewModels
{
    public partial class NotificationsViewModel : BaseViewModel
    {
        private readonly NotificationService _notificationService;
        private readonly AuthService _authService;
        
        [ObservableProperty]
        private ObservableCollection<Notification> notifications;
        
        [ObservableProperty]
        private ObservableCollection<Notification> unreadNotifications;
        
        [ObservableProperty]
        private ObservableCollection<Notification> readNotifications;
        
        [ObservableProperty]
        private bool hasUnreadNotifications;
        
        [ObservableProperty]
        private bool hasReadNotifications;
        
        [ObservableProperty]
        private bool hasNotifications;
        
        public NotificationsViewModel(NotificationService notificationService, AuthService authService)
        {
            _notificationService = notificationService;
            _authService = authService;
            
            Title = "Уведомления";
            Notifications = new ObservableCollection<Notification>();
            UnreadNotifications = new ObservableCollection<Notification>();
            ReadNotifications = new ObservableCollection<Notification>();
        }
        
        public override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadNotifications();
        }
        
        [RelayCommand]
        public async Task LoadNotifications()
        {
            if (IsBusy)
                return;
                
            try
            {
                IsBusy = true;
                ClearError();
                
                var currentUser = AuthService.CurrentUser;
                if (currentUser == null)
                    return;
                    
                var allNotifications = await _notificationService.GetNotificationsByUserAsync(currentUser.Id);
                
                Notifications.Clear();
                UnreadNotifications.Clear();
                ReadNotifications.Clear();
                
                foreach (var notification in allNotifications)
                {
                    Notifications.Add(notification);
                    
                    if (notification.IsRead)
                    {
                        ReadNotifications.Add(notification);
                    }
                    else
                    {
                        UnreadNotifications.Add(notification);
                    }
                }
                
                HasUnreadNotifications = UnreadNotifications.Any();
                HasReadNotifications = ReadNotifications.Any();
                HasNotifications = Notifications.Any();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при загрузке уведомлений: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                IsRefreshing = false;
            }
        }
        
        [RelayCommand]
        public async Task MarkAsRead(Notification notification)
        {
            if (IsBusy || notification == null || notification.IsRead)
                return;
                
            try
            {
                IsBusy = true;
                
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                
                await _notificationService.UpdateNotificationAsync(notification);
                
                // Move from unread to read collection
                UnreadNotifications.Remove(notification);
                ReadNotifications.Add(notification);
                
                HasUnreadNotifications = UnreadNotifications.Any();
                HasReadNotifications = ReadNotifications.Any();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при обновлении уведомления: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        [RelayCommand]
        public async Task MarkAllAsRead()
        {
            if (IsBusy || !HasUnreadNotifications)
                return;
                
            try
            {
                IsBusy = true;
                
                var currentUser = AuthService.CurrentUser;
                if (currentUser == null)
                    return;
                
                await _notificationService.MarkAllAsReadAsync(currentUser.Id);
                
                // Update local collections
                foreach (var notification in UnreadNotifications.ToList())
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                    
                    UnreadNotifications.Remove(notification);
                    ReadNotifications.Add(notification);
                }
                
                HasUnreadNotifications = false;
                HasReadNotifications = ReadNotifications.Any();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при обновлении уведомлений: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        [RelayCommand]
        public async Task DeleteNotification(Notification notification)
        {
            if (IsBusy || notification == null)
                return;
                
            try
            {
                IsBusy = true;
                
                await _notificationService.DeleteNotificationAsync(notification.Id);
                
                // Remove from collections
                Notifications.Remove(notification);
                
                if (notification.IsRead)
                {
                    ReadNotifications.Remove(notification);
                }
                else
                {
                    UnreadNotifications.Remove(notification);
                }
                
                HasUnreadNotifications = UnreadNotifications.Any();
                HasReadNotifications = ReadNotifications.Any();
                HasNotifications = Notifications.Any();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при удалении уведомления: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        [RelayCommand]
        public async Task ClearAllNotifications()
        {
            if (IsBusy || !HasNotifications)
                return;
                
            try
            {
                IsBusy = true;
                
                bool confirmed = await Shell.Current.DisplayAlert(
                    "Очистка уведомлений", 
                    "Вы уверены, что хотите удалить все уведомления?", 
                    "Да", "Нет");
                    
                if (!confirmed)
                    return;
                
                var currentUser = AuthService.CurrentUser;
                if (currentUser == null)
                    return;
                
                await _notificationService.DeleteAllNotificationsAsync(currentUser.Id);
                
                // Clear local collections
                Notifications.Clear();
                UnreadNotifications.Clear();
                ReadNotifications.Clear();
                
                HasUnreadNotifications = false;
                HasReadNotifications = false;
                HasNotifications = false;
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при удалении уведомлений: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        [RelayCommand]
        public async Task Refresh()
        {
            IsRefreshing = true;
            await LoadNotifications();
        }
    }
}

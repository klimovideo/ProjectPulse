using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProjectPulse.ViewModels
{
    public partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool isBusy;

        [ObservableProperty]
        private string title;

        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        private string errorMessage;

        [ObservableProperty]
        private bool hasError;

        public bool IsNotBusy => !IsBusy;

        public BaseViewModel()
        {
            Title = string.Empty;
            ErrorMessage = string.Empty;
        }

        public virtual void OnAppearing()
        {
            // Base implementation does nothing, overridden in derived classes
        }

        public virtual void OnDisappearing()
        {
            // Base implementation does nothing, overridden in derived classes
        }
        
        [RelayCommand]
        public virtual Task OnAppearingCommand()
        {
            OnAppearing();
            return Task.CompletedTask;
        }

        [RelayCommand]
        public virtual Task OnDisappearingCommand()
        {
            OnDisappearing();
            return Task.CompletedTask;
        }

        public virtual void ShowError(string message)
        {
            HasError = true;
            ErrorMessage = message;
        }

        public virtual void ClearError()
        {
            HasError = false;
            ErrorMessage = string.Empty;
        }
    }
}

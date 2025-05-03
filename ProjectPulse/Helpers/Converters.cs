using System;
using System.Globalization;
using ProjectPulse.Models;

namespace ProjectPulse.Helpers
{
    public class PercentageToDecimalConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double percentage)
            {
                return percentage / 100.0;
            }
            return 0;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double decimalValue)
            {
                return decimalValue * 100.0;
            }
            return 0;
        }
    }

    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }

    public class GreaterThanZeroConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                return intValue > 0;
            }
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            ProjectStatus status;
            if (value is Models.ProjectStatus)
            {
                status = (Models.ProjectStatus) value;
                return status switch
                {
                    Models.ProjectStatus.New => Colors.Gray,
                    Models.ProjectStatus.InProgress => Colors.Yellow,
                    Models.ProjectStatus.Completed => Colors.Green,
                    Models.ProjectStatus.OnHold => Colors.Orange,
                    Models.ProjectStatus.Cancelled => Colors.Red,
                    _ => Colors.Gray
                };
            }
            return Colors.Gray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PriorityToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Models.TaskPriority priority)
            {
                return priority switch
                {
                    Models.TaskPriority.Urgent => Colors.Red,
                    Models.TaskPriority.High => Colors.Orange,
                    Models.TaskPriority.Medium => Colors.Yellow,
                    Models.TaskPriority.Low => Colors.Green,
                    _ => Colors.Gray
                };
            }
            return Colors.Gray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

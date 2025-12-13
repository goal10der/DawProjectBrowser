using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace DawProjectBrowser.Desktop.Converters
{
    /// <summary>
    /// Simple converter that checks if an object exists or not
    /// Pretty handy for enabling/disabling UI elements based on whether we have data
    /// </summary>
    public class ObjectIsNotNullConverter : IValueConverter
    {
        // Singleton pattern - makes XAML binding easier
        public static ObjectIsNotNullConverter Instance { get; } = new ObjectIsNotNullConverter();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Simple null check - return true if we have something, false if we don't
            bool hasValue = value != null;
            return hasValue;  // Could just return value != null directly, but this is clearer
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Don't really need this for this use case
            // Most of the time we're just doing one-way binding anyway
            throw new NotSupportedException("ConvertBack operation is not supported for ObjectIsNotNullConverter");
        }
    }
}
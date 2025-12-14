#nullable enable
using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace DawProjectBrowser.Desktop.Converters
{
    public class WindowStateToGeometryConverter : IValueConverter
    {
        public object? NormalValue { get; set; }
        public object? MaximizedValue { get; set; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is WindowState state)
            {
                // If the window is Maximized, return the Restore icon geometry.
                if (state == WindowState.Maximized)
                {
                    return MaximizedValue;
                }
                // Otherwise (Normal or Minimized), return the Maximize icon geometry.
                return NormalValue;
            }
            return NormalValue;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Conversion back from geometry to WindowState is not supported/needed.
            throw new NotImplementedException();
        }
    }
}
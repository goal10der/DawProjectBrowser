// DawProjectBrowser.Desktop/Converters/BooleanToGeometryConverter.cs

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DawProjectBrowser.Desktop.Converters
{
    /// <summary>
    /// Converts a boolean value to a Geometry object based on TrueValue and FalseValue properties.
    /// Used to switch between Play/Pause icons in the audio bar.
    /// </summary>
    public class BooleanToGeometryConverter : IValueConverter
    {
        public Geometry? TrueValue { get; set; }
        public Geometry? FalseValue { get; set; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isTrue)
            {
                return isTrue ? TrueValue : FalseValue;
            }

            // Return FalseValue by default if the value is null or not a bool
            return FalseValue;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // One-way binding (Geometry back to Bool is not supported)
            throw new NotSupportedException();
        }
    }
}
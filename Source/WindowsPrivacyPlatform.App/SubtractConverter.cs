using System.Globalization;
using System.Windows.Data;

namespace WindowsPrivacyPlatform.App;

public sealed class SubtractConverter : IValueConverter
{
    public double Amount { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is double width ? Math.Max(0, width - Amount) : 0d;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

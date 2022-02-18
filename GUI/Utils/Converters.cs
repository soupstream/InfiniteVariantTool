using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace InfiniteVariantTool.GUI
{

    public class NullConverter<T> : IValueConverter
    {
        public NullConverter(T nullValue, T notNullValue)
        {
            Null = nullValue;
            NotNull = notNullValue;
        }

        public T Null { get; set; }
        public T NotNull { get; set; }

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Null : NotNull;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class NullToVisibilityConverter : NullConverter<Visibility>
    {
        public NullToVisibilityConverter() : base(Visibility.Visible, Visibility.Collapsed)
        {

        }
    }

    public sealed class NullToBooleanConverter : NullConverter<bool>
    {
        public NullToBooleanConverter() : base(false, true)
        {

        }
    }

    public class BooleanConverter<T> : IValueConverter
    {
        public BooleanConverter(T trueValue, T falseValue)
        {
            True = trueValue;
            False = falseValue;
        }

        public T True { get; set; }
        public T False { get; set; }

        public virtual object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool && ((bool)value) ? True : False;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is T && EqualityComparer<T>.Default.Equals((T)value, True);
        }
    }

    public sealed class BooleanToVisibilityConverter : BooleanConverter<Visibility>
    {
        public BooleanToVisibilityConverter() : base(Visibility.Visible, Visibility.Collapsed)
        {

        }
    }
}

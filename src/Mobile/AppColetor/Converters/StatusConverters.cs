using System.Globalization;

namespace AppColetor.Converters
{
    /// <summary>
    /// Converte bool para cor de fundo de status
    /// </summary>
    public class StatusBackgroundConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool connected)
            {
                return connected
                    ? Application.Current?.Resources["Success"]
                    : Application.Current?.Resources["Danger"];
            }
            return Application.Current?.Resources["TextMuted"];
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Converte quantidade de pendentes para cor de fundo
    /// </summary>
    public class PendingBackgroundConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                if (count == 0) return Application.Current?.Resources["Success"];
                if (count < 10) return Application.Current?.Resources["Warning"];
                return Application.Current?.Resources["Danger"];
            }
            return Application.Current?.Resources["TextMuted"];
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Converte status de sync para cor de fundo
    /// </summary>
    public class SyncBackgroundConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool sincronizado)
            {
                return sincronizado
                    ? Application.Current?.Resources["Success"]
                    : Application.Current?.Resources["Warning"];
            }
            return Application.Current?.Resources["Warning"];
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Converte status de sync para texto
    /// </summary>
    public class SyncStatusTextConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool sincronizado)
            {
                return sincronizado ? "✓ Enviado" : "⏳ Pendente";
            }
            return "⏳ Pendente";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Converte status de sync para cor do texto
    /// </summary>
    public class SyncStatusColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool sincronizado)
            {
                return sincronizado
                    ? Application.Current?.Resources["Success"]
                    : Application.Current?.Resources["Warning"];
            }
            return Application.Current?.Resources["TextMuted"];
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Converte string para bool (não vazia = true)
    /// </summary>
    public class StringToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return !string.IsNullOrEmpty(value?.ToString());
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Converte null para bool (não null = true)
    /// </summary>
    public class NullToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Converte permissão USB para texto
    /// </summary>
    public class PermissionStatusConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool hasPermission)
            {
                return hasPermission ? "✓" : "🔒";
            }
            return "🔒";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
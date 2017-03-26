using System;
using System.Collections.Generic;
using System.Text;
using TEngine;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace SoundHub
{
    public sealed class StateToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            ConnectionState state = (ConnectionState)value;
            switch (state)
            {
                case ConnectionState.Connected:
                    return "Connected";
                case ConnectionState.Disconnected:
                    return "Not connected";
                case ConnectionState.LackOfResponse:
                    return "Lack of response";
            }
            return null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public sealed class StateToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            ConnectionState state = (ConnectionState)value;
            switch (state)
            {
                case ConnectionState.Connected:
                    return (Brush)Application.Current.Resources["NormalMainBrush"];
                case ConnectionState.Disconnected:
                    return (Brush)Application.Current.Resources["NormalGrayBrush"];
                case ConnectionState.LackOfResponse:
                    return (Brush)Application.Current.Resources["NormalGrayMainBrush"];
            }
            return null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public sealed class LowerCaseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return ((string)value).ToLower();
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public sealed class FloatToDataRateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return ((float)value / 1000).ToString() + " kBps";
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public sealed class ObjectToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null)
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public sealed class FloatToGainConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (float.IsNegativeInfinity((float)value))
                return "-";
            else
                return ((float)value).ToString("F2") + " dB";
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public sealed class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if ((bool)value)
                return (Brush)Application.Current.Resources["NearBlackGrayBrush"];
            else return new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public sealed class AppStateToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            AppState state = (AppState)value;
            switch (state)
            {
                case AppState.None: return "";
                case AppState.Recording: return "Recording";
                case AppState.Scanning: return "Scanning";
                case AppState.ScanningCanceling: return "Scanning canceling";
            }
            return "";
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

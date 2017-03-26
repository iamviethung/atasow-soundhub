using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Windows.Storage;

namespace SoundHub
{
    public sealed partial class OptionsFlyout : SettingsFlyout
    {
        ApplicationDataContainer localSettings;

        public OptionsFlyout()
        {
            this.InitializeComponent();
            localSettings = ApplicationData.Current.LocalSettings;

            t_guid.Text = ((Guid)localSettings.Values["Guid"]).ToString();
            t_name.Text = (string)localSettings.Values["Name"];
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            localSettings.Values["Name"] = t_name.Text;
            ChangeSettings();
        }

        private void ChangeSettings()
        {
            s_change.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }
    }
}

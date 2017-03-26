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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace SoundHub
{
    public sealed partial class RecordButton : UserControl
    {
        public event EventHandler Enable;
        public event EventHandler Disable;
        public event EventHandler Locked;
        public event EventHandler Unlocked;

        bool record = false, locked = false;

        public RecordButton()
        {
            this.InitializeComponent();
        }

        private void UserControl_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            locked = !locked;
            if (locked)
            {
                if (Locked != null) Locked(this, null);
            }
            else
                if (Unlocked != null) Unlocked(this, null);
            UpdateButton();
        }
        private void UserControl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            record = true;
            UpdateButton();
        }
        private void UserControl_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            record = false;
            UpdateButton();
        }
        void UpdateButton()
        {
            c_lock.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            if (record || locked)
            {
                if (Enable != null)
                    Enable(this, null);
                e_button.Fill = (Brush)Application.Current.Resources["NormalMainBrush"];
                e_button.Stroke = t_button.Foreground = (Brush)Application.Current.Resources["LighterMainBrush"];
                if (locked)
                {
                    c_lock.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    t_button.Text = "Double-click to unlock";
                }
                else
                    t_button.Text = "Release to stop";
            }
            else
            {
                if (Disable != null)
                    Disable(this, null);
                e_button.Fill = (Brush)Application.Current.Resources["NormalGrayBrush"];
                e_button.Stroke = t_button.Foreground = (Brush)Application.Current.Resources["LighterGrayBrush"];
                t_button.Text = "Press to record";
            }
        }

        public void UnpressButton()
        {
            record = false;
            UpdateButton();
        }
    }
}

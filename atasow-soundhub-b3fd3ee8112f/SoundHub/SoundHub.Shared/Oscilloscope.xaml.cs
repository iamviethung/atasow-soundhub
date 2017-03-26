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

using HNet.Converters;
using Windows.UI.Core;

namespace SoundHub
{
    public sealed partial class Oscilloscope : UserControl, FloatConverter
    {
        CoreDispatcher dispatcher;

        public Oscilloscope()
        {
            this.InitializeComponent();
            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
        }

        public async void Write(float[] buffer)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                p_osc.Points.Clear();

                int n = buffer.Length;
                double yconst = this.ActualHeight / 2;
                double xinc = this.ActualWidth / (n - 1);
                double x = 0;

                if (buffer.Length == 0)
                {
                    p_osc.Points.Add(new Point(0, yconst));
                    p_osc.Points.Add(new Point(this.ActualWidth, yconst));
                    return;
                }

                for (int i = 0; i < n; i++)
                {
                    p_osc.Points.Add(new Point(x, yconst * (1 - buffer[i])));
                    x += xinc;
                }
            });
        }

        public void Write(byte[] buffer)
        {
            if (buffer.Length % 4 != 0)
                throw new ArgumentException("Length must be dividable by the size of float");

            float[] fBuffer = new float[buffer.Length / 4];
            Buffer.BlockCopy(buffer, 0, fBuffer, 0, buffer.Length);
            Write(fBuffer);
        }

        public async void Silent()
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                p_osc.Points.Clear();
                p_osc.Points.Add(new Point(0, this.ActualHeight / 2));
                p_osc.Points.Add(new Point(this.ActualWidth, this.ActualHeight / 2));
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

namespace HNet.Converters
{
    public class Mixer : OutFloatConverter, FloatConverter
    {
        Stopwatch watch;
        float[] data;
        bool cancel = false;
        int current = 1;

        public Mixer(FloatConverter innerConverter, TimeSpan period, int frameSize)
            : base(innerConverter)
        {
            Period = period;
            data = new float[frameSize];

            watch = Stopwatch.StartNew();
            var ignore = SynchonizeAsync();
        }

        async Task SynchonizeAsync()
        {
            await Task.Run(() =>
            {
                while (!cancel)
                {
                    if (watch.Elapsed.Ticks >= current * Period.Ticks)
                    {
                        lock (this)
                        {
                            base.Write(data);
                            Array.Clear(data, 0, data.Length);
                        }
                        current++;
                    }
                }
            });
        }
        public void Stop()
        {
            cancel = true;
        }

        public override void Write(float[] buffer)
        {
            lock (this)
            {
                int count = Math.Min(buffer.Length, data.Length);
                for (int i = 0; i < count; i++)
                    data[i] += buffer[i];
            }
        }

        public TimeSpan Period
        {
            get;
            private set;
        }
    }
}

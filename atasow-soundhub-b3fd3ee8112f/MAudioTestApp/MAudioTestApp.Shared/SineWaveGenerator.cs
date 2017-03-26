using System;
using System.Collections.Generic;
using System.Text;

using MAudio;
using System.IO;

namespace MAudioTestApp
{
    public sealed class SineWaveGenerator
    {
        public SineWaveGenerator(MAudioFormat format, int freq, double magnitude)
        {
            NumberOfChannels = format.NumberOfChannels;
            SampleRate = format.SampleRate;
            Frequency = freq;
            Magnitude = magnitude;
            Phase = 0;
        }

        public float[] GetSamples(int nSamples)
        {
            float[] data = new float[nSamples * NumberOfChannels];
            double phaseInc = 2 * Math.PI * Frequency / SampleRate;

            for (int i = 0; i < nSamples; i++)
            {
                double sinValue = Magnitude * Math.Sin(Phase);
                Phase = (Phase + phaseInc) % (2 * Math.PI);

                for (int j = 0; j < NumberOfChannels; j++)
                    data[i * NumberOfChannels + j] = (float)sinValue;
            }

            return data;
        }

        public double Phase { get; set; }
        public int NumberOfChannels { get; private set; }
        public int SampleRate { get; private set; }
        public int Frequency { get; set; }
        public double Magnitude { get; set; }
    }

    public sealed class SineWaveStream : Stream
    {
        SineWaveGenerator sineGen;

        public SineWaveStream(MAudioFormat format, int freq, double magnitude)
        {
            sineGen = new SineWaveGenerator(format, freq, magnitude);
        }

        #region Override methods
        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return false; } }
        public override void Flush()
        {
            throw new NotSupportedException();
        }
        public override long Length
        {
            get { throw new NotSupportedException(); }
        }
        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
        #endregion

        public override int Read(byte[] buffer, int offset, int count)
        {
            float[] data = sineGen.GetSamples(count / 4 / sineGen.NumberOfChannels);
            Buffer.BlockCopy(data, 0, buffer, offset, data.Length * 4);
            return data.Length * 4;
        }
    }
}

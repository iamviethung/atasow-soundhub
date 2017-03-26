using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using System.IO;

namespace HNet.Converters
{
    public sealed class FloatToPCMConverter : OutShortConverter, FloatConverter
    {
        public FloatToPCMConverter(ShortConverter innerConverter)
            : base(innerConverter)
        { }

        public void Write(float[] buffer)
        {
            short[] sBuffer = new short[buffer.Length];
            for (int i = 0; i < buffer.Length; i++)
                sBuffer[i] = (short)(buffer[i] * short.MaxValue + 0.5f);

            Write(sBuffer);
        }
        public override void Write(byte[] buffer)
        {
            if (buffer.Length % 4 != 0)
                throw new ArgumentException("Length must be dividable by the size of float");

            float[] fBuffer = new float[buffer.Length / 4];
            Buffer.BlockCopy(buffer, 0, fBuffer, 0, buffer.Length);
            Write(fBuffer);
        }
    }

    public sealed class PCMToFloatConverter : OutFloatConverter, ShortConverter
    {
        public PCMToFloatConverter(FloatConverter innerConverter)
            : base(innerConverter)
        { }

        public void Write(short[] buffer)
        {
            float[] fBuffer = new float[buffer.Length];
            for (int i = 0; i < buffer.Length; i++)
                fBuffer[i] = (float)buffer[i] / short.MaxValue;

            Write(fBuffer);
        }
        public override void Write(byte[] buffer)
        {
            if (buffer.Length % 2 != 0)
                throw new ArgumentException("Length must be dividable by the size of short");

            short[] sBuffer = new short[buffer.Length / 2];
            Buffer.BlockCopy(buffer, 0, sBuffer, 0, buffer.Length);
            Write(sBuffer);
        }
    }
}

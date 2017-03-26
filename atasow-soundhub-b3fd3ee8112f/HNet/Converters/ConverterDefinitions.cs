using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace HNet.Converters
{
    public interface Converter
    {
        void Write(byte[] buffer);
    }
    public interface FloatConverter : Converter
    {
        void Write(float[] buffer);
    }
    public interface ShortConverter : Converter
    {
        void Write(short[] buffer);
    }

    public abstract class OutFloatConverter : Converter
    {
        public OutFloatConverter(FloatConverter innerConverter)
        {
            InnerConverter = innerConverter;
        }

        public FloatConverter InnerConverter { get; set; }

        public virtual void Write(byte[] buffer)
        {
            if (buffer.Length % 4 != 0)
                throw new ArgumentException("Length must be dividable by the size of float");

            float[] fBuffer = new float[buffer.Length / 4];
            Buffer.BlockCopy(buffer, 0, fBuffer, 0, buffer.Length);
            Write(fBuffer);
        }
        public virtual void Write(float[] buffer)
        {
            if (InnerConverter != null)
                InnerConverter.Write(buffer);
        }
    }
    public abstract class OutShortConverter : Converter
    {
        public OutShortConverter(ShortConverter innerConverter)
        {
            InnerConverter = innerConverter;
        }

        public ShortConverter InnerConverter { get; set; }

        public virtual void Write(byte[] buffer)
        {
            if (buffer.Length % 2 != 0)
                throw new ArgumentException("Length must be dividable by the size of short");

            short[] sBuffer = new short[buffer.Length / 2];
            Buffer.BlockCopy(buffer, 0, sBuffer, 0, buffer.Length);
            Write(sBuffer);
        }
        public virtual void Write(short[] buffer)
        {
            if (InnerConverter != null)
                InnerConverter.Write(buffer);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace HNet.Converters
{
    public sealed class FloatSimplifier : OutFloatConverter, FloatConverter
    {
        int framePassed = 0;

        public FloatSimplifier(FloatConverter innerConverter, int ratio, int frameSize)
            : base(innerConverter)
        {
            Ratio = ratio;
            FrameSize = frameSize;
        }

        public override void Write(float[] buffer)
        {
            if (buffer.Length % FrameSize != 0)
                throw new ArgumentException("Count must be dividable by the frame size");

            float[] sBuffer = new float[(buffer.Length / FrameSize + (framePassed > 0 ? framePassed - 1 : Ratio - 1)) / Ratio * FrameSize];

            int current = (framePassed > 0 ? Ratio - framePassed : 0) * FrameSize;
            for (int i = 0; i < sBuffer.Length; i+=FrameSize)
            {
                if (FrameSize > 1) Buffer.BlockCopy(buffer, current, sBuffer, i, FrameSize);
                else sBuffer[i] = buffer[current];
                current += Ratio * FrameSize;
            }

            framePassed = (buffer.Length - current) / FrameSize + Ratio;

            base.Write(sBuffer);
        }

        #region Properties
        public int Ratio
        {
            get;
            private set;
        }
        public int FrameSize
        {
            get;
            private set;
        }
        #endregion
    }

    public sealed class ShortSimplifier : OutShortConverter, ShortConverter
    {
        int framePassed = 0;

        public ShortSimplifier(ShortConverter innerConverter, int ratio, int frameSize)
            : base(innerConverter)
        {
            Ratio = ratio;
            FrameSize = frameSize;
        }

        public override void Write(short[] buffer)
        {
            if (buffer.Length % FrameSize != 0)
                throw new ArgumentException("Count must be dividable by the frame size");

            short[] sBuffer = new short[(buffer.Length / FrameSize + (framePassed > 0 ? framePassed - 1 : Ratio - 1)) / Ratio * FrameSize];

            int current = (framePassed > 0 ? Ratio - framePassed : 0) * FrameSize;
            for (int i = 0; i < sBuffer.Length; i += FrameSize)
            {
                if (FrameSize > 1) Buffer.BlockCopy(buffer, current, sBuffer, i, FrameSize);
                else sBuffer[i] = buffer[current];
                current += Ratio * FrameSize;
            }

            framePassed = (buffer.Length - current) / FrameSize + Ratio;

            base.Write(sBuffer);
        }

        #region Properties
        public int Ratio
        {
            get;
            private set;
        }
        public int FrameSize
        {
            get;
            private set;
        }
        #endregion
    }
}

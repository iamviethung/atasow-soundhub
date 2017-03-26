using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using System.IO;

namespace HNet.Converters
{
    public sealed class FirstOrderFiller : OutFloatConverter, FloatConverter
    {
        float lastData;

        public FirstOrderFiller(FloatConverter innerConverter, int ratio)
            : base(innerConverter)
        {
            Ratio = ratio;
            lastData = 0;
        }

        public override void Write(float[] buffer)
        {
            float[] fBuffer = new float[buffer.Length * Ratio];
            for (int i = 0; i < buffer.Length; i++)
            {
                for (int j = 0; j < Ratio - 1; j++)
                    fBuffer[i * Ratio + j] = lastData + (buffer[i] - lastData) * (j + 1) / Ratio;
                fBuffer[(i + 1) * Ratio - 1] = lastData = buffer[i];
            }

            base.Write(fBuffer);
        }

        #region Properties
        public int Ratio
        {
            get;
            private set;
        }
        #endregion
    }
}

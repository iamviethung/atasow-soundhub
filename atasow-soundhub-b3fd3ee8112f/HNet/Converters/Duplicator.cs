using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace HNet.Converters
{
    public sealed class Duplicator : OutFloatConverter, FloatConverter
    {
        public Duplicator(FloatConverter innerConverter)
            : base(innerConverter)
        { }

        public override void Write(float[] buffer)
        {
            float[] dBuffer = new float[buffer.Length * 2];

            for (int i = 0; i < buffer.Length; i++)
                dBuffer[i * 2] = dBuffer[i * 2 + 1] = buffer[i];

            base.Write(dBuffer);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HNet.Converters
{
    public class ScalingConverter : OutFloatConverter, FloatConverter
    {
        public ScalingConverter(FloatConverter innerConverter, float ratio)
            : base(innerConverter)
        {
            Ratio = ratio;
        }

        public override void Write(float[] buffer)
        {
            float[] sBuffer = new float[buffer.Length];
            
            for (int i = 0; i < buffer.Length; i++)
                sBuffer[i] = buffer[i] * Ratio;

            base.Write(sBuffer);
        }

        public float Ratio
        {
            get;
            set;
        }
    }
}

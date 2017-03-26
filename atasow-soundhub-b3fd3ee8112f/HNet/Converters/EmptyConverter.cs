using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HNet.Converters
{
    public class EmptyConverter : FloatConverter, ShortConverter
    {
        public void Write(float[] buffer) { }
        public void Write(byte[] buffer) { }
        public void Write(short[] buffer) { }
    }
}

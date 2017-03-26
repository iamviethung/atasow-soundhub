using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HNet.Converters
{
    public class BranchingConverter : OutFloatConverter, FloatConverter
    {
        public BranchingConverter(FloatConverter mainConverter)
            : base(mainConverter)
        {
            Branches = new List<FloatConverter>();
        }

        public override void Write(float[] buffer)
        {
            foreach (FloatConverter branch in Branches)
                branch.Write(buffer);
            base.Write(buffer);
        }

        public List<FloatConverter> Branches { get; private set; }
    }
}

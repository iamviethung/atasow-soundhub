using System;
using System.Collections.Generic;
using System.Text;

using MWASAPI;
using System.Threading.Tasks;

namespace MAudio
{
    public interface IMAudioCapturer
    {
        Task InitializeAsync();
        Task InitializeAsync(MAudioFormat format);

        void Start();
        void Stop();
        void Reset();

        MCaptureStream Stream { get; }
        MAudioFormat Format { get; }
    }
}

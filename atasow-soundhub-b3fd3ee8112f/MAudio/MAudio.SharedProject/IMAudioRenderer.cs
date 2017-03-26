using System;
using System.Collections.Generic;
using System.Text;

using System.Threading.Tasks;
using Windows.Media;

namespace MAudio
{
    public interface IMAudioRenderer
    {
        Task InitializeAsync();
        Task InitializeAsync(MAudioFormat format);

        void Start();
        void Stop();
        void Reset();

        MRenderStream Stream { get; }
        MAudioFormat Format { get; }
        SystemMediaTransportControls SystemMediaTransportControls { get; }
        bool BackgroundCapable { get; }
    }
}

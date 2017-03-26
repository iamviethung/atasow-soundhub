using System;
using System.Collections.Generic;
using System.Text;

using M = MWASAPI;

namespace MAudio
{
    public struct MAudioFormat
    {
        public MAudioEncodingType EncodingType;
        public int NumberOfChannels;
        public int SampleRate;

        public MAudioFormat(MAudioEncodingType encodingType, int numberOfChannels, int sampleRate)
        {
            EncodingType = encodingType;
            NumberOfChannels = numberOfChannels;
            SampleRate = sampleRate;
        }

        public int FrameSize
        {
            get
            {
                switch (EncodingType)
                {
                    case MAudioEncodingType.PCM8bits:
                        return NumberOfChannels;
                    case MAudioEncodingType.PCM16bits:
                        return 2 * NumberOfChannels;
                    case MAudioEncodingType.Float:
                        return 4 * NumberOfChannels;
                    default:
                        return 0;
                }
            }
        }

        internal M.MWaveFormat MWASAPIFormat
        {
            get
            {
                return new M.MWaveFormat((M.MFormatType)EncodingType, SampleRate, NumberOfChannels);
            }
        }
        internal MAudioFormat(M.MWaveFormat format) {
            EncodingType = (MAudioEncodingType)format.FormatType;
            NumberOfChannels = format.NumberOfChannels;
            SampleRate = format.SampleRate;
        }
    }
    public enum MAudioEncodingType
    {
        Unknown,
        Float,
        PCM16bits,
        PCM8bits
    }
}

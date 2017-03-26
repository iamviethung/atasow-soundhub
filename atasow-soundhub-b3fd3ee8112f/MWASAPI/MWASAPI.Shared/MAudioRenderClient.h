#pragma once

#include <Audioclient.h>
#include <mmdeviceapi.h>
#include <MWaveFormat.h>

namespace MWASAPI
{
	public enum class MAudioClientSilentFlag {
		None,
		Silent
	};

	public ref class MAudioRenderClient sealed
	{
	public:
		int LoadBuffer(int nFrameRequest, const Platform::Array<byte>^ Data, int Offset, MAudioClientSilentFlag SilentFlag);

		property MWaveFormat^ MixFormat {
			MWaveFormat^ get() { return m_format; }
		}

	internal:
		MAudioRenderClient(IAudioRenderClient* pRenderClient, MWaveFormat^ pformat);

	private:
		~MAudioRenderClient();

		MWaveFormat^				m_format;
		IAudioRenderClient			*m_RenderClient;
	};
}
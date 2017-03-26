#pragma once

#include <Audioclient.h>
#include <mmdeviceapi.h>
#include <MWaveFormat.h>

namespace MWASAPI
{
	public enum class MAudioCaptureDiscontinuity {
		None,
		Discontinued
	};
	public ref class MAudioCaptureInformation sealed {
	public:
		MAudioCaptureInformation() {}

		property int FrameAvailable {
	public:int get() { return nFrameAvailable; }
	internal: void set(int value) { nFrameAvailable = value; }
		}
		property MAudioCaptureDiscontinuity Discontinuity {
	public:MAudioCaptureDiscontinuity get() { return discontinuity; }
	internal: void set(MAudioCaptureDiscontinuity value) { discontinuity = value; }
		}
		property long long Position {
	public:long long get() { return devicePosition; }
	internal: void set(long long value) { devicePosition = value; }
		}

	private:
		int nFrameAvailable;
		MAudioCaptureDiscontinuity discontinuity;
		long long devicePosition;
	};

	public ref class MAudioCaptureClient sealed
	{
	public:
		Platform::Array<byte>^ LoadBuffer(MAudioCaptureInformation^ outInformation);

		property MWaveFormat^ MixFormat {
			MWaveFormat^ get() { return m_format; }
		}
		property int NextPacketSize {
			int get();
		}

	internal:
		MAudioCaptureClient(IAudioCaptureClient* pCaptureClient, MWaveFormat^ pformat);

	private:
		~MAudioCaptureClient();

		MWaveFormat^				m_format;
		IAudioCaptureClient			*m_CaptureClient;
	};
}
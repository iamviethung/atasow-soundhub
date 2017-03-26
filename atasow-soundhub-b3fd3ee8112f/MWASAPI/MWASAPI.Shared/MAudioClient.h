#pragma once

#include <Audioclient.h>
#include <mmdeviceapi.h>
#include <wrl\implements.h>
#include "MClientState.h"
#include "MWaveFormat.h"
#include "MAudioClientProperties.h"
#include "MAudioSessionTypes.h"
#include "MAudioRenderClient.h"
#include "MAudioCaptureClient.h"

using namespace Windows::Foundation;
using namespace Microsoft::WRL;

namespace MWASAPI
{
	ref class MAudioClient;

	public enum class MAudioDeviceType {
		Render,
		Capture
	};

	class MAudioDevice :
		public RuntimeClass<RuntimeClassFlags<ClassicCom>, FtmBase, IActivateAudioInterfaceCompletionHandler>
	{
	public:
		MAudioDevice();

		HRESULT ActivateAudioInterface(MAudioClient^ pOwner, IAudioClient2** pAudioClient, MAudioDeviceType deviceType);
		STDMETHOD(ActivateCompleted)(IActivateAudioInterfaceAsyncOperation *operation);

	private:
		IAudioClient2			**m_AudioClient;
		MAudioClient			^m_Owner;
	};

	public value struct MAudioBufferSizeLimits {
		Windows::Foundation::TimeSpan Minimum;
		Windows::Foundation::TimeSpan Maximum;
	};

	//
	// MAudioClient
	//
	public ref class MAudioClient sealed
    {
    public:
		MAudioClient();

		// Initializing functions
		void ActivateAsync(MAudioDeviceType deviceType);
		void Initialize(
			MAudioClientShareMode ShareMode,
			MAudioClientStreamFlags StreamFlags,
			TimeSpan BufferDuration,
			TimeSpan Periodicity,
			MWaveFormat^ Format);

		// Controlling functions
		void Start();
		void Stop();
		void Reset();

		// Get/Set functions
		MAudioBufferSizeLimits GetBufferSizeLimits(MWaveFormat^ pFormat, bool bEventDriven);
		int GetBufferSizeInBytes(int nFrame);
		TimeSpan GetDuration(int nFrame);		
		void SetClientProperties(MAudioClientProperties props);
		MWaveFormat^ IsFormatSupported(MAudioClientShareMode ShareMode, MWaveFormat^ pFormat);

		// Get services functions
		MAudioRenderClient^ GetRenderClient();
		MAudioCaptureClient^ GetCaptureClient();

		// Events
		event Windows::Foundation::EventHandler<Platform::Object^>^			Activated;
		event Windows::Foundation::EventHandler<Platform::Object^>^			Initialized;
		event Windows::Foundation::EventHandler<Platform::Object^>^			BufferReady;

		// Properties
		property MAudioClientState State {
			MAudioClientState get() { return state; }
		}
		property int BufferSize {
			int get();
		}
		property int CurrentPadding {
			int get();
		}
		property MWaveFormat^ MixFormat {
			MWaveFormat^ get();
		}

	internal:
		void OnActivated();
		void OnError(HRESULT hr);

	private:
		~MAudioClient();

		IAudioClient2			*m_AudioClient;
		ComPtr<MAudioDevice>	m_dev;
		MAudioClientState		state;
		HANDLE					eventHandle;
		IAsyncAction^			eventThrow;
    };
}
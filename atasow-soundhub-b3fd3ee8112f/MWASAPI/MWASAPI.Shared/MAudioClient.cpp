#include "pch.h"
#include "MAudioClient.h"
#include "MAudioClientException.h"

using namespace MWASAPI;
using namespace Platform;
using namespace Windows::Foundation;
using namespace Windows::Media::Devices;
using namespace Concurrency;

//
// MAudioDevice
//
MAudioDevice::MAudioDevice() :
m_Owner(nullptr), m_AudioClient(nullptr) {}

HRESULT MAudioDevice::ActivateAudioInterface(MAudioClient^ pOwner, IAudioClient2** pAudioClient, MAudioDeviceType deviceType) {
	IActivateAudioInterfaceAsyncOperation *asyncOp;
	HRESULT hr = S_OK;
	m_Owner = pOwner;
	m_AudioClient = pAudioClient;

	String^ m_DeviceIdString;
	if (deviceType == MAudioDeviceType::Render)
		m_DeviceIdString = MediaDevice::GetDefaultAudioRenderId(AudioDeviceRole::Default);
	if (deviceType == MAudioDeviceType::Capture)
		m_DeviceIdString = MediaDevice::GetDefaultAudioCaptureId(AudioDeviceRole::Default);

	hr = ActivateAudioInterfaceAsync(m_DeviceIdString->Data(), __uuidof(IAudioClient2), nullptr, this, &asyncOp);
	
	SAFE_RELEASE(asyncOp);
	
	if (FAILED(hr))
		m_Owner->OnError(hr);
	return hr;
}

HRESULT MAudioDevice::ActivateCompleted(IActivateAudioInterfaceAsyncOperation *operation)
{
	HRESULT hr = S_OK;
	HRESULT hrActivateResult = S_OK;
	IUnknown *punkAudioInterface = nullptr;

	if (m_Owner->State != MAudioClientState::Uninitialized) {
		hr = E_NOT_VALID_STATE;
		goto exit;
	}

	hr = operation->GetActivateResult(&hrActivateResult, &punkAudioInterface);
	if (SUCCEEDED(hr) && SUCCEEDED(hrActivateResult))
	{
		punkAudioInterface->QueryInterface(IID_PPV_ARGS(m_AudioClient));
		if (*m_AudioClient == nullptr) {
			hr = E_FAIL;
			goto exit;
		}

		m_Owner->OnActivated();
	}

exit:
	if (FAILED(hr))
		m_Owner->OnError(hr);

	return S_OK;
}

//
// MAudioClient
//
MAudioClient::MAudioClient() :
m_AudioClient(nullptr), m_dev(nullptr),
state(MAudioClientState::Uninitialized), eventThrow(nullptr) {
	eventHandle = CreateEventEx(nullptr, nullptr, 0, EVENT_ALL_ACCESS);
}

void MAudioClient::ActivateAsync(MAudioDeviceType deviceType) {
	m_dev = Make<MAudioDevice>();
	m_dev->ActivateAudioInterface(this, &m_AudioClient, deviceType);
}

MAudioClient::~MAudioClient()
{
	if (eventThrow)
		eventThrow->Cancel();
	SAFE_RELEASE(m_AudioClient);
}

void MAudioClient::OnActivated() {
	state = MAudioClientState::Activated;
	Activated(this, nullptr);
}

void MAudioClient::OnError(HRESULT hr) {
	state = MAudioClientState::InError;
	throw Exception::CreateException(hr, L"Error on AudioClient activating process");
}

MWaveFormat^ MAudioClient::MixFormat::get() {
	WAVEFORMATEX *pDeviceFormat;
	HRESULT hr = m_AudioClient->GetMixFormat(&pDeviceFormat);
	MAudioClientException::Throw(hr);
	return ref new MWaveFormat(pDeviceFormat);
}

MAudioBufferSizeLimits MAudioClient::GetBufferSizeLimits(MWaveFormat^ pFormat, bool bEventDriven) {
	MAudioBufferSizeLimits limits;
	REFERENCE_TIME min, max;

	HRESULT hr = m_AudioClient->GetBufferSizeLimits(pFormat->Data, bEventDriven, &min, &max);
	MAudioClientException::Throw(hr);

	limits.Minimum.Duration = min;
	limits.Maximum.Duration = max;
	return limits;
}

void MAudioClient::SetClientProperties(MAudioClientProperties props) {
	AudioClientProperties uprops;
	uprops.cbSize = sizeof(AudioClientProperties);
	uprops.bIsOffload = props.IsOffload;
	uprops.eCategory = (AUDIO_STREAM_CATEGORY)props.Category;
	uprops.Options = (AUDCLNT_STREAMOPTIONS)props.Options;

	HRESULT hr = m_AudioClient->SetClientProperties(&uprops);
	MAudioClientException::Throw(hr);
}

void MAudioClient::Initialize(MAudioClientShareMode ShareMode, MAudioClientStreamFlags StreamFlags,
	TimeSpan BufferDuration, TimeSpan Periodicity, MWaveFormat^ Format) {

	DWORD streamflags = 0;
	if (StreamFlags.StreamCrossProcess) streamflags |= AUDCLNT_STREAMFLAGS_CROSSPROCESS;
	if (StreamFlags.StreamLoopBack) streamflags |= AUDCLNT_STREAMFLAGS_LOOPBACK;
	if (StreamFlags.StreamEventCallback) streamflags |= AUDCLNT_STREAMFLAGS_EVENTCALLBACK;
	if (StreamFlags.StreamNoPersist) streamflags |= AUDCLNT_STREAMFLAGS_NOPERSIST;
	if (StreamFlags.StreamRateAdjust) streamflags |= AUDCLNT_STREAMFLAGS_RATEADJUST;
	if (StreamFlags.SessionExpireWhenUnowned) streamflags |= AUDCLNT_SESSIONFLAGS_EXPIREWHENUNOWNED;
	if (StreamFlags.SessionDisplayHide) streamflags |= AUDCLNT_SESSIONFLAGS_DISPLAY_HIDE;
	if (StreamFlags.SessionDisplayHideWhenExpired) streamflags |= AUDCLNT_SESSIONFLAGS_DISPLAY_HIDEWHENEXPIRED;

	HRESULT hr = m_AudioClient->Initialize((_AUDCLNT_SHAREMODE)ShareMode, streamflags,
		BufferDuration.Duration, Periodicity.Duration, Format->Data, nullptr);
	MAudioClientException::Throw(hr);

	if (StreamFlags.StreamEventCallback) {
		hr = m_AudioClient->SetEventHandle(eventHandle);
		MAudioClientException::Throw(hr);

		eventThrow = create_async([this]() {
			while (true) {
				WaitForSingleObjectEx(eventHandle, INFINITE, TRUE);
				BufferReady(this, nullptr);
			}
		});
	}

	state = MAudioClientState::Initialized;
	Initialized(this, nullptr);
}

int MAudioClient::BufferSize::get() {
	UINT32 tmp;

	HRESULT hr = m_AudioClient->GetBufferSize(&tmp);
	MAudioClientException::Throw(hr);

	return (int)tmp;
}

int MAudioClient::CurrentPadding::get() {
	UINT32 tmp;

	HRESULT hr = m_AudioClient->GetCurrentPadding(&tmp);
	MAudioClientException::Throw(hr);

	return (int)tmp;
}

void MAudioClient::Start() {
	HRESULT hr = m_AudioClient->Start();
	MAudioClientException::Throw(hr);
	state = MAudioClientState::Started;
}

void MAudioClient::Stop() {
	HRESULT hr = m_AudioClient->Stop();
	MAudioClientException::Throw(hr);
	state = MAudioClientState::Stopped;
}

void MAudioClient::Reset() {
	HRESULT hr = m_AudioClient->Reset();
	MAudioClientException::Throw(hr);
	state = MAudioClientState::JustReset;
}

MAudioRenderClient^ MAudioClient::GetRenderClient() {
	IAudioRenderClient* pRenderClient;

	HRESULT hr = m_AudioClient->GetService(__uuidof(IAudioRenderClient), (void**)&pRenderClient);
	MAudioClientException::Throw(hr);

	return ref new MAudioRenderClient(pRenderClient, MixFormat);
}

MAudioCaptureClient^ MAudioClient::GetCaptureClient() {
	IAudioCaptureClient* pCaptureClient;

	HRESULT hr = m_AudioClient->GetService(__uuidof(IAudioCaptureClient), (void**)&pCaptureClient);
	MAudioClientException::Throw(hr);

	return ref new MAudioCaptureClient(pCaptureClient, MixFormat);
}

int MAudioClient::GetBufferSizeInBytes(int nFrame) {
	return nFrame * MixFormat->FrameSize;
}

TimeSpan MAudioClient::GetDuration(int nFrame) {
	TimeSpan duration;
	duration.Duration = (long long)10000000 * nFrame / MixFormat->SampleRate;
	return duration;
}

MWaveFormat^ MAudioClient::IsFormatSupported(MAudioClientShareMode ShareMode, MWaveFormat^ pFormat) {
	WAVEFORMATEX* outFormat;
	HRESULT hr = m_AudioClient->IsFormatSupported((_AUDCLNT_SHAREMODE)ShareMode, pFormat->Data, &outFormat);
	MAudioClientException::Throw(hr);

	if (outFormat != nullptr)
		return ref new MWaveFormat(outFormat);
	else return pFormat;
}
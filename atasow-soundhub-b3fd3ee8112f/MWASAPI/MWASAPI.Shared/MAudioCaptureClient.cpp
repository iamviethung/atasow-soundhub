#include "pch.h"
#include "MAudioCaptureClient.h"
#include "MAudioClientException.h"

using namespace MWASAPI;
using namespace Platform;

MAudioCaptureClient::MAudioCaptureClient(IAudioCaptureClient* pCaptureClient, MWaveFormat^ pFormat) :
m_CaptureClient(pCaptureClient), m_format(pFormat) {}

MAudioCaptureClient::~MAudioCaptureClient() {
	SAFE_RELEASE(m_CaptureClient);
}

Array<byte>^ MAudioCaptureClient::LoadBuffer(MAudioCaptureInformation^ outInformation) {
	if (NextPacketSize > 0) {
		HRESULT hr;
		byte* pData;

		UINT32 nFrameReceived;
		DWORD flags;
		UINT64 devPosition;

		hr = m_CaptureClient->GetBuffer(&pData, &nFrameReceived, &flags, &devPosition, NULL);
		MAudioClientException::Throw(hr);

		Array<byte>^ Data = ref new Array<byte>(pData, nFrameReceived * m_format->FrameSize);

		hr = m_CaptureClient->ReleaseBuffer(nFrameReceived);
		MAudioClientException::Throw(hr);

		if (outInformation != nullptr) {
			outInformation->FrameAvailable = (int)nFrameReceived;
			outInformation->Discontinuity = (MAudioCaptureDiscontinuity)(flags & AUDCLNT_BUFFERFLAGS_DATA_DISCONTINUITY);
			outInformation->Position = (long long)devPosition;
		}
		else throw ref new InvalidArgumentException("The parameter <outInformation> in MAudioCaptureClient.LoadBuffer was not initialized");

		return Data;
	}
	else
		return nullptr;
}

int MAudioCaptureClient::NextPacketSize::get() {
	UINT32 tmp;

	HRESULT hr = m_CaptureClient->GetNextPacketSize(&tmp);
	MAudioClientException::Throw(hr);

	return (int)tmp;
}
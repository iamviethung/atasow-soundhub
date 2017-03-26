#include "pch.h"
#include "MAudioRenderClient.h"
#include "MAudioClientException.h"

using namespace MWASAPI;
using namespace Platform;

MAudioRenderClient::MAudioRenderClient(IAudioRenderClient* pRenderClient, MWaveFormat^ pFormat) :
m_RenderClient(pRenderClient), m_format(pFormat) {}

MAudioRenderClient::~MAudioRenderClient() {
	SAFE_RELEASE(m_RenderClient);
}

int MAudioRenderClient::LoadBuffer(
	int nFrameRequest, const Array<byte>^ Data, int Offset, MAudioClientSilentFlag SilentFlag) {
	HRESULT hr;
	byte* pData;

	int frameSize = m_format->FrameSize;
	int availableFrame = (Data->Length - Offset) / frameSize;
	int nWrittenFrame = nFrameRequest < availableFrame ? nFrameRequest : availableFrame;

	hr = m_RenderClient->GetBuffer(nFrameRequest, &pData);
	MAudioClientException::Throw(hr);

	memcpy(pData, Data->begin() + Offset, nWrittenFrame*frameSize);

	hr = m_RenderClient->ReleaseBuffer(nWrittenFrame,
		SilentFlag == MAudioClientSilentFlag::Silent ? AUDCLNT_BUFFERFLAGS_SILENT : 0);
	MAudioClientException::Throw(hr);

	return nWrittenFrame;
}
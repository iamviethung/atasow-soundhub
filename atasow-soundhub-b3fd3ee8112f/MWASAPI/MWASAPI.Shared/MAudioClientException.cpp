#include "pch.h"
#include "MAudioClientException.h"

using namespace MWASAPI;
using namespace Platform::Collections;

Map<MAudioClientErrorType, Platform::String^>^ MAudioClientException::errormesg = MAudioClientException::ErrorMsgInitialize();
Map<MAudioClientErrorType, Platform::String^>^ MAudioClientException::ErrorMsgInitialize() {
	auto tmp = ref new Map < MAudioClientErrorType, Platform::String^ >;

	tmp->Insert(MAudioClientErrorType::NotInitialized, "AudioClient Error: Not Initialized");
	tmp->Insert(MAudioClientErrorType::AlreadyInitialized, "AudioClient Error: Already Initialized");
	tmp->Insert(MAudioClientErrorType::WrongEndpointType, "AudioClient Error: Wrong Endpoint Type");
	tmp->Insert(MAudioClientErrorType::DeviceInvalidated, "AudioClient Error: Device Invalidated");
	tmp->Insert(MAudioClientErrorType::NotStopped, "AudioClient Error: Not Stopped");
	tmp->Insert(MAudioClientErrorType::BufferTooLarge, "AudioClient Error: Buffer Too Large");
	tmp->Insert(MAudioClientErrorType::OutOfOrder, "AudioClient Error: Out Of Order");
	tmp->Insert(MAudioClientErrorType::UnsupportedFormat, "AudioClient Error: Unsupported Format");
	tmp->Insert(MAudioClientErrorType::InvalidSize, "AudioClient Error: Invalid Size");
	tmp->Insert(MAudioClientErrorType::DeviceInUse, "AudioClient Error: Device In Use");
	tmp->Insert(MAudioClientErrorType::BufferOperationPending, "AudioClient Error: Buffer Operation Pending");
	tmp->Insert(MAudioClientErrorType::ThreadNotRegistered, "AudioClient Error: Thread Not Registered");
	tmp->Insert(MAudioClientErrorType::ExclusiveModeNotAllowed, "AudioClient Error: Exclusive Mode Not Allowed");
	tmp->Insert(MAudioClientErrorType::EndpointCreateFailed, "AudioClient Error: Endpoint Create Failed");
	tmp->Insert(MAudioClientErrorType::ServiceNotRunning, "AudioClient Error: Service Not Running");
	tmp->Insert(MAudioClientErrorType::EventHandleNotExpected, "AudioClient Error: Event Handle Not Expected");
	tmp->Insert(MAudioClientErrorType::ExclusiveModeOnly, "AudioClient Error: Exclusive Mode Only");
	tmp->Insert(MAudioClientErrorType::BufferDurationAndPeriodNotEqual, "AudioClient Error: Buffer Duration And Period Not Equal");
	tmp->Insert(MAudioClientErrorType::EventHandleNotSet, "AudioClient Error: Event Handle Not Set");
	tmp->Insert(MAudioClientErrorType::IncorrectBufferSize, "AudioClient Error: Incorrect Buffer Size");
	tmp->Insert(MAudioClientErrorType::BufferSizeError, "AudioClient Error: Buffer Size Error");
	tmp->Insert(MAudioClientErrorType::CPUUsageExceeded, "AudioClient Error: CPU Usage Exceeded");
	tmp->Insert(MAudioClientErrorType::BufferError, "AudioClient Error: Buffer Error");
	tmp->Insert(MAudioClientErrorType::BufferSizeNotAligned, "AudioClient Error: Buffer Size Not Aligned");
	tmp->Insert(MAudioClientErrorType::InvalidDevicePeriod, "AudioClient Error: Invalid Device Period");
	tmp->Insert(MAudioClientErrorType::InvalidStreamFlag, "AudioClient Error: Invalid Stream Flag");
	tmp->Insert(MAudioClientErrorType::EndpointOffloadNotCapable, "AudioClient Error: Endpoint Offload Not Capable");
	tmp->Insert(MAudioClientErrorType::OutOfOffloadResource, "AudioClient Error: Out Of Offload Resource");
	tmp->Insert(MAudioClientErrorType::OffloadModeOnly, "AudioClient Error: Offload Mode Only");
	tmp->Insert(MAudioClientErrorType::NonOffloadModeOnly, "AudioClient Error: Non-Offload Mode Only");
	tmp->Insert(MAudioClientErrorType::ResourcesInvalidated, "AudioClient Error: Resources Invalidated");
	tmp->Insert(MAudioClientErrorType::RawModeUnsupported, "AudioClient Error: Raw Mode Unsupported");

	return tmp;
};

MAudioClientException::MAudioClientException(MAudioClientErrorType type) :
m_type(type)
{ }

bool MAudioClientException::IsAudioClientException(HRESULT hr) {
	return ((hr & AUDCLNT_ERR(0)) == AUDCLNT_ERR(0));
}

void MAudioClientException::Throw(HRESULT hr) {
	if (FAILED(hr)) {
		if (IsAudioClientException(hr))
			throw;
		else
			throw;
	}
}
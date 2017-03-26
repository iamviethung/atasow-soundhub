#pragma once

#include "pch.h"
#include <Audioclient.h>

namespace MWASAPI {
	public enum class MAudioClientErrorType {
		None = 0,
		NotInitialized = AUDCLNT_E_NOT_INITIALIZED,
		AlreadyInitialized = AUDCLNT_E_ALREADY_INITIALIZED,
		WrongEndpointType = AUDCLNT_E_WRONG_ENDPOINT_TYPE,
		DeviceInvalidated = AUDCLNT_E_DEVICE_INVALIDATED,
		NotStopped = AUDCLNT_E_NOT_STOPPED,
		BufferTooLarge = AUDCLNT_E_BUFFER_TOO_LARGE,
		OutOfOrder = AUDCLNT_E_OUT_OF_ORDER,
		UnsupportedFormat = AUDCLNT_E_UNSUPPORTED_FORMAT,
		InvalidSize = AUDCLNT_E_INVALID_SIZE,
		DeviceInUse = AUDCLNT_E_DEVICE_IN_USE,
		BufferOperationPending = AUDCLNT_E_BUFFER_OPERATION_PENDING,
		ThreadNotRegistered = AUDCLNT_E_THREAD_NOT_REGISTERED,
		ExclusiveModeNotAllowed = AUDCLNT_E_EXCLUSIVE_MODE_NOT_ALLOWED,
		EndpointCreateFailed = AUDCLNT_E_ENDPOINT_CREATE_FAILED,
		ServiceNotRunning = AUDCLNT_E_SERVICE_NOT_RUNNING,
		EventHandleNotExpected = AUDCLNT_E_EVENTHANDLE_NOT_EXPECTED,
		ExclusiveModeOnly = AUDCLNT_E_EXCLUSIVE_MODE_ONLY,
		BufferDurationAndPeriodNotEqual = AUDCLNT_E_BUFDURATION_PERIOD_NOT_EQUAL,
		EventHandleNotSet = AUDCLNT_E_EVENTHANDLE_NOT_SET,
		IncorrectBufferSize = AUDCLNT_E_INCORRECT_BUFFER_SIZE,
		BufferSizeError = AUDCLNT_E_BUFFER_SIZE_ERROR,
		CPUUsageExceeded = AUDCLNT_E_CPUUSAGE_EXCEEDED,
		BufferError = AUDCLNT_E_BUFFER_ERROR,
		BufferSizeNotAligned = AUDCLNT_E_BUFFER_SIZE_NOT_ALIGNED,
		InvalidDevicePeriod = AUDCLNT_E_INVALID_DEVICE_PERIOD,
		InvalidStreamFlag = AUDCLNT_E_INVALID_STREAM_FLAG,
		EndpointOffloadNotCapable = AUDCLNT_E_ENDPOINT_OFFLOAD_NOT_CAPABLE,
		OutOfOffloadResource = AUDCLNT_E_OUT_OF_OFFLOAD_RESOURCES,
		OffloadModeOnly = AUDCLNT_E_OFFLOAD_MODE_ONLY,
		NonOffloadModeOnly = AUDCLNT_E_NONOFFLOAD_MODE_ONLY,
		ResourcesInvalidated = AUDCLNT_E_RESOURCES_INVALIDATED,
		RawModeUnsupported = AUDCLNT_E_RAW_MODE_UNSUPPORTED
	};

	public ref class MAudioClientException sealed
	{
	public:
		MAudioClientException(MAudioClientErrorType type);

		property MAudioClientErrorType Detail {
			MAudioClientErrorType get() { return m_type; }
		}

	internal:
		static bool IsAudioClientException(HRESULT hr);
		static void Throw(HRESULT hr);

	private:
		MAudioClientErrorType		m_type;
		static Platform::Collections::Map<MAudioClientErrorType, Platform::String^>^	errormesg;

		static Platform::Collections::Map<MAudioClientErrorType, Platform::String^>^	ErrorMsgInitialize();
	};
}
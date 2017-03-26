#pragma once

namespace MWASAPI {
	public enum class MAudioClientState
	{
		Uninitialized,
		InError,
		Activated,
		Initialized,
		Started,
		Stopped,
		JustReset
	};
}
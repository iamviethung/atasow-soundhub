#pragma once

namespace MWASAPI {
	public enum class MAudioClientShareMode { Shared, Exclusive };

	public value struct MAudioClientStreamFlags
	{
	public:
		bool StreamCrossProcess,
			StreamLoopBack,
			StreamEventCallback,
			StreamNoPersist,
			StreamRateAdjust,
			SessionExpireWhenUnowned,
			SessionDisplayHide,
			SessionDisplayHideWhenExpired;
	};
}
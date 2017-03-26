#pragma once

#include <Audioclient.h>

namespace MWASAPI {
	public enum class MAudioStreamCategory {
		Other = 0,
		ForegroundOnlyMedia,
		BackgroundCapableMedia,
		Communications,
		Alerts,
		SoundEffects,
		GameEffects,
		GameMedia
	};

	public enum class MAudioClientStreamOption {
		None = 0,
		Raw = 1
	};

	public value struct MAudioClientProperties {
		bool IsOffload;
		MAudioStreamCategory Category;
		MAudioClientStreamOption Options;
	};
}
#pragma once

#include <Audioclient.h>

namespace MWASAPI {

	public enum class MFormatType {
		Unknown,
		Float,
		PCM16Bit,
		PCM8Bit
	};

	public ref class MWaveFormat sealed
	{
	public:
		MWaveFormat(MFormatType formatType, int sampleRate, int nChannels) {
			m_WaveFormat = (WAVEFORMATEX*)CoTaskMemAlloc(sizeof(WAVEFORMATEX));
			NumberOfChannels = nChannels;
			FormatType = formatType;
			SampleRate = sampleRate;
			m_WaveFormat->cbSize = 0;
		}

		property MFormatType FormatType {
			MFormatType get() {
				if ((m_WaveFormat->wFormatTag == WAVE_FORMAT_PCM) || ((m_WaveFormat->wFormatTag == WAVE_FORMAT_EXTENSIBLE) &&
					(reinterpret_cast<WAVEFORMATEXTENSIBLE*>(m_WaveFormat)->SubFormat == KSDATAFORMAT_SUBTYPE_PCM))) {
					if (m_WaveFormat->wBitsPerSample == 16)
						return MFormatType::PCM16Bit;
					if (m_WaveFormat->wBitsPerSample == 8)
						return MFormatType::PCM8Bit;
				}
				else if ((m_WaveFormat->wFormatTag == WAVE_FORMAT_IEEE_FLOAT) || ((m_WaveFormat->wFormatTag == WAVE_FORMAT_EXTENSIBLE) &&
					(reinterpret_cast<WAVEFORMATEXTENSIBLE*>(m_WaveFormat)->SubFormat == KSDATAFORMAT_SUBTYPE_IEEE_FLOAT)))
					return MFormatType::Float;

				return MFormatType::Unknown;
			}
			void set(MFormatType value) {
				switch (value)
				{
				case MFormatType::PCM8Bit:
					m_WaveFormat->wFormatTag = WAVE_FORMAT_PCM;
					m_WaveFormat->wBitsPerSample = 8;
					break;
				case MFormatType::PCM16Bit:
					m_WaveFormat->wFormatTag = WAVE_FORMAT_PCM;
					m_WaveFormat->wBitsPerSample = 16;
					break;
				case MFormatType::Float:
					m_WaveFormat->wFormatTag = WAVE_FORMAT_IEEE_FLOAT;
					m_WaveFormat->wBitsPerSample = 32;
					break;
				default:
					break;
				}
				Invalidate();
			}
		}
		property int FrameSize {
			int get() { return m_WaveFormat->nBlockAlign; }
		}
		property int SampleRate {
			int get() { return m_WaveFormat->nSamplesPerSec; }
			void set(int value) {
				m_WaveFormat->nSamplesPerSec = value;
				Invalidate();
			}
		}
		property int AverageByteRate {
			int get() { return m_WaveFormat->nAvgBytesPerSec; }
		}
		property int NumberOfChannels {
			int get() { return m_WaveFormat->nChannels; }
			void set(int value) {
				m_WaveFormat->nChannels = value;
				Invalidate();
			}
		}

	internal:
		MWaveFormat(WAVEFORMATEX *pWaveFormat) :
			m_WaveFormat(pWaveFormat) {}
		property WAVEFORMATEX* Data {
			WAVEFORMATEX* get() { return m_WaveFormat; }
		}

	private:
		~MWaveFormat() {
			CoTaskMemFree(m_WaveFormat);
		}
		void Invalidate() {
			m_WaveFormat->nBlockAlign = m_WaveFormat->nChannels * m_WaveFormat->wBitsPerSample / 8;
			m_WaveFormat->nAvgBytesPerSec = m_WaveFormat->nBlockAlign * m_WaveFormat->nSamplesPerSec;
		}

		WAVEFORMATEX	*m_WaveFormat;
	};
}
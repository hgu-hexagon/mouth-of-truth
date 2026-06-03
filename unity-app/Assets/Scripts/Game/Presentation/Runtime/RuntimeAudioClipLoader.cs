using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MouthOfTruth.Game.Presentation.Runtime
{
    public static class RuntimeAudioClipLoader
    {
        private const int PCM_AUDIO_FORMAT = 1;
        private const int EXTENSIBLE_AUDIO_FORMAT = 0xFFFE;

        public static Task<AudioClip> LoadClipOrNullAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || File.Exists(filePath) == false)
            {
                return Task.FromResult<AudioClip>(null);
            }

            if (string.Equals(Path.GetExtension(filePath), ".wav", StringComparison.OrdinalIgnoreCase) == false)
            {
                Debug.LogWarning($"Runtime audio loading expects wav assets: '{filePath}'.");
                return Task.FromResult<AudioClip>(null);
            }

            try
            {
                return Task.FromResult(loadWaveClip(filePath));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to load wav audio clip from '{filePath}': {exception.Message}");
                return Task.FromResult<AudioClip>(null);
            }
        }

        private static AudioClip loadWaveClip(string filePath)
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);

            if (readAscii(fileBytes, 0, 4) != "RIFF" || readAscii(fileBytes, 8, 4) != "WAVE")
            {
                throw new InvalidDataException("The file is not a RIFF/WAVE audio file.");
            }

            int channels = 0;
            int sampleRate = 0;
            int bitsPerSample = 0;
            int audioFormat = 0;
            int dataOffset = -1;
            int dataSize = 0;
            int chunkOffset = 12;

            while (chunkOffset + 8 <= fileBytes.Length)
            {
                string chunkID = readAscii(fileBytes, chunkOffset, 4);
                int chunkSize = BitConverter.ToInt32(fileBytes, chunkOffset + 4);
                int chunkDataOffset = chunkOffset + 8;

                if (chunkID == "fmt ")
                {
                    audioFormat = BitConverter.ToUInt16(fileBytes, chunkDataOffset);
                    channels = BitConverter.ToUInt16(fileBytes, chunkDataOffset + 2);
                    sampleRate = BitConverter.ToInt32(fileBytes, chunkDataOffset + 4);
                    bitsPerSample = BitConverter.ToUInt16(fileBytes, chunkDataOffset + 14);
                }
                else if (chunkID == "data")
                {
                    dataOffset = chunkDataOffset;
                    dataSize = chunkSize;
                    break;
                }

                chunkOffset = chunkDataOffset + chunkSize + (chunkSize % 2);
            }

            if (channels <= 0 || sampleRate <= 0 || dataOffset < 0 || dataSize <= 0)
            {
                throw new InvalidDataException("The wav file is missing required format or data chunks.");
            }

            float[] samples = decodeWaveSamples(fileBytes, dataOffset, dataSize, bitsPerSample, audioFormat);
            int sampleCount = samples.Length / channels;
            AudioClip audioClip = AudioClip.Create(Path.GetFileNameWithoutExtension(filePath), sampleCount, channels, sampleRate, false);
            audioClip.SetData(samples, 0);
            return audioClip;
        }

        private static float[] decodeWaveSamples(byte[] fileBytes, int dataOffset, int dataSize, int bitsPerSample, int audioFormat)
        {
            bool isSupportedWaveFormat = audioFormat == PCM_AUDIO_FORMAT
                || audioFormat == EXTENSIBLE_AUDIO_FORMAT;

            if (isSupportedWaveFormat == false || bitsPerSample != 16)
            {
                throw new InvalidDataException("Only 16-bit PCM wav audio is supported.");
            }

            int bytesPerSample = bitsPerSample / 8;
            int sampleValueCount = dataSize / bytesPerSample;
            float[] samples = new float[sampleValueCount];

            for (int sampleIndex = 0; sampleIndex < sampleValueCount; sampleIndex++)
            {
                int byteOffset = dataOffset + (sampleIndex * bytesPerSample);
                short sampleValue = BitConverter.ToInt16(fileBytes, byteOffset);
                samples[sampleIndex] = Mathf.Clamp(sampleValue / 32768.0f, -1.0f, 1.0f);
            }

            return samples;
        }

        private static string readAscii(byte[] fileBytes, int offset, int count)
        {
            if (offset < 0 || offset + count > fileBytes.Length)
            {
                return string.Empty;
            }

            return Encoding.ASCII.GetString(fileBytes, offset, count);
        }
    }
}

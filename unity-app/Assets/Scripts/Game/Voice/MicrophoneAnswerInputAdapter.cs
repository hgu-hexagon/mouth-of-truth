using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MouthOfTruth.Game.Voice
{
    public class MicrophoneAnswerInputAdapter : IAnswerCaptureInputAdapter
    {
        private const int SAMPLE_RATE = 16000;
        private const int MAX_SEGMENT_DURATION_SECONDS = 20;
        private const float SPEECH_WINDOW_SECONDS = 0.20f;
        private const float SPEECH_ACTIVITY_RMS_THRESHOLD = 0.0085f;
        private const float SPEECH_EVIDENCE_RMS_THRESHOLD = 0.0145f;
        private const float SPEECH_EVIDENCE_PEAK_RMS_THRESHOLD = 0.0200f;
        private const int MINIMUM_SPEECH_EVIDENCE_WINDOW_COUNT = 4;

        private readonly List<float[]> mRecordedSegments = new List<float[]>();

        private AudioClip mActiveRecordingClip;
        private string mSelectedDeviceName;
        private bool mIsCollecting;
        private bool mIsMicrophoneRunning;
        private int mSegmentStartSamplePosition;
        private int mRecordedSegmentCount;

        public MicrophoneAnswerInputAdapter()
        {
            mSelectedDeviceName = selectDefaultDeviceName();
        }

        public bool RequiresManualTextEntry => false;

        public string TranscriptPlaceholderText =>
            "음성 입력이 자동으로 수집됩니다.";

        public void Reset()
        {
            stopCurrentRecording(preserveActiveSegment: false);
            mRecordedSegments.Clear();
            mRecordedSegmentCount = 0;
        }

        public void PrepareAudioSession()
        {
            startMicrophoneIfNeeded();
        }

        public void BeginCollection()
        {
            startNewRecordingSegment();
        }

        public void PauseCollection()
        {
            stopCurrentRecording(preserveActiveSegment: true);
        }

        public void ResumeCollection()
        {
            startNewRecordingSegment();
        }

        public void CancelCollection()
        {
            Reset();
            stopMicrophoneIfRunning();
        }

        public AnswerCaptureFrameSnapshot Update(float deltaTimeSeconds)
        {
            bool isSpeechDetected = mIsCollecting && calculateCurrentSpeechRms() >= SPEECH_ACTIVITY_RMS_THRESHOLD;
            return new AnswerCaptureFrameSnapshot(string.Empty, isSpeechDetected);
        }

        public Task<AnswerCaptureResult> CompleteCollectionAsync(string questionID, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            stopCurrentRecording(preserveActiveSegment: true);

            if (mRecordedSegments.Count == 0)
            {
                return Task.FromResult(new AnswerCaptureResult(string.Empty, string.Empty, 0));
            }

            int totalSampleCount = 0;

            foreach (float[] segmentSamples in mRecordedSegments)
            {
                totalSampleCount += segmentSamples.Length;
            }

            float[] mergedSamples = new float[totalSampleCount];
            int nextOffset = 0;

            foreach (float[] segmentSamples in mRecordedSegments)
            {
                Array.Copy(segmentSamples, 0, mergedSamples, nextOffset, segmentSamples.Length);
                nextOffset += segmentSamples.Length;
            }

            string audioFilePath = AnswerAudioWorkspacePaths.BuildAudioFilePath(questionID);
            WaveFileWriter.WriteMono16BitPcm(audioFilePath, mergedSamples, SAMPLE_RATE);

            return Task.FromResult(new AnswerCaptureResult(string.Empty, audioFilePath, mRecordedSegmentCount));
        }

        public bool HasAvailableDevice()
        {
            return string.IsNullOrWhiteSpace(mSelectedDeviceName) == false;
        }

        private void startNewRecordingSegment()
        {
            if (HasAvailableDevice() == false)
            {
                throw new InvalidOperationException("No microphone input device is available.");
            }

            if (mIsCollecting)
            {
                return;
            }

            startMicrophoneIfNeeded();
            mSegmentStartSamplePosition = Mathf.Clamp(Microphone.GetPosition(mSelectedDeviceName), 0, mActiveRecordingClip.samples);
            mIsCollecting = true;
        }

        private void startMicrophoneIfNeeded()
        {
            if (HasAvailableDevice() == false)
            {
                throw new InvalidOperationException("No microphone input device is available.");
            }

            if (mIsMicrophoneRunning && mActiveRecordingClip != null)
            {
                return;
            }

            mActiveRecordingClip = Microphone.Start(mSelectedDeviceName, true, MAX_SEGMENT_DURATION_SECONDS, SAMPLE_RATE);

            if (mActiveRecordingClip == null)
            {
                mIsMicrophoneRunning = false;
                throw new InvalidOperationException($"Failed to start microphone capture for device '{mSelectedDeviceName}'.");
            }

            mIsMicrophoneRunning = true;
        }

        private void stopMicrophoneIfRunning()
        {
            if (mIsMicrophoneRunning == false || string.IsNullOrWhiteSpace(mSelectedDeviceName))
            {
                return;
            }

            Microphone.End(mSelectedDeviceName);
            mActiveRecordingClip = null;
            mIsMicrophoneRunning = false;
            mIsCollecting = false;
            mSegmentStartSamplePosition = 0;
        }

        private void stopCurrentRecording(bool preserveActiveSegment)
        {
            if (mIsCollecting == false || string.IsNullOrWhiteSpace(mSelectedDeviceName))
            {
                return;
            }

            float[] activeSegmentSamples = preserveActiveSegment
                ? readActiveSegmentSamples()
                : Array.Empty<float>();

            mIsCollecting = false;

            if (activeSegmentSamples.Length == 0)
            {
                return;
            }

            if (containsSpeechEvidence(activeSegmentSamples) == false)
            {
                return;
            }

            mRecordedSegments.Add(activeSegmentSamples);
            mRecordedSegmentCount += 1;
        }

        private float[] readActiveSegmentSamples()
        {
            if (mActiveRecordingClip == null || string.IsNullOrWhiteSpace(mSelectedDeviceName))
            {
                return Array.Empty<float>();
            }

            int currentSamplePosition = Mathf.Clamp(Microphone.GetPosition(mSelectedDeviceName), 0, mActiveRecordingClip.samples);
            int segmentStartSamplePosition = Mathf.Clamp(mSegmentStartSamplePosition, 0, mActiveRecordingClip.samples);
            int recordedSampleCount = calculateLoopedSampleDistance(segmentStartSamplePosition, currentSamplePosition, mActiveRecordingClip.samples);

            if (recordedSampleCount <= 0)
            {
                return Array.Empty<float>();
            }

            return readLoopedMonoSamples(segmentStartSamplePosition, recordedSampleCount);
        }

        private bool containsSpeechEvidence(float[] monoSamples)
        {
            if (monoSamples == null || monoSamples.Length == 0)
            {
                return false;
            }

            int windowSampleCount = Mathf.Max(1, Mathf.CeilToInt(SAMPLE_RATE * SPEECH_WINDOW_SECONDS));
            int strideSampleCount = Mathf.Max(1, windowSampleCount / 2);

            if (monoSamples.Length <= windowSampleCount)
            {
                float singleWindowRms = calculateWindowRms(monoSamples, 0, monoSamples.Length);
                return singleWindowRms >= SPEECH_EVIDENCE_RMS_THRESHOLD
                    && singleWindowRms >= SPEECH_EVIDENCE_PEAK_RMS_THRESHOLD;
            }

            int speechWindowCount = 0;
            float peakRms = 0.0f;

            for (int startSampleIndex = 0;
                 startSampleIndex + windowSampleCount <= monoSamples.Length;
                 startSampleIndex += strideSampleCount)
            {
                float windowRms = calculateWindowRms(monoSamples, startSampleIndex, windowSampleCount);
                peakRms = Math.Max(peakRms, windowRms);

                if (windowRms >= SPEECH_EVIDENCE_RMS_THRESHOLD)
                {
                    speechWindowCount += 1;
                }
            }

            int tailWindowStartIndex = Math.Max(0, monoSamples.Length - windowSampleCount);
            int tailSampleCount = monoSamples.Length - tailWindowStartIndex;
            float tailWindowRms = calculateWindowRms(monoSamples, tailWindowStartIndex, tailSampleCount);
            peakRms = Math.Max(peakRms, tailWindowRms);

            if (tailWindowRms >= SPEECH_EVIDENCE_RMS_THRESHOLD)
            {
                speechWindowCount += 1;
            }

            return speechWindowCount >= MINIMUM_SPEECH_EVIDENCE_WINDOW_COUNT
                && peakRms >= SPEECH_EVIDENCE_PEAK_RMS_THRESHOLD;
        }

        private float calculateWindowRms(float[] monoSamples, int startSampleIndex, int sampleCount)
        {
            if (sampleCount <= 0)
            {
                return 0.0f;
            }

            double squaredSum = 0.0d;

            for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex += 1)
            {
                float sampleValue = monoSamples[startSampleIndex + sampleIndex];
                squaredSum += sampleValue * sampleValue;
            }

            double meanSquare = squaredSum / sampleCount;
            return (float)Math.Sqrt(meanSquare);
        }

        private float calculateCurrentSpeechRms()
        {
            if (mActiveRecordingClip == null || mIsMicrophoneRunning == false || string.IsNullOrWhiteSpace(mSelectedDeviceName))
            {
                return 0.0f;
            }

            int currentSamplePosition = Mathf.Clamp(Microphone.GetPosition(mSelectedDeviceName), 0, mActiveRecordingClip.samples);
            int availableSampleCount = calculateLoopedSampleDistance(mSegmentStartSamplePosition, currentSamplePosition, mActiveRecordingClip.samples);
            int windowSampleCount = Mathf.Min(availableSampleCount, Mathf.CeilToInt(SAMPLE_RATE * SPEECH_WINDOW_SECONDS));

            if (windowSampleCount <= 0)
            {
                return 0.0f;
            }

            int startSampleOffset = currentSamplePosition - windowSampleCount;

            if (startSampleOffset < 0)
            {
                startSampleOffset += mActiveRecordingClip.samples;
            }

            float[] clipBuffer = readLoopedMonoSamples(startSampleOffset, windowSampleCount);

            if (clipBuffer.Length == 0)
            {
                return 0.0f;
            }

            double squaredSum = 0.0d;

            foreach (float sample in clipBuffer)
            {
                squaredSum += sample * sample;
            }

            double meanSquare = squaredSum / clipBuffer.Length;
            return (float)Math.Sqrt(meanSquare);
        }

        private float[] readLoopedMonoSamples(int startSamplePosition, int sampleCount)
        {
            if (mActiveRecordingClip == null || sampleCount <= 0)
            {
                return Array.Empty<float>();
            }

            int channels = Mathf.Max(1, mActiveRecordingClip.channels);
            int clipSampleCount = mActiveRecordingClip.samples;

            if (clipSampleCount <= 0)
            {
                return Array.Empty<float>();
            }

            int remainingSampleCount = Mathf.Min(sampleCount, clipSampleCount);
            int readSamplePosition = Mathf.Clamp(startSamplePosition, 0, clipSampleCount - 1);
            int outputSampleOffset = 0;
            float[] monoSamples = new float[remainingSampleCount];

            while (remainingSampleCount > 0)
            {
                int chunkSampleCount = Math.Min(remainingSampleCount, clipSampleCount - readSamplePosition);
                float[] interleavedSamples = new float[chunkSampleCount * channels];
                mActiveRecordingClip.GetData(interleavedSamples, readSamplePosition);

                for (int sampleIndex = 0; sampleIndex < chunkSampleCount; sampleIndex += 1)
                {
                    float mixedValue = 0.0f;

                    for (int channelIndex = 0; channelIndex < channels; channelIndex += 1)
                    {
                        mixedValue += interleavedSamples[(sampleIndex * channels) + channelIndex];
                    }

                    monoSamples[outputSampleOffset + sampleIndex] = mixedValue / channels;
                }

                outputSampleOffset += chunkSampleCount;
                remainingSampleCount -= chunkSampleCount;
                readSamplePosition = 0;
            }

            return monoSamples;
        }

        private int calculateLoopedSampleDistance(int startSamplePosition, int endSamplePosition, int clipSampleCount)
        {
            if (clipSampleCount <= 0)
            {
                return 0;
            }

            int clampedStartSamplePosition = Mathf.Clamp(startSamplePosition, 0, clipSampleCount);
            int clampedEndSamplePosition = Mathf.Clamp(endSamplePosition, 0, clipSampleCount);

            if (clampedEndSamplePosition >= clampedStartSamplePosition)
            {
                return clampedEndSamplePosition - clampedStartSamplePosition;
            }

            return (clipSampleCount - clampedStartSamplePosition) + clampedEndSamplePosition;
        }

        private string selectDefaultDeviceName()
        {
            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                return string.Empty;
            }

            return Microphone.devices[0];
        }
    }
}

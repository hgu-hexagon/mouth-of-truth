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
        private const float SPEECH_RMS_THRESHOLD = 0.0085f;

        private readonly List<float[]> mRecordedSegments = new List<float[]>();

        private AudioClip mActiveRecordingClip;
        private string mSelectedDeviceName;
        private bool mIsCollecting;
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
        }

        public AnswerCaptureFrameSnapshot Update(float deltaTimeSeconds)
        {
            bool isSpeechDetected = mIsCollecting && calculateCurrentSpeechRms() >= SPEECH_RMS_THRESHOLD;
            return new AnswerCaptureFrameSnapshot(string.Empty, isSpeechDetected);
        }

        public Task<AnswerCaptureResult> CompleteCollectionAsync(
            string questionID,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            stopCurrentRecording(preserveActiveSegment: true);

            if (mRecordedSegments.Count == 0)
            {
                return Task.FromResult(
                    new AnswerCaptureResult(
                        string.Empty,
                        string.Empty,
                        0));
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

            return Task.FromResult(
                new AnswerCaptureResult(
                    string.Empty,
                    audioFilePath,
                    mRecordedSegmentCount));
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

            mActiveRecordingClip = Microphone.Start(
                mSelectedDeviceName,
                false,
                MAX_SEGMENT_DURATION_SECONDS,
                SAMPLE_RATE);

            if (mActiveRecordingClip == null)
            {
                throw new InvalidOperationException(
                    $"Failed to start microphone capture for device '{mSelectedDeviceName}'.");
            }

            mIsCollecting = true;
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

            Microphone.End(mSelectedDeviceName);
            mActiveRecordingClip = null;
            mIsCollecting = false;

            if (activeSegmentSamples.Length == 0)
            {
                return;
            }

            if (containsSpeechSignal(activeSegmentSamples) == false)
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

            int recordedSampleCount = Mathf.Clamp(
                Microphone.GetPosition(mSelectedDeviceName),
                0,
                mActiveRecordingClip.samples);

            if (recordedSampleCount <= 0)
            {
                return Array.Empty<float>();
            }

            float[] clipBuffer = new float[recordedSampleCount * mActiveRecordingClip.channels];
            mActiveRecordingClip.GetData(clipBuffer, 0);

            if (mActiveRecordingClip.channels == 1)
            {
                return clipBuffer;
            }

            float[] monoBuffer = new float[recordedSampleCount];

            for (int sampleIndex = 0; sampleIndex < recordedSampleCount; sampleIndex += 1)
            {
                float mixedValue = 0.0f;

                for (int channelIndex = 0; channelIndex < mActiveRecordingClip.channels; channelIndex += 1)
                {
                    mixedValue += clipBuffer[(sampleIndex * mActiveRecordingClip.channels) + channelIndex];
                }

                monoBuffer[sampleIndex] = mixedValue / mActiveRecordingClip.channels;
            }

            return monoBuffer;
        }

        private bool containsSpeechSignal(float[] monoSamples)
        {
            if (monoSamples == null || monoSamples.Length == 0)
            {
                return false;
            }

            int windowSampleCount = Mathf.Max(
                1,
                Mathf.CeilToInt(SAMPLE_RATE * SPEECH_WINDOW_SECONDS));
            int strideSampleCount = Mathf.Max(1, windowSampleCount / 2);

            if (monoSamples.Length <= windowSampleCount)
            {
                return calculateWindowRms(monoSamples, 0, monoSamples.Length) >= SPEECH_RMS_THRESHOLD;
            }

            for (int startSampleIndex = 0;
                 startSampleIndex + windowSampleCount <= monoSamples.Length;
                 startSampleIndex += strideSampleCount)
            {
                if (calculateWindowRms(monoSamples, startSampleIndex, windowSampleCount)
                    >= SPEECH_RMS_THRESHOLD)
                {
                    return true;
                }
            }

            int tailWindowStartIndex = Math.Max(0, monoSamples.Length - windowSampleCount);
            int tailSampleCount = monoSamples.Length - tailWindowStartIndex;
            return calculateWindowRms(monoSamples, tailWindowStartIndex, tailSampleCount)
                   >= SPEECH_RMS_THRESHOLD;
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
            if (mActiveRecordingClip == null || string.IsNullOrWhiteSpace(mSelectedDeviceName))
            {
                return 0.0f;
            }

            int currentSamplePosition = Microphone.GetPosition(mSelectedDeviceName);

            if (currentSamplePosition <= 0)
            {
                return 0.0f;
            }

            int windowSampleCount = Mathf.Min(
                currentSamplePosition,
                Mathf.CeilToInt(SAMPLE_RATE * SPEECH_WINDOW_SECONDS));

            if (windowSampleCount <= 0)
            {
                return 0.0f;
            }

            int startSampleOffset = currentSamplePosition - windowSampleCount;
            float[] clipBuffer = new float[windowSampleCount * mActiveRecordingClip.channels];
            mActiveRecordingClip.GetData(clipBuffer, startSampleOffset);

            double squaredSum = 0.0d;

            foreach (float sample in clipBuffer)
            {
                squaredSum += sample * sample;
            }

            double meanSquare = squaredSum / clipBuffer.Length;
            return (float)Math.Sqrt(meanSquare);
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

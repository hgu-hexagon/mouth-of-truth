using System;
using System.Threading;
using System.Threading.Tasks;
using MouthOfTruth.Game.Presentation.Runtime;

namespace MouthOfTruth.Game.Voice
{
    public class KeyboardTranscriptAnswerInputAdapter : IAnswerCaptureInputAdapter
    {
        private const float TYPING_ACTIVITY_GRACE_SECONDS = 0.75f;

        private readonly MouthOfTruthGameView mGameView;

        private float mTypingActivityGraceSeconds;
        private string mLastObservedTranscript = string.Empty;

        public KeyboardTranscriptAnswerInputAdapter(MouthOfTruthGameView gameView)
        {
            if (gameView == null)
            {
                throw new ArgumentNullException(nameof(gameView));
            }

            mGameView = gameView;
        }

        public bool RequiresManualTextEntry => true;

        public string TranscriptPlaceholderText =>
            "답변을 입력하세요. 입력이 멈추면 3초 뒤 자동 종료됩니다.";

        public void Reset()
        {
            mTypingActivityGraceSeconds = 0.0f;
            mLastObservedTranscript = string.Empty;
        }

        public void BeginCollection()
        {
        }

        public void PauseCollection()
        {
        }

        public void ResumeCollection()
        {
        }

        public void CancelCollection()
        {
            Reset();
        }

        public AnswerCaptureFrameSnapshot Update(float deltaTimeSeconds)
        {
            string currentTranscript = mGameView.GetAnswerTranscript();

            if (string.Equals(currentTranscript, mLastObservedTranscript, StringComparison.Ordinal) == false)
            {
                mLastObservedTranscript = currentTranscript;
                mTypingActivityGraceSeconds = TYPING_ACTIVITY_GRACE_SECONDS;
            }

            if (mTypingActivityGraceSeconds > 0.0f)
            {
                mTypingActivityGraceSeconds = Math.Max(0.0f, mTypingActivityGraceSeconds - deltaTimeSeconds);
            }

            return new AnswerCaptureFrameSnapshot(currentTranscript, mTypingActivityGraceSeconds > 0.0f);
        }

        public Task<AnswerCaptureResult> CompleteCollectionAsync(string questionID, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string transcriptText = mGameView.GetAnswerTranscript().Trim();
            int voiceSegmentCount = string.IsNullOrWhiteSpace(transcriptText) ? 0 : 1;
            return Task.FromResult(new AnswerCaptureResult(transcriptText, string.Empty, voiceSegmentCount));
        }
    }
}

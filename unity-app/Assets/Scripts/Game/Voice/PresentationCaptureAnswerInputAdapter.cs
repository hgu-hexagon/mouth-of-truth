using System.Threading;
using System.Threading.Tasks;

namespace MouthOfTruth.Game.Voice
{
    public class PresentationCaptureAnswerInputAdapter : IAnswerCaptureInputAdapter
    {
        public bool RequiresManualTextEntry => false;

        public string TranscriptPlaceholderText => string.Empty;

        public void Reset()
        {
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
        }

        public AnswerCaptureFrameSnapshot Update(float deltaTimeSeconds)
        {
            return new AnswerCaptureFrameSnapshot(string.Empty, false);
        }

        public Task<AnswerCaptureResult> CompleteCollectionAsync(
            string questionID,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new AnswerCaptureResult(string.Empty, string.Empty, 0));
        }
    }
}

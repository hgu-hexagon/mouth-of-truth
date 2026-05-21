using System.Threading;
using System.Threading.Tasks;

namespace MouthOfTruth.Game.Voice
{
    public interface IAnswerCaptureInputAdapter
    {
        bool RequiresManualTextEntry { get; }

        string TranscriptPlaceholderText { get; }

        void Reset();

        void BeginCollection();

        void PauseCollection();

        void ResumeCollection();

        void CancelCollection();

        AnswerCaptureFrameSnapshot Update(float deltaTimeSeconds);

        Task<AnswerCaptureResult> CompleteCollectionAsync(string questionID, CancellationToken cancellationToken);
    }
}

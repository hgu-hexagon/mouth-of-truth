using System.Threading;
using System.Threading.Tasks;

namespace MouthOfTruth.Game.Face
{
    public class NoOpFaceCaptureInputAdapter : IFaceCaptureInputAdapter
    {
        public bool HasAvailableDevice()
        {
            return false;
        }

        public void Reset()
        {
        }

        public void BeginCollection(string questionID)
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

        public void Update(float deltaTimeSeconds)
        {
        }

        public Task<FaceCaptureResult> CompleteCollectionAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new FaceCaptureResult(string.Empty, 0));
        }
    }
}

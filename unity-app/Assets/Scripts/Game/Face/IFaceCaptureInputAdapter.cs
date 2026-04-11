using System.Threading;
using System.Threading.Tasks;

namespace MouthOfTruth.Game.Face
{
    public interface IFaceCaptureInputAdapter
    {
        bool HasAvailableDevice();

        void Reset();

        void BeginCollection(string questionID);

        void PauseCollection();

        void ResumeCollection();

        void CancelCollection();

        void Update(float deltaTimeSeconds);

        Task<FaceCaptureResult> CompleteCollectionAsync(CancellationToken cancellationToken);
    }
}

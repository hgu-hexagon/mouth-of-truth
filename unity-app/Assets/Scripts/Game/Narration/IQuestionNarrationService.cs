using System.Threading;
using System.Threading.Tasks;

namespace MouthOfTruth.Game.Narration
{
    public interface IQuestionNarrationService
    {
        Task SpeakQuestionAsync(string questionText, CancellationToken cancellationToken);
    }
}

using System.Threading;
using System.Threading.Tasks;
using MouthOfTruth.Game.Data;

namespace MouthOfTruth.Game.Narration
{
    public interface IQuestionNarrationService
    {
        Task SpeakQuestionAsync(QuestionDefinition questionDefinition, CancellationToken cancellationToken);
    }
}

using System.Threading;
using System.Threading.Tasks;
using MouthOfTruth.Game.Data;

namespace MouthOfTruth.Game.Narration
{
    public class SilentQuestionNarrationService : IQuestionNarrationService
    {
        private readonly int mDelayMilliseconds;

        public SilentQuestionNarrationService(int delayMilliseconds = 1200)
        {
            mDelayMilliseconds = delayMilliseconds;
        }

        public Task SpeakQuestionAsync(QuestionDefinition questionDefinition, CancellationToken cancellationToken)
        {
            return Task.Delay(mDelayMilliseconds, cancellationToken);
        }
    }
}

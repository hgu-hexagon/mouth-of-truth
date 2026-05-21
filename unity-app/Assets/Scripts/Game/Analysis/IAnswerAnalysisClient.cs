using System.Threading;
using System.Threading.Tasks;

namespace MouthOfTruth.Game.Analysis
{
    public interface IAnswerAnalysisClient
    {
        Task WarmUpAsync(CancellationToken cancellationToken);

        Task<AnswerAnalysisResult> AnalyzeAsync(AnswerAnalysisRequest answerAnalysisRequest, CancellationToken cancellationToken);
    }
}

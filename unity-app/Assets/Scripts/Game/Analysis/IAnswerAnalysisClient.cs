using System.Threading;
using System.Threading.Tasks;

namespace NewMouthOfTruth.Game.Analysis
{
    public interface IAnswerAnalysisClient
    {
        Task<AnswerAnalysisResult> AnalyzeAsync(
            AnswerAnalysisRequest answerAnalysisRequest,
            CancellationToken cancellationToken);
    }
}

namespace MouthOfTruth.Game.Input
{
    public interface IHandInteractionFallbackGate
    {
        bool ShouldSuppressFallbackInput { get; }
    }
}

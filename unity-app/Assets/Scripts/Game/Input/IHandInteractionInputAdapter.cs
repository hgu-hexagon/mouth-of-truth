namespace MouthOfTruth.Game.Input
{
    public interface IHandInteractionInputAdapter
    {
        bool WasInsertPressedThisFrame();

        bool WasInsertReleasedThisFrame();

        bool WasReturnToTitleTriggeredThisFrame();
    }
}

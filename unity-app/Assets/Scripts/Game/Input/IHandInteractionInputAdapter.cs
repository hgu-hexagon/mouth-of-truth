using UnityEngine;

namespace MouthOfTruth.Game.Input
{
    public interface IHandInteractionInputAdapter
    {
        bool TryGetPointerScreenPosition(out Vector2 screenPosition);

        bool WasReturnToTitleTriggeredThisFrame();
    }
}

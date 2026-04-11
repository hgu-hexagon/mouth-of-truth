using UnityEngine;

namespace MouthOfTruth.Game.Input.Keyboard
{
    public class KeyboardHandInputAdapter : IHandInteractionInputAdapter
    {
        private readonly KeyCode mReturnToTitleKeyCode;

        public KeyboardHandInputAdapter(KeyCode returnToTitleKeyCode = KeyCode.Backspace)
        {
            mReturnToTitleKeyCode = returnToTitleKeyCode;
        }

        public bool TryGetPointerScreenPosition(out Vector2 screenPosition)
        {
            screenPosition = UnityEngine.Input.mousePosition;
            return true;
        }

        public bool WasReturnToTitleTriggeredThisFrame()
        {
            return UnityEngine.Input.GetKeyDown(mReturnToTitleKeyCode);
        }
    }
}

using UnityEngine;

namespace MouthOfTruth.Game.Input.Keyboard
{
    public class KeyboardHandInputAdapter : IHandInteractionInputAdapter
    {
        private readonly KeyCode mInsertKeyCode;
        private readonly KeyCode mReturnToTitleKeyCode;

        public KeyboardHandInputAdapter(
            KeyCode insertKeyCode = KeyCode.Space,
            KeyCode returnToTitleKeyCode = KeyCode.Backspace)
        {
            mInsertKeyCode = insertKeyCode;
            mReturnToTitleKeyCode = returnToTitleKeyCode;
        }
        public bool WasInsertPressedThisFrame()
        {
            return UnityEngine.Input.GetKeyDown(mInsertKeyCode);
        }

        public bool WasInsertReleasedThisFrame()
        {
            return UnityEngine.Input.GetKeyUp(mInsertKeyCode);
        }

        public bool WasReturnToTitleTriggeredThisFrame()
        {
            return UnityEngine.Input.GetKeyDown(mReturnToTitleKeyCode);
        }
    }
}

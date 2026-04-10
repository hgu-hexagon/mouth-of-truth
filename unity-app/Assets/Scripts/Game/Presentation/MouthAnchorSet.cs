using UnityEngine;

namespace NewMouthOfTruth.Game.Presentation
{
    public class MouthAnchorSet : MonoBehaviour
    {
        [SerializeField] private Transform mTruthMouth;
        [SerializeField] private Transform mMouthFrontAnchor;
        [SerializeField] private Transform mMouthInnerAnchor;

        public Transform TruthMouth => mTruthMouth;

        public Transform MouthFrontAnchor => mMouthFrontAnchor;

        public Transform MouthInnerAnchor => mMouthInnerAnchor;

        public bool HasRequiredAnchors()
        {
            return mTruthMouth != null
                && mMouthFrontAnchor != null
                && mMouthInnerAnchor != null;
        }
    }
}

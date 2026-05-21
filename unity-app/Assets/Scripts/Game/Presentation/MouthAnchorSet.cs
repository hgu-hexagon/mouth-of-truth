using UnityEngine;

namespace MouthOfTruth.Game.Presentation
{
    public class MouthAnchorSet : MonoBehaviour
    {
        [SerializeField] private Transform mTruthMouth;
        [SerializeField] private Transform mMouthFrontAnchor;
        [SerializeField] private Transform mMouthInnerAnchor;

        public Transform TruthMouth => mTruthMouth;

        public Transform MouthFrontAnchor => mMouthFrontAnchor;

        public Transform MouthInnerAnchor => mMouthInnerAnchor;

        public void Configure(Transform truthMouth, Transform mouthFrontAnchor, Transform mouthInnerAnchor)
        {
            mTruthMouth = truthMouth;
            mMouthFrontAnchor = mouthFrontAnchor;
            mMouthInnerAnchor = mouthInnerAnchor;
        }

        public bool HasRequiredAnchors()
        {
            return mTruthMouth != null
                && mMouthFrontAnchor != null
                && mMouthInnerAnchor != null;
        }
    }
}

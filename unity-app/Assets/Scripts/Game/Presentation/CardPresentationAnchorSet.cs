using System;
using UnityEngine;

namespace MouthOfTruth.Game.Presentation
{
    public class CardPresentationAnchorSet : MonoBehaviour
    {
        [SerializeField] private Transform mLeftCard;
        [SerializeField] private Transform mCenterCard;
        [SerializeField] private Transform mRightCard;

        public Transform LeftCard => mLeftCard;

        public Transform CenterCard => mCenterCard;

        public Transform RightCard => mRightCard;

        public bool HasRequiredAnchors()
        {
            return mLeftCard != null
                && mCenterCard != null
                && mRightCard != null;
        }
    }
}

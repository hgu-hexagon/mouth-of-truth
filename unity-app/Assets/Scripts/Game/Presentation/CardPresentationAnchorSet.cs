using System;
using MouthOfTruth.Game.Data;
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

        public void Configure(Transform leftCard, Transform centerCard, Transform rightCard)
        {
            mLeftCard = leftCard;
            mCenterCard = centerCard;
            mRightCard = rightCard;
        }

        public Transform GetAnchor(EQuestionCardSlot questionCardSlot)
        {
            return questionCardSlot switch
            {
                EQuestionCardSlot.LeftCard => mLeftCard,
                EQuestionCardSlot.CenterCard => mCenterCard,
                EQuestionCardSlot.RightCard => mRightCard,
                _ => null,
            };
        }

        public bool HasRequiredAnchors()
        {
            return mLeftCard != null
                && mCenterCard != null
                && mRightCard != null;
        }
    }
}

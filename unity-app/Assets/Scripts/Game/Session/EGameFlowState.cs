namespace MouthOfTruth.Game.Session
{
    public enum EGameFlowState
    {
        StartScreen = 0,
        PresentingCards = 1,
        AwaitingCardSelection = 2,
        RevealingQuestionCard = 3,
        NarratingQuestion = 4,
        AwaitingHandInsertion = 5,
        InsertingHand = 6,
        Answering = 7,
        AnswerPaused = 8,
        AnalyzingAnswer = 9,
        ShowingResult = 10,
    }
}

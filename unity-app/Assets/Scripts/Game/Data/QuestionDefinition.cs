using System;

namespace MouthOfTruth.Game.Data
{
    [Serializable]
    public class QuestionDefinition
    {
        public QuestionDefinition(string id, string text, string category, int difficulty, bool isEnabled)
        {
            ID = id;
            Text = text;
            Category = category;
            Difficulty = difficulty;
            IsEnabled = isEnabled;
        }

        public string ID { get; }

        public string Text { get; }

        public string Category { get; }

        public int Difficulty { get; }

        public bool IsEnabled { get; }
    }
}

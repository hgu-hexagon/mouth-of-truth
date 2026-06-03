using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MouthOfTruth.Game.Data
{
    public static class QuestionPoolLoader
    {
        public static IReadOnlyList<QuestionDefinition> LoadQuestionDefinitions(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Question pool file path cannot be empty.", nameof(filePath));
            }

            if (File.Exists(filePath) == false)
            {
                throw new FileNotFoundException("Question pool JSON file was not found.", filePath);
            }

            string jsonText = File.ReadAllText(filePath);
            QuestionPoolJsonDocument questionPoolJsonDocument = JsonUtility.FromJson<QuestionPoolJsonDocument>(jsonText);

            if (questionPoolJsonDocument == null || questionPoolJsonDocument.questions == null)
            {
                throw new InvalidDataException("Question pool JSON did not contain a questions array.");
            }

            List<QuestionDefinition> questionDefinitions = new List<QuestionDefinition>();

            foreach (QuestionDefinitionJsonRecord questionDefinitionJsonRecord in questionPoolJsonDocument.questions)
            {
                if (string.IsNullOrWhiteSpace(questionDefinitionJsonRecord.id))
                {
                    throw new InvalidDataException("Question definition id cannot be empty.");
                }

                if (string.IsNullOrWhiteSpace(questionDefinitionJsonRecord.text))
                {
                    throw new InvalidDataException($"Question definition text cannot be empty: {questionDefinitionJsonRecord.id}");
                }

                string category = string.IsNullOrEmpty(questionDefinitionJsonRecord.category)
                    ? string.Empty
                    : questionDefinitionJsonRecord.category;

                questionDefinitions.Add(new QuestionDefinition(questionDefinitionJsonRecord.id, questionDefinitionJsonRecord.text, category, questionDefinitionJsonRecord.difficulty, questionDefinitionJsonRecord.enabled));
            }

            return questionDefinitions;
        }

        [Serializable]
        private sealed class QuestionPoolJsonDocument
        {
            // JsonUtility maps fields by the external question-pool JSON keys.
            public List<QuestionDefinitionJsonRecord> questions = new List<QuestionDefinitionJsonRecord>();
        }

        [Serializable]
        private sealed class QuestionDefinitionJsonRecord
        {
            // JsonUtility maps fields by the external question-pool JSON keys.
            public string id = string.Empty;
            public string text = string.Empty;
            public string category = string.Empty;
            public int difficulty = 0;
            public bool enabled = true;
        }
    }
}

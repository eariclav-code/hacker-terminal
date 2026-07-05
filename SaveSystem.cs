using System;
using System.IO;
using System.Text.Json;

namespace HackerTerminal
{
    // Сохраняет и загружает прогресс игры в JSON файл.
    internal static class SaveSystem
    {
        private static readonly string SavePath = "save.json";

        // Данные для сериализации (только то, что нужно сохранить).
        private class SaveData
        {
            public string CurrentNodeId { get; set; } = "";
            public int Level { get; set; }
            public int Score { get; set; }
            public string[] FoundKeys { get; set; } = Array.Empty<string>();
            public string[] DecryptedFiles { get; set; } = Array.Empty<string>();
            public string[] KnownNodes { get; set; } = Array.Empty<string>();
            public string[] VisitedNodes { get; set; } = Array.Empty<string>();
            public bool GameCompleted { get; set; }
            public string? EndingAchieved { get; set; }
        }

        public static void Save(GameState state)
        {
            var data = new SaveData
            {
                CurrentNodeId = state.CurrentNodeId,
                Level = state.Level,
                Score = state.Score,
                FoundKeys = new string[state.FoundKeys.Count],
                DecryptedFiles = new string[state.DecryptedFiles.Count],
                KnownNodes = new string[state.KnownNodes.Count],
                VisitedNodes = new string[state.VisitedNodes.Count],
                GameCompleted = state.GameCompleted,
                EndingAchieved = state.EndingAchieved
            };

            state.FoundKeys.CopyTo(data.FoundKeys);
            state.DecryptedFiles.CopyTo(data.DecryptedFiles);
            state.KnownNodes.CopyTo(data.KnownNodes);
            state.VisitedNodes.CopyTo(data.VisitedNodes);

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(SavePath, json);
        }

        // Возвращает true, если сохранение найдено и успешно применено к state.
        public static bool Load(GameState state)
        {
            if (!File.Exists(SavePath))
                return false;

            try
            {
                string json = File.ReadAllText(SavePath);
                var data = JsonSerializer.Deserialize<SaveData>(json);

                if (data == null || string.IsNullOrEmpty(data.CurrentNodeId))
                    return false;

                state.CurrentNodeId = data.CurrentNodeId;
                state.Level = data.Level;
                state.Score = data.Score;
                state.GameCompleted = data.GameCompleted;
                state.EndingAchieved = data.EndingAchieved;

                foreach (var key in data.FoundKeys)
                    state.FoundKeys.Add(key);

                foreach (var file in data.DecryptedFiles)
                    state.DecryptedFiles.Add(file);

                foreach (var node in data.KnownNodes)
                    state.KnownNodes.Add(node);

                foreach (var node in data.VisitedNodes)
                    state.VisitedNodes.Add(node);

                return true;
            }
            catch (Exception)
            {
                // Если файл сохранения повреждён — просто начинаем заново.
                return false;
            }
        }
    }
}

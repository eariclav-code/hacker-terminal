using System;
using System.IO;
using System.Text.Json;

namespace HackerTerminal
{
    // Сохраняет и загружает прогресс игры в JSON файл.
    internal static class SaveSystem
    {
        private static readonly string SavePath = "save.json";

        // Данные для сериализации (только то, что нужно сохранить)
        private class SaveData
        {
            public int Level { get; set; }
            public int Score { get; set; }
            public string[] FoundKeys { get; set; } = Array.Empty<string>();
            public string[] DecryptedFiles { get; set; } = Array.Empty<string>();
            public bool GameCompleted { get; set; }
        }

        public static void Save(GameState state)
        {
            var data = new SaveData
            {
                Level = state.Level,
                Score = state.Score,
                FoundKeys = new string[state.FoundKeys.Count],
                DecryptedFiles = new string[state.DecryptedFiles.Count],
                GameCompleted = state.GameCompleted
            };

            state.FoundKeys.CopyTo(data.FoundKeys);
            state.DecryptedFiles.CopyTo(data.DecryptedFiles);

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(SavePath, json);
        }

        public static void Load(GameState state)
        {
            if (!File.Exists(SavePath))
                return;

            try
            {
                string json = File.ReadAllText(SavePath);
                var data = JsonSerializer.Deserialize<SaveData>(json);

                if (data == null)
                    return;

                state.Level = data.Level;
                state.Score = data.Score;
                state.GameCompleted = data.GameCompleted;

                foreach (var key in data.FoundKeys)
                    state.FoundKeys.Add(key);

                foreach (var file in data.DecryptedFiles)
                    state.DecryptedFiles.Add(file);
            }
            catch (Exception)
            {
                // Если файл сохранения повреждён — просто начинаем заново
            }
        }
    }
}
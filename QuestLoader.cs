using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace HackerTerminal.Quests
{
    // Читает JSON-файлы квестов из папки /quests и строит по ним
    // карту узлов сети. Весь сюжет, тексты файлов, пароли и концовки
    // хранятся в этих JSON-файлах, а не в коде.
    internal static class QuestLoader
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        public static Dictionary<string, NetworkNodeData> Nodes { get; private set; } = new();
        public static string StartNodeId { get; private set; } = "";

        public static void LoadAll(string questsDir = "quests")
        {
            Nodes = new Dictionary<string, NetworkNodeData>();

            string manifestPath = Path.Combine(questsDir, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                throw new FileNotFoundException(
                    $"Не найден файл манифеста квестов: {manifestPath}. " +
                    "Убедись, что папка /quests лежит рядом с исполняемым файлом.");
            }

            string manifestJson = File.ReadAllText(manifestPath);
            var manifest = JsonSerializer.Deserialize<QuestManifest>(manifestJson, Options)
                ?? throw new InvalidDataException("Манифест квестов повреждён или пуст.");

            if (string.IsNullOrEmpty(manifest.StartNode))
                throw new InvalidDataException("В манифесте квестов не указан startNode.");

            StartNodeId = manifest.StartNode;

            foreach (var fileName in manifest.Nodes)
            {
                string nodePath = Path.Combine(questsDir, fileName);

                if (!File.Exists(nodePath))
                    throw new FileNotFoundException($"Не найден файл узла квеста: {nodePath}");

                string json = File.ReadAllText(nodePath);
                var node = JsonSerializer.Deserialize<NetworkNodeData>(json, Options)
                    ?? throw new InvalidDataException($"Файл узла повреждён: {nodePath}");

                if (string.IsNullOrEmpty(node.Id))
                    throw new InvalidDataException($"У узла в файле {nodePath} не указан id.");

                Nodes[node.Id] = node;
            }

            if (!Nodes.ContainsKey(StartNodeId))
            {
                throw new InvalidDataException(
                    $"Стартовый узел '{StartNodeId}' не найден среди загруженных узлов квестов.");
            }
        }

        public static NetworkNodeData GetNode(string id)
        {
            if (!Nodes.TryGetValue(id, out var node))
                throw new KeyNotFoundException($"Узел сети '{id}' не найден в квестах.");

            return node;
        }
    }
}

using System.Collections.Generic;

namespace HackerTerminal
{
    // Хранит текущее состояние игры: где находится игрок в сетевой карте,
    // прогресс, найденные ключи доступа и достигнутую концовку.
    internal class GameState
    {
        // Id узла сети, к которому сейчас подключён игрок (например "home-pc").
        public string CurrentNodeId { get; set; } = "";

        // Текущая папка внутри файловой системы текущего узла.
        public VirtualDirectory CurrentDirectory { get; set; } = null!;

        // Построенные (лениво) виртуальные ФС для каждого посещённого узла.
        public Dictionary<string, VirtualDirectory> NodeFileSystems { get; } = new();

        // Узлы, известные игроку (обнаружены через scan или это стартовый узел) —
        // только к ним можно подключиться командой connect.
        public HashSet<string> KnownNodes { get; set; } = new();

        // Узлы, к которым игрок хотя бы раз подключался (нужно для восстановления ФС из сохранения).
        public HashSet<string> VisitedNodes { get; set; } = new();

        // Текущий уровень (0 — старт, 1, 2 — этапы, 3 — финал/игра пройдена).
        public int Level { get; set; } = 0;

        // Очки игрока.
        public int Score { get; set; } = 0;

        // Найденные ключи доступа (пароли, коды), например "shadow42", "admin1234".
        public HashSet<string> FoundKeys { get; set; } = new();

        // Расшифрованные файлы. Хранится как "nodeId:относительный/путь/файл",
        // чтобы одинаковые имена файлов на разных узлах не конфликтовали.
        public HashSet<string> DecryptedFiles { get; set; } = new();

        // Игра завершена (достигнута любая из концовок).
        public bool GameCompleted { get; set; } = false;

        // Id достигнутой концовки ("clean", "partial", "traced", "bribe"), если игра завершена.
        public string? EndingAchieved { get; set; } = null;

        // Корень файловой системы узла, на котором сейчас находится игрок.
        public VirtualDirectory CurrentNodeRoot => NodeFileSystems[CurrentNodeId];

        public string GetCurrentPath()
        {
            var root = CurrentNodeRoot;

            if (CurrentDirectory == root)
                return "/";

            var parts = new Stack<string>();
            var dir = CurrentDirectory;

            while (dir != root && dir.Parent != null)
            {
                parts.Push(dir.Name);
                dir = dir.Parent;
            }

            return "/" + string.Join("/", parts);
        }

        // Строка-приглашение терминала: "узел:/путь".
        public string GetPrompt() => $"{CurrentNodeId}:{GetCurrentPath()}";
    }
}

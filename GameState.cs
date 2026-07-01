using System.Collections.Generic;

namespace HackerTerminal
{
    // Хранит текущее состояние игры.
    internal class GameState
    {
        public VirtualDirectory CurrentDirectory { get; set; }
        public VirtualDirectory RootDirectory { get; }

        // Текущий уровень (0 — старт, 1 — первый уровень пройден и т.д.)
        public int Level { get; set; } = 0;

        // Очки игрока
        public int Score { get; set; } = 0;

        // Найденные ключи и пароли (например "shadow42" после расшифровки)
        public HashSet<string> FoundKeys { get; set; } = new();

        // Открытые файлы (имена файлов, которые игрок уже расшифровал)
        public HashSet<string> DecryptedFiles { get; set; } = new();

        public GameState(VirtualDirectory root)
        {
            RootDirectory = root;
            CurrentDirectory = root;
        }

        public string GetCurrentPath()
        {
            if (CurrentDirectory == RootDirectory)
                return "/";

            var parts = new System.Collections.Generic.Stack<string>();
            var dir = CurrentDirectory;

            while (dir != RootDirectory && dir.Parent != null)
            {
                parts.Push(dir.Name);
                dir = dir.Parent;
            }

            return "/" + string.Join("/", parts);
        }
    }
}
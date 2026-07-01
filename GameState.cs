namespace HackerTerminal
{
    // Хранит текущее состояние игры.
    // Позже сюда добавим уровень, очки, найденные ключи.
    internal class GameState
    {
        // Текущая папка, в которой находится игрок.
        public VirtualDirectory CurrentDirectory { get; set; }

        // Корневая папка — нужна чтобы знать, выше неё нельзя подняться.
        public VirtualDirectory RootDirectory { get; }

        public GameState(VirtualDirectory root)
        {
            RootDirectory = root;
            CurrentDirectory = root;
        }

        // Возвращает текущий путь для отображения в приглашении ввода (например, /system/secret).
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
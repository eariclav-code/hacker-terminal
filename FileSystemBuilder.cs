namespace HackerTerminal
{
    // Строит стартовое дерево фейковой файловой системы.
    // На вторник недели 1 - это просто захардкоженная структура для проверки ls/cd/cat.
    // Позже (неделя 2-3) сюда добавим зашифрованные файлы и сюжетные подсказки.
    internal static class FileSystemBuilder
    {
        public static VirtualDirectory BuildRoot()
        {
            var root = new VirtualDirectory("/");

            // Корневые файлы
            root.AddFile(new VirtualFile("readme.txt",
                "Добро пожаловать в систему. Используй 'help' для списка команд."));

            // Папка home
            var home = new VirtualDirectory("home", root);
            home.AddFile(new VirtualFile("notes.txt",
                "Напоминание самому себе: сменить пароль от почты."));
            root.AddSubdirectory(home);

            // Папка system
            var system = new VirtualDirectory("system", root);
            system.AddFile(new VirtualFile("log.txt",
                "12:01 - Login successful.\n12:03 - Unknown process started."));
            root.AddSubdirectory(system);

            // Скрытая папка внутри system — появится после hack
            var secret = new VirtualDirectory("secret", system, isHidden: true);
            secret.AddFile(new VirtualFile("password.txt", "admin1234"));
            system.AddSubdirectory(secret);

            return root;
        }
    }
}
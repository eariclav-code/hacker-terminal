namespace HackerTerminal
{
    internal static class FileSystemBuilder
    {
        public static VirtualDirectory BuildRoot()
        {
            var root = new VirtualDirectory("/");

            // Корневые файлы
            root.AddFile(new VirtualFile("readme.txt",
                "Добро пожаловать в защищённую систему Nortech Industries.\n" +
                "Несанкционированный доступ отслеживается службой безопасности.\n" +
                "Используй 'help' для списка команд."));

            // Подсказка к расшифровке — лежит открыто
            root.AddFile(new VirtualFile("hint.txt",
                "Один из файлов зашифрован шифром Цезаря. Сдвиг: 3. Используй: decrypt <файл> 3"));

            // Список известных узлов сети — виден с самого начала,
            // но nortech-core станет доступен только после уровня 2
            root.AddFile(new VirtualFile("network.txt",
                "СПИСОК ИЗВЕСТНЫХ УЗЛОВ СЕТИ:\n" +
                "  nortech-core [ЗАБЛОКИРОВАН] — требуется код доступа администратора\n\n" +
                "Используй: connect <узел>"));

            // Папка home
            var home = new VirtualDirectory("home", root);
            home.AddFile(new VirtualFile("notes.txt",
                "Если меня уволят раньше времени — не забудь: пароль от резервного\n" +
                "архива лежит в secret.txt. Никому не говори."));

            // Зашифрованный файл — содержимое зашифровано шифром Цезаря со сдвигом 3
            string secretText = "access granted password is shadow42";
            string encryptedText = CaesarCipher.Encrypt(secretText, 3);
            home.AddFile(new VirtualFile("secret.txt", encryptedText, isEncrypted: true, cipherShift: 3));

            root.AddSubdirectory(home);

            // Папка system
            var system = new VirtualDirectory("system", root);
            system.AddFile(new VirtualFile("log.txt",
                "12:01 - Login successful.\n" +
                "12:03 - Unknown process started.\n" +
                "12:07 - Обнаружена попытка подключения с внешнего узла.\n" +
                "12:15 - Административная сессия истекла."));
            root.AddSubdirectory(system);

            // Скрытая папка внутри system — открывается после hack mainframe
            var secret = new VirtualDirectory("secret", system, isHidden: true);
            secret.AddFile(new VirtualFile("password.txt", "admin1234"));
            secret.AddFile(new VirtualFile("briefing.txt",
                "ВНУТРЕННЯЯ СЛУЖЕБНАЯ ЗАПИСКА\n\n" +
                "Проект \"Тихая гавань\" закрыт. Данные об утечках на заводе в\n" +
                "Северном секторе перемещены в архивный узел nortech-core.\n" +
                "Код доступа админа действует и там же — не меняли с прошлого\n" +
                "квартала.\n\n" +
                "Если это читаешь не ты, Марк — у нас проблема.\n\n" +
                "Используй: connect nortech-core"));
            system.AddSubdirectory(secret);

            return root;
        }
    }
}
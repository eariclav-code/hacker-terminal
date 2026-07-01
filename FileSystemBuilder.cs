namespace HackerTerminal
{
    internal static class FileSystemBuilder
    {
        public static VirtualDirectory BuildRoot()
        {
            var root = new VirtualDirectory("/");

            // Корневые файлы
            root.AddFile(new VirtualFile("readme.txt",
                "Добро пожаловать в систему. Используй 'help' для списка команд."));

            // Подсказка к расшифровке — лежит открыто
            root.AddFile(new VirtualFile("hint.txt",
                "Один из файлов зашифрован шифром Цезаря. Сдвиг: 3. Используй: decrypt <файл> 3"));

            // Папка home
            var home = new VirtualDirectory("home", root);
            home.AddFile(new VirtualFile("notes.txt",
                "Напоминание самому себе: сменить пароль от почты."));

            // Зашифрованный файл — содержимое зашифровано шифром Цезаря со сдвигом 3
            string secretText = "access granted password is shadow42";
            string encryptedText = CaesarCipher.Encrypt(secretText, 3);
            home.AddFile(new VirtualFile("secret.txt", encryptedText, isEncrypted: true, cipherShift: 3));

            root.AddSubdirectory(home);

            // Папка system
            var system = new VirtualDirectory("system", root);
            system.AddFile(new VirtualFile("log.txt",
                "12:01 - Login successful.\n12:03 - Unknown process started."));
            root.AddSubdirectory(system);

            // Скрытая папка внутри system
            var secret = new VirtualDirectory("secret", system, isHidden: true);
            secret.AddFile(new VirtualFile("password.txt", "admin1234"));
            system.AddSubdirectory(secret);

            return root;
        }
    }
}
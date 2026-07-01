namespace HackerTerminal
{
    // Представляет один файл в фейковой файловой системе.
    internal class VirtualFile
    {
        public string Name { get; }

        // Реальное содержимое файла (то, что увидит игрок после расшифровки или если файл открыт сразу).
        public string Content { get; private set; }

        // Зашифрован ли файл сейчас. Пока true - cat должен отказывать в просмотре.
        public bool IsEncrypted { get; private set; }

        // Сдвиг шифра Цезаря, которым зашифрован файл (используется decrypt-ом).
        public int CipherShift { get; }

        public VirtualFile(string name, string content, bool isEncrypted = false, int cipherShift = 0)
        {
            Name = name;
            Content = content;
            IsEncrypted = isEncrypted;
            CipherShift = cipherShift;
        }

        // Помечает файл расшифрованным и заменяет содержимое на открытый текст.
        public void Decrypt(string plainText)
        {
            Content = plainText;
            IsEncrypted = false;
        }
    }
}
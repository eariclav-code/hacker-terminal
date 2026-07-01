namespace HackerTerminal
{
    // Реализация шифра Цезаря.
    // Encrypt — зашифровать текст со сдвигом.
    // Decrypt — расшифровать текст (сдвиг в обратную сторону).
    internal static class CaesarCipher
    {
        public static string Encrypt(string text, int shift)
        {
            return Shift(text, shift);
        }

        public static string Decrypt(string text, int shift)
        {
            return Shift(text, -shift);
        }

        private static string Shift(string text, int shift)
        {
            var result = new System.Text.StringBuilder();

            foreach (char c in text)
            {
                if (char.IsLetter(c))
                {
                    // Определяем базовый символ (A или a)
                    char base_ = char.IsUpper(c) ? 'A' : 'a';

                    // Сдвигаем букву по кругу (26 букв в латинском алфавите)
                    char shifted = (char)(((c - base_ + shift % 26 + 26) % 26) + base_);
                    result.Append(shifted);
                }
                else
                {
                    // Не буква — оставляем как есть (пробелы, цифры, знаки)
                    result.Append(c);
                }
            }

            return result.ToString();
        }
    }
}
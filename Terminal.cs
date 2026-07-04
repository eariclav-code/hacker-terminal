using System;
using System.Threading;

namespace HackerTerminal
{
    // Централизованная терминальная тема: зелёный "хакерский" стиль
    // и эффект печатной машинки, используемые по всей игре.
    internal static class Terminal
    {
        public const ConsoleColor DefaultColor = ConsoleColor.Green;
        public const ConsoleColor WarningColor = ConsoleColor.Red;

        // Печатает текст посимвольно с задержкой (эффект печатной машинки).
        public static void Type(string text, int delayMs = 20, ConsoleColor color = DefaultColor)
        {
            Console.ForegroundColor = color;
            foreach (char c in text)
            {
                Console.Write(c);
                Thread.Sleep(delayMs);
            }
            Console.ResetColor();
        }

        // То же самое, но с переносом строки в конце.
        public static void TypeLine(string text, int delayMs = 20, ConsoleColor color = DefaultColor)
        {
            Type(text, delayMs, color);
            Console.WriteLine();
        }

        // Печатает многоточие "загрузки" с задержкой между точками.
        public static void Dots(int count = 3, int delayMs = 300)
        {
            Console.ForegroundColor = DefaultColor;
            for (int i = 0; i < count; i++)
            {
                Thread.Sleep(delayMs);
                Console.Write(".");
            }
            Console.ResetColor();
            Console.WriteLine();
        }
    }
}

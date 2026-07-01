using System;
using System.Threading;
using Figgle;
using Figgle.Fonts;
using Spectre.Console;

namespace HackerTerminal
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Hacker Terminal";
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            ShowBanner();
            RunBootSequence();

            // На этом этапе главный цикл команд ещё не готов —
            // это будет следующим шагом (среда, неделя 1).
            AnsiConsole.MarkupLine("[green]> Система готова. Главный цикл команд появится на следующем шаге.[/]");
        // Временная проверка (уберём в среду, когда появится главный цикл команд)
        var root = FileSystemBuilder.BuildRoot();
        AnsiConsole.MarkupLine($"[green]ФС загружена: файлов в корне: {root.Files.Count}, папок: {System.Linq.Enumerable.Count(root.VisibleSubdirectories())}[/]");
        }

        static void ShowBanner()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;

            string banner = FiggleFonts.Standard.Render("HACKER TERMINAL");
            Console.WriteLine(banner);

            Console.ResetColor();
        }

        static void RunBootSequence()
        {
            string[] bootMessages = new[]
            {
                "Booting kernel...",
                "Mounting virtual filesystem...",
                "Establishing secure shell...",
                "Loading user profile: [unknown]...",
                "Initializing terminal interface..."
            };

            foreach (var message in bootMessages)
            {
                TypePrint(message + " ", 15);
                Thread.Sleep(200);
                AnsiConsole.MarkupLine("[green][[OK]][/]");
                Thread.Sleep(150);
            }

            Thread.Sleep(400);
            AnsiConsole.MarkupLine("\n[green]Welcome, intruder.[/]\n");
        }

        // Эффект печатной машинки — текст печатается по буквам с задержкой
        static void TypePrint(string text, int delayMs = 20)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            foreach (char c in text)
            {
                Console.Write(c);
                Thread.Sleep(delayMs);
            }
            Console.ResetColor();
        }
    }
}
using System;
using System.Threading;
using Figgle;
using Figgle.Fonts;
using Spectre.Console;

namespace HackerTerminal
{
    internal class Program
    {
        static GameState? _state;

        static void Main(string[] args)
        {
            Console.Title = "Hacker Terminal";
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            ShowBanner();
            RunBootSequence();

            var root = FileSystemBuilder.BuildRoot();
            _state = new GameState(root);

            RunCommandLoop();
        }

        static void RunCommandLoop()
        {
            AnsiConsole.MarkupLine("[green]Введи 'help' для списка команд.[/]\n");

            while (true)
            {
                string path = _state!.GetCurrentPath();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"root@hacker:{path}> ");
                Console.ResetColor();

                string? input = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(input))
                    continue;

                string[] parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                string command = parts[0].ToLower();
                string argument = parts.Length > 1 ? parts[1] : "";

                HandleCommand(command, argument);
            }
        }

        static void HandleCommand(string command, string argument)
        {
            switch (command)
            {
                case "help":
                    ShowHelp();
                    break;

                case "exit":
                    TypePrint("\nЗавершение сеанса...\n", 30);
                    Environment.Exit(0);
                    break;

                case "ls":
                case "dir":
                    CommandLs();
                    break;

                case "cd":
                    CommandCd(argument);
                    break;

                case "cat":
                case "open":
                    CommandCat(argument);
                    break;

                case "clear":
                case "cls":
                    Console.Clear();
                    break;

                default:
                    AnsiConsole.MarkupLine($"[red]Неизвестная команда: '{command}'. Введи 'help'.[/]");
                    break;
            }
        }

        static void ShowHelp()
        {
            AnsiConsole.MarkupLine("\n[green]Доступные команды:[/]");
            AnsiConsole.MarkupLine("[green]  help[/]              — показать этот список");
            AnsiConsole.MarkupLine("[green]  ls / dir[/]          — показать содержимое папки");
            AnsiConsole.MarkupLine("[green]  cd <папка>[/]        — перейти в папку (cd .. — назад)");
            AnsiConsole.MarkupLine("[green]  cat <файл>[/]        — прочитать файл");
            AnsiConsole.MarkupLine("[green]  decrypt <файл>[/]    — расшифровать файл");
            AnsiConsole.MarkupLine("[green]  hack <цель>[/]       — взломать цель");
            AnsiConsole.MarkupLine("[green]  scan[/]              — сканировать систему");
            AnsiConsole.MarkupLine("[green]  connect <адрес>[/]   — подключиться к узлу");
            AnsiConsole.MarkupLine("[green]  status[/]            — показать прогресс");
            AnsiConsole.MarkupLine("[green]  clear / cls[/]       — очистить экран");
            AnsiConsole.MarkupLine("[green]  exit[/]              — выйти из игры\n");
        }

        static void CommandLs()
        {
            var dir = _state!.CurrentDirectory;

            AnsiConsole.MarkupLine($"\n[green]Содержимое папки {_state.GetCurrentPath()}:[/]");

            foreach (var subdir in dir.VisibleSubdirectories())
            {
                AnsiConsole.MarkupLine($"[blue]  [[{subdir.Name}/]][/]");
            }

            foreach (var file in dir.Files.Values)
            {
                string encrypted = file.IsEncrypted ? " [red][[зашифрован]][/]" : "";
                AnsiConsole.MarkupLine($"[green]  {file.Name}[/]{encrypted}");
            }

            Console.WriteLine();
        }

        static void CommandCd(string argument)
        {
            if (string.IsNullOrEmpty(argument))
            {
                AnsiConsole.MarkupLine("[red]Укажи папку. Пример: cd home[/]");
                return;
            }

            if (argument == "..")
            {
                if (_state!.CurrentDirectory == _state.RootDirectory)
                {
                    AnsiConsole.MarkupLine("[red]Выше корня подняться нельзя.[/]");
                    return;
                }

                _state.CurrentDirectory = _state.CurrentDirectory.Parent!;
                return;
            }

            var current = _state!.CurrentDirectory;

            if (current.Subdirectories.TryGetValue(argument, out var target))
            {
                if (target.IsHidden)
                {
                    AnsiConsole.MarkupLine($"[red]Папка '{argument}' не найдена.[/]");
                    return;
                }

                _state.CurrentDirectory = target;
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Папка '{argument}' не найдена.[/]");
            }
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
    static void CommandCat(string argument)
        {
            // Пустой аргумент
            if (string.IsNullOrEmpty(argument))
            {
                AnsiConsole.MarkupLine("[red]Укажи файл. Пример: cat readme.txt[/]");
                return;
            }

            var dir = _state!.CurrentDirectory;

            // Ищем файл в текущей папке
            if (!dir.Files.TryGetValue(argument, out var file))
            {
                AnsiConsole.MarkupLine($"[red]Файл '{argument}' не найден.[/]");
                return;
            }

            // Файл зашифрован
            if (file.IsEncrypted)
            {
                AnsiConsole.MarkupLine($"[red]Файл '{argument}' зашифрован. Используй: decrypt {argument} <ключ>[/]");
                return;
            }

            // Выводим содержимое
            AnsiConsole.MarkupLine($"\n[green]--- {argument} ---[/]");
            Console.WriteLine(file.Content);
            AnsiConsole.MarkupLine("[green]--- конец файла ---[/]\n");
        }
    }
}
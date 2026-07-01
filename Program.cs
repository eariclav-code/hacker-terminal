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

                case "decrypt":
                    CommandDecrypt(argument);
                    break;

                case "hack":
                    AnsiConsole.MarkupLine("[yellow]Команда 'hack' появится завтра.[/]");
                    break;

                case "scan":
                    AnsiConsole.MarkupLine("[yellow]Команда 'scan' появится завтра.[/]");
                    break;

                case "connect":
                    AnsiConsole.MarkupLine("[yellow]Команда 'connect' появится позже.[/]");
                    break;

                case "status":
                    AnsiConsole.MarkupLine("[yellow]Команда 'status' появится завтра.[/]");
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

        static void CommandCat(string argument)
        {
            if (string.IsNullOrEmpty(argument))
            {
                AnsiConsole.MarkupLine("[red]Укажи файл. Пример: cat readme.txt[/]");
                return;
            }

            var dir = _state!.CurrentDirectory;

            if (!dir.Files.TryGetValue(argument, out var file))
            {
                AnsiConsole.MarkupLine($"[red]Файл '{argument}' не найден.[/]");
                return;
            }

            if (file.IsEncrypted)
            {
                AnsiConsole.MarkupLine($"[red]Файл '{argument}' зашифрован. Используй: decrypt {argument} <ключ>[/]");
                return;
            }

            AnsiConsole.MarkupLine($"\n[green]--- {argument} ---[/]");
            Console.WriteLine(file.Content);
            AnsiConsole.MarkupLine("[green]--- конец файла ---[/]\n");
        }

        static void CommandDecrypt(string argument)
        {
            if (string.IsNullOrEmpty(argument))
            {
                AnsiConsole.MarkupLine("[red]Использование: decrypt <файл> <ключ>[/]");
                return;
            }

            string[] parts = argument.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
            {
                AnsiConsole.MarkupLine("[red]Укажи файл и ключ. Пример: decrypt secret.txt 3[/]");
                return;
            }

            string fileName = parts[0];
            string keyStr = parts[1];

            if (!int.TryParse(keyStr, out int key))
            {
                AnsiConsole.MarkupLine("[red]Ключ должен быть числом. Пример: decrypt secret.txt 3[/]");
                return;
            }

            var dir = _state!.CurrentDirectory;

            if (!dir.Files.TryGetValue(fileName, out var file))
            {
                AnsiConsole.MarkupLine($"[red]Файл '{fileName}' не найден.[/]");
                return;
            }

            if (!file.IsEncrypted)
            {
                AnsiConsole.MarkupLine($"[yellow]Файл '{fileName}' не зашифрован.[/]");
                return;
            }

            if (key != file.CipherShift)
            {
                AnsiConsole.MarkupLine("[red]Неверный ключ. Попробуй другое число.[/]");
                return;
            }

            string decrypted = CaesarCipher.Decrypt(file.Content, key);
            file.Decrypt(decrypted);

            TypePrint("\nРасшифровка", 40);
            Thread.Sleep(200);
            Console.Write(".");
            Thread.Sleep(200);
            Console.Write(".");
            Thread.Sleep(200);
            Console.WriteLine(".");
            Thread.Sleep(400);
            AnsiConsole.MarkupLine("[green]Файл успешно расшифрован![/]");
            AnsiConsole.MarkupLine($"\n[green]--- {fileName} ---[/]");
            Console.WriteLine(decrypted);
            AnsiConsole.MarkupLine("[green]--- конец файла ---[/]\n");

            // Запоминаем расшифрованный файл и проверяем уровень
            _state!.DecryptedFiles.Add(fileName);
            _state.Score += 50;
            AnsiConsole.MarkupLine("[green]+50 очков за расшифровку![/]");
            LevelManager.CheckLevelUp(_state);
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
    }
}
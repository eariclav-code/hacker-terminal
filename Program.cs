using System;
using System.IO;
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

            // Загружаем прогресс если есть
            if (File.Exists("save.json"))
            {
                SaveSystem.Load(_state);
                AnsiConsole.MarkupLine("[green]Загружен сохранённый прогресс.[/]");
                AnsiConsole.MarkupLine($"[green]Уровень: {_state.Level}, Очки: {_state.Score}[/]\n");
                RestoreFileSystemState(_state, root);
            }

            RunCommandLoop();
        }

        // Восстанавливает состояние ФС после загрузки сохранения
        static void RestoreFileSystemState(GameState state, VirtualDirectory root)
        {
            // Если взлом был выполнен — открываем скрытую папку
            if (state.FoundKeys.Contains("shadow42"))
            {
                if (root.Subdirectories.TryGetValue("system", out var systemDir))
                {
                    if (systemDir.Subdirectories.TryGetValue("secret", out var secretDir))
                    {
                        secretDir.Reveal();
                    }
                }
            }

            // Если файл был расшифрован — расшифровываем его снова
            if (state.DecryptedFiles.Contains("secret.txt"))
            {
                if (root.Subdirectories.TryGetValue("home", out var homeDir))
                {
                    if (homeDir.Files.TryGetValue("secret.txt", out var file) && file.IsEncrypted)
                    {
                        string decrypted = CaesarCipher.Decrypt(file.Content, file.CipherShift);
                        file.Decrypt(decrypted);
                    }
                }
            }
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
                    SaveSystem.Save(_state!);
                    AnsiConsole.MarkupLine("[green]Прогресс сохранён.[/]");
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
                    CommandHack(argument);
                    break;

                case "scan":
                    CommandScan();
                    break;

                case "status":
                    CommandStatus();
                    break;

                case "connect":
                    CommandConnect(argument);
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
            AnsiConsole.MarkupLine("[green]  decrypt <файл> <ключ>[/] — расшифровать файл");
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
            Terminal.Dots(3, 200);
            Thread.Sleep(400);
            AnsiConsole.MarkupLine("[green]Файл успешно расшифрован![/]");
            AnsiConsole.MarkupLine($"\n[green]--- {fileName} ---[/]");
            Console.WriteLine(decrypted);
            AnsiConsole.MarkupLine("[green]--- конец файла ---[/]\n");

            _state!.DecryptedFiles.Add(fileName);
            _state.Score += 50;
            AnsiConsole.MarkupLine("[green]+50 очков за расшифровку![/]");
            LevelManager.CheckLevelUp(_state);

            // Автосохранение после важного действия
            SaveSystem.Save(_state);
        }

        static void CommandHack(string argument)
        {
            if (string.IsNullOrEmpty(argument))
            {
                AnsiConsole.MarkupLine("[red]Укажи цель. Пример: hack mainframe[/]");
                return;
            }

            if (_state!.Level < 1)
            {
                AnsiConsole.MarkupLine("[red]Недостаточно доступа. Сначала найди пароль в системе.[/]");
                return;
            }

            TypePrint($"\nЗапуск взлома цели: {argument}", 25);
            Thread.Sleep(300);
            Console.WriteLine();
            TypePrint("Подбор учётных данных", 20);
            Terminal.Dots(3, 300);
            Thread.Sleep(400);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Введи пароль для взлома: ");
            Console.ResetColor();
            string? password = Console.ReadLine()?.Trim();

            if (password == "shadow42")
            {
                Thread.Sleep(300);
                AnsiConsole.MarkupLine("\n[green]Пароль принят! Доступ получен.[/]");

                if (_state.RootDirectory.Subdirectories.TryGetValue("system", out var systemDir))
                {
                    if (systemDir.Subdirectories.TryGetValue("secret", out var secretDir))
                    {
                        secretDir.Reveal();
                        AnsiConsole.MarkupLine("[green]Обнаружена скрытая директория: /system/secret[/]");
                    }
                }

                _state.FoundKeys.Add("shadow42");
                _state.Score += 150;
                AnsiConsole.MarkupLine("[green]+150 очков за взлом![/]\n");
                LevelManager.CheckLevelUp(_state);

                // Автосохранение
                SaveSystem.Save(_state);
            }
            else
            {
                AnsiConsole.MarkupLine("\n[red]Неверный пароль. Взлом не удался.[/]\n");
            }
        }

        static void CommandScan()
        {
            TypePrint("\nСканирование системы", 20);
            Terminal.Dots(3, 300);
            Console.WriteLine();
            Thread.Sleep(400);

            AnsiConsole.MarkupLine("[green]Результаты сканирования:[/]");

            if (_state!.Level == 0)
            {
                AnsiConsole.MarkupLine("[green]  > Обнаружен зашифрованный файл в /home[/]");
                AnsiConsole.MarkupLine("[green]  > Подсказка: прочитай hint.txt в корне[/]");
            }
            else if (_state.Level == 1)
            {
                AnsiConsole.MarkupLine("[green]  > Пароль найден. Используй 'hack mainframe'[/]");
                AnsiConsole.MarkupLine("[green]  > Цель: mainframe[/]");
            }
            else if (_state.Level >= 2)
            {
                AnsiConsole.MarkupLine("[green]  > Обнаружена скрытая директория: /system/secret[/]");
                AnsiConsole.MarkupLine("[green]  > Финальный файл ждёт тебя там[/]");
            }

            Console.WriteLine();
        }

        static void CommandStatus()
        {
            AnsiConsole.MarkupLine("\n[green]===== СТАТУС ИГРОКА =====[/]");
            AnsiConsole.MarkupLine($"[green]  Уровень:  {_state!.Level}[/]");
            AnsiConsole.MarkupLine($"[green]  Очки:     {_state.Score}[/]");
            AnsiConsole.MarkupLine($"[green]  Локация:  {_state.GetCurrentPath()}[/]");

            if (_state.DecryptedFiles.Count > 0)
                AnsiConsole.MarkupLine($"[green]  Расшифровано файлов: {_state.DecryptedFiles.Count}[/]");

            if (_state.FoundKeys.Count > 0)
                AnsiConsole.MarkupLine($"[green]  Найдено паролей: {_state.FoundKeys.Count}[/]");

            if (_state.GameCompleted)
                AnsiConsole.MarkupLine("[green]  Статус: АРХИВ NORTECH РАЗОБЛАЧЁН[/]");

            AnsiConsole.MarkupLine("[green]=========================[/]\n");
        }

        static void CommandConnect(string argument)
        {
            if (string.IsNullOrEmpty(argument))
            {
                AnsiConsole.MarkupLine("[red]Укажи узел. Пример: connect nortech-core[/]");
                return;
            }

            string target = argument.Trim().ToLower();

            if (target != "nortech-core")
            {
                AnsiConsole.MarkupLine($"[red]Узел '{argument}' не найден. Проверь network.txt.[/]");
                return;
            }

            if (_state!.Level < 2)
            {
                AnsiConsole.MarkupLine("[red]Недостаточно доступа для подключения к этому узлу.[/]");
                return;
            }

            if (_state.GameCompleted)
            {
                AnsiConsole.MarkupLine("[yellow]Узел уже взломан. Архив Nortech полностью открыт.[/]");
                return;
            }

            TypePrint($"\nУстановка соединения с узлом: {target}", 25);
            Thread.Sleep(300);
            Console.WriteLine();
            TypePrint("Проверка учётных данных", 20);
            Terminal.Dots(3, 300);
            Thread.Sleep(400);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Введи код доступа администратора: ");
            Console.ResetColor();
            string? code = Console.ReadLine()?.Trim();

            if (code == "admin1234")
            {
                Thread.Sleep(300);
                AnsiConsole.MarkupLine("\n[green]Код принят. Соединение установлено.[/]");

                _state.FoundKeys.Add("admin1234");
                _state.Score += 300;
                LevelManager.CheckLevelUp(_state);

                SaveSystem.Save(_state);

                ShowEndingScene();
            }
            else
            {
                AnsiConsole.MarkupLine("\n[red]Код доступа отклонён. Соединение разорвано.[/]\n");
            }
        }

        static void ShowEndingScene()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            string banner = FiggleFonts.Standard.Render("EXPOSED");
            Console.WriteLine(banner);
            Console.ResetColor();

            Terminal.TypeLine(
                "Сервер отдаёт архив: платёжные ведомости, переписка, отчёты об" +
                " утечках токсичных отходов на заводе в Северном секторе...", 15);
            Thread.Sleep(400);

            Terminal.TypeLine(
                "Ты копируешь всё и отправляешь анонимную посылку в редакцию" +
                " \"Открытый код\".", 15);
            Thread.Sleep(500);

            Console.WriteLine();
            Terminal.TypeLine(
                "[ВНИМАНИЕ] Обнаружено ещё одно активное подключение к узлу.",
                20, ConsoleColor.Red);
            Thread.Sleep(400);

            Terminal.TypeLine(
                "Кто-то ещё был внутри вместе с тобой. Соединение разорвано" +
                " принудительно.", 15);

            Console.WriteLine();
            AnsiConsole.MarkupLine("[green]=================================================[/]");
            AnsiConsole.MarkupLine("[green]  NORTECH ЧАСТИЧНО РАЗОБЛАЧЁН. ИГРА ОКОНЧЕНА... ПОКА ЧТО.[/]");
            AnsiConsole.MarkupLine("[green]=================================================[/]\n");
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
            Terminal.Type(text, delayMs);
        }
    }
}
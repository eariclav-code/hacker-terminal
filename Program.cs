using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Figgle;
using Figgle.Fonts;
using Spectre.Console;
using HackerTerminal.Quests;

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

            try
            {
                QuestLoader.LoadAll();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Ошибка загрузки квестов: {ex.Message}[/]");
                return;
            }

            _state = new GameState();

            bool loaded = SaveSystem.Load(_state);

            if (loaded)
            {
                RestoreFileSystemState(_state);
                AnsiConsole.MarkupLine("[green]Загружен сохранённый прогресс.[/]");
                AnsiConsole.MarkupLine(
                    $"[green]Узел: {_state.CurrentNodeId}, Уровень: {_state.Level}, Очки: {_state.Score}[/]\n");
            }
            else
            {
                _state.CurrentNodeId = QuestLoader.StartNodeId;
                EnsureNodeFileSystem(_state.CurrentNodeId);
                _state.CurrentDirectory = _state.CurrentNodeRoot;
                _state.KnownNodes.Add(_state.CurrentNodeId);
                _state.VisitedNodes.Add(_state.CurrentNodeId);
            }

            RunCommandLoop();
        }

        // Гарантирует, что виртуальная ФС для узла построена по его квест-файлу.
        static void EnsureNodeFileSystem(string nodeId)
        {
            if (_state!.NodeFileSystems.ContainsKey(nodeId))
                return;

            var nodeData = QuestLoader.GetNode(nodeId);
            var fs = NodeFileSystemBuilder.Build(nodeData);
            _state.NodeFileSystems[nodeId] = fs;
        }

        // Восстанавливает состояние ФС всех посещённых узлов после загрузки сохранения:
        // открывает скрытые директории для уже взломанных целей и расшифровывает
        // файлы, которые игрок расшифровал в прошлой сессии.
        static void RestoreFileSystemState(GameState state)
        {
            var nodesToRestore = new HashSet<string>(state.VisitedNodes) { state.CurrentNodeId };

            foreach (var nodeId in nodesToRestore)
            {
                if (!QuestLoader.Nodes.ContainsKey(nodeId))
                    continue;

                EnsureNodeFileSystem(nodeId);
                var nodeData = QuestLoader.GetNode(nodeId);
                var fs = state.NodeFileSystems[nodeId];

                if (nodeData.Hack != null
                    && !string.IsNullOrEmpty(nodeData.Hack.GrantsKey)
                    && state.FoundKeys.Contains(nodeData.Hack.GrantsKey)
                    && !string.IsNullOrEmpty(nodeData.Hack.RevealsDirectory))
                {
                    FindDirectoryByPath(fs, nodeData.Hack.RevealsDirectory)?.Reveal();
                }

                RestoreDecryptedFiles(nodeId, fs, state);
            }

            state.CurrentDirectory = state.NodeFileSystems[state.CurrentNodeId];
        }

        static void RestoreDecryptedFiles(string nodeId, VirtualDirectory root, GameState state)
        {
            void Walk(VirtualDirectory dir, string path)
            {
                foreach (var file in dir.Files.Values)
                {
                    string qualifier = $"{nodeId}:{(path.Length == 0 ? file.Name : path + "/" + file.Name)}";

                    if (file.IsEncrypted && state.DecryptedFiles.Contains(qualifier))
                    {
                        string plain = CaesarCipher.Decrypt(file.Content, file.CipherShift);
                        file.Decrypt(plain);
                    }
                }

                foreach (var sub in dir.Subdirectories.Values)
                {
                    Walk(sub, path.Length == 0 ? sub.Name : path + "/" + sub.Name);
                }
            }

            Walk(root, "");
        }

        static void RunCommandLoop()
        {
            AnsiConsole.MarkupLine("[green]Введи 'help' для списка команд.[/]\n");

            while (true)
            {
                string prompt = _state!.GetPrompt();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"root@hacker:{prompt}> ");
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
            AnsiConsole.MarkupLine("[green]  hack <цель>[/]       — взломать цель (мини-игра Mastermind)");
            AnsiConsole.MarkupLine("[green]  scan[/]              — сканировать узел, искать новые подключения");
            AnsiConsole.MarkupLine("[green]  connect <узел>[/]    — подключиться к другому узлу сети");
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
                if (_state!.CurrentDirectory == _state.CurrentNodeRoot)
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

            if (!string.IsNullOrEmpty(file.GrantsKeyOnRead) && _state.FoundKeys.Add(file.GrantsKeyOnRead))
            {
                AnsiConsole.MarkupLine("[yellow]Информация зафиксирована. Возможно, пригодится позже.[/]\n");
                SaveSystem.Save(_state);
            }
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

            string qualifier = BuildFileQualifier(fileName);
            _state!.DecryptedFiles.Add(qualifier);
            _state.Score += 50;
            AnsiConsole.MarkupLine("[green]+50 очков за расшифровку![/]");

            if (!string.IsNullOrEmpty(file.GrantsKeyOnDecrypt) && _state.FoundKeys.Add(file.GrantsKeyOnDecrypt))
            {
                AnsiConsole.MarkupLine($"[green]Обнаружен ключ доступа: {file.GrantsKeyOnDecrypt}[/]");
            }

            LevelManager.CheckLevelUp(_state);

            SaveSystem.Save(_state);
        }

        static string BuildFileQualifier(string fileName)
        {
            string path = _state!.GetCurrentPath();
            string rel = path == "/" ? fileName : $"{path.TrimStart('/')}/{fileName}";
            return $"{_state.CurrentNodeId}:{rel}";
        }

        static void CommandHack(string argument)
        {
            if (_state!.GameCompleted)
            {
                AnsiConsole.MarkupLine("[yellow]Игра уже завершена. Удали save.json, чтобы начать заново.[/]");
                return;
            }

            if (string.IsNullOrEmpty(argument))
            {
                AnsiConsole.MarkupLine("[red]Укажи цель. Пример: hack mainframe[/]");
                return;
            }

            var node = QuestLoader.GetNode(_state.CurrentNodeId);
            var hackTarget = node.Hack;

            if (hackTarget == null || !string.Equals(hackTarget.TargetName, argument, StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine($"[red]Цель '{argument}' не найдена на этом узле.[/]");
                return;
            }

            bool alreadyHacked = !string.IsNullOrEmpty(hackTarget.GrantsKey)
                && _state.FoundKeys.Contains(hackTarget.GrantsKey);

            if (alreadyHacked)
            {
                AnsiConsole.MarkupLine("[yellow]Эта цель уже взломана.[/]");
                return;
            }

            if (!string.IsNullOrEmpty(hackTarget.RequiredKey) && !_state.FoundKeys.Contains(hackTarget.RequiredKey))
            {
                AnsiConsole.MarkupLine("[red]Недостаточно доступа. Сначала найди пароль в системе.[/]");
                return;
            }

            // Развилка "взятка" — доступна только на финальном узле,
            // если игрок уже прочитал сообщение с предложением сделки.
            if (node.IsEnding && _state.FoundKeys.Contains("bribe_offer"))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("\nТебе предлагают сделку взамен на отказ от взлома. Принять? (yes/no): ");
                Console.ResetColor();
                string? decision = Console.ReadLine()?.Trim().ToLower();

                if (decision == "yes" || decision == "да")
                {
                    TriggerEnding(node, "bribe");
                    return;
                }

                AnsiConsole.MarkupLine("[green]Ты отклоняешь предложение и продолжаешь взлом.[/]\n");
            }

            TypePrint($"\nЗапуск взлома цели: {argument}", 25);
            Thread.Sleep(300);
            Console.WriteLine();

            var outcome = Mastermind.Play(hackTarget.Code, hackTarget.MaxAttempts, hackTarget.PursuitSeconds);

            switch (outcome.Result)
            {
                case MastermindResult.Success:
                    HandleHackSuccess(node, hackTarget, outcome);
                    break;

                case MastermindResult.OutOfAttempts:
                    HandleHackFailure(node, "Превышено число попыток. Система блокирует канал доступа.");
                    break;

                case MastermindResult.Traced:
                    HandleHackFailure(node, "Таймер слежки истёк — служба безопасности обнаружила подключение.");
                    break;
            }
        }

        static void HandleHackSuccess(NetworkNodeData node, HackTargetData hackTarget, MastermindOutcome outcome)
        {
            AnsiConsole.MarkupLine("\n[green]Код подобран! Доступ получен.[/]");

            if (!string.IsNullOrEmpty(hackTarget.RevealsDirectory))
            {
                var dir = FindDirectoryByPath(_state!.CurrentNodeRoot, hackTarget.RevealsDirectory);
                if (dir != null)
                {
                    dir.Reveal();
                    AnsiConsole.MarkupLine($"[green]Обнаружена скрытая директория: /{hackTarget.RevealsDirectory}[/]");
                }
            }

            if (!string.IsNullOrEmpty(hackTarget.GrantsKey))
                _state!.FoundKeys.Add(hackTarget.GrantsKey);

            int bonus = ScoreBonusForSpeed(outcome, hackTarget);
            int totalScore = 150 + bonus;
            _state!.Score += totalScore;
            AnsiConsole.MarkupLine($"[green]+{totalScore} очков за взлом![/]\n");

            LevelManager.CheckLevelUp(_state);

            if (node.IsEnding)
            {
                string condition = DetermineEndingCondition(outcome, hackTarget);
                TriggerEnding(node, condition);
                return;
            }

            SaveSystem.Save(_state);
        }

        static void HandleHackFailure(NetworkNodeData node, string message)
        {
            AnsiConsole.MarkupLine($"\n[red]{message}[/]");

            _state!.Score = Math.Max(0, _state.Score - 30);
            AnsiConsole.MarkupLine("[red]-30 очков за провал попытки взлома.[/]\n");

            if (node.IsEnding)
            {
                TriggerEnding(node, "traced");
                return;
            }

            AnsiConsole.MarkupLine("[yellow]Можно попробовать снова.[/]\n");
            SaveSystem.Save(_state);
        }

        static int ScoreBonusForSpeed(MastermindOutcome outcome, HackTargetData hackTarget)
        {
            bool fastAttempts = outcome.AttemptsUsed <= hackTarget.MaxAttempts * 0.6;
            bool fastTime = outcome.SecondsUsed <= hackTarget.PursuitSeconds * 0.6;

            if (fastAttempts && fastTime)
                return 100;

            if (fastAttempts || fastTime)
                return 40;

            return 0;
        }

        static string DetermineEndingCondition(MastermindOutcome outcome, HackTargetData hackTarget)
        {
            bool fastAttempts = outcome.AttemptsUsed <= hackTarget.MaxAttempts * 0.6;
            bool fastTime = outcome.SecondsUsed <= hackTarget.PursuitSeconds * 0.6;

            return (fastAttempts && fastTime) ? "clean" : "partial";
        }

        static void TriggerEnding(NetworkNodeData node, string conditionId)
        {
            EndingOptionData? ending = node.Endings.Find(e => e.Condition == conditionId);
            ending ??= node.Endings.Find(e => e.Condition == "partial");
            ending ??= (node.Endings.Count > 0 ? node.Endings[0] : null);

            _state!.GameCompleted = true;
            _state.EndingAchieved = ending?.Id ?? conditionId;

            SaveSystem.Save(_state);

            if (ending != null)
                ShowEndingScene(ending);
        }

        static void ShowEndingScene(EndingOptionData ending)
        {
            bool isBad = ending.Condition == "traced";

            Console.WriteLine();
            Console.ForegroundColor = isBad ? ConsoleColor.Red : ConsoleColor.Green;
            string bannerText = isBad ? "CAUGHT" : "EXPOSED";
            string banner = FiggleFonts.Standard.Render(bannerText);
            Console.WriteLine(banner);
            Console.ResetColor();

            Terminal.TypeLine(ending.Text, 15, isBad ? ConsoleColor.Red : Terminal.DefaultColor);

            Console.WriteLine();
            AnsiConsole.MarkupLine("[green]=================================================[/]");
            AnsiConsole.MarkupLine($"[green]  ФИНАЛ: {ending.Title.ToUpperInvariant()}[/]");
            AnsiConsole.MarkupLine("[green]=================================================[/]\n");
        }

        static VirtualDirectory? FindDirectoryByPath(VirtualDirectory root, string path)
        {
            var current = root;

            foreach (var seg in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!current.Subdirectories.TryGetValue(seg, out var next))
                    return null;

                current = next;
            }

            return current;
        }

        static void CommandScan()
        {
            var node = QuestLoader.GetNode(_state!.CurrentNodeId);

            TypePrint("\nСканирование системы", 20);
            Terminal.Dots(3, 300);
            Console.WriteLine();
            Thread.Sleep(400);

            AnsiConsole.MarkupLine("[green]Результаты сканирования:[/]");

            foreach (var hint in node.ScanHints)
            {
                AnsiConsole.MarkupLine($"[green]  > {hint}[/]");
            }

            bool hacked = node.Hack != null
                && !string.IsNullOrEmpty(node.Hack.GrantsKey)
                && _state.FoundKeys.Contains(node.Hack.GrantsKey);

            if (hacked)
            {
                foreach (var connId in node.Connections)
                {
                    if (_state.KnownNodes.Add(connId))
                    {
                        var connNode = QuestLoader.GetNode(connId);
                        AnsiConsole.MarkupLine($"[blue]  > Обнаружен новый узел сети: {connId} ({connNode.DisplayName})[/]");
                    }
                }
            }
            else if (node.Hack != null)
            {
                AnsiConsole.MarkupLine(
                    $"[yellow]  > Обнаружена защищённая цель: {node.Hack.TargetName}. Используй 'hack {node.Hack.TargetName}'[/]");
            }

            if (_state.GameCompleted)
            {
                AnsiConsole.MarkupLine("[green]  > Все известные системы Nortech скомпрометированы.[/]");
            }

            Console.WriteLine();
            SaveSystem.Save(_state);
        }

        static void CommandStatus()
        {
            var node = QuestLoader.GetNode(_state!.CurrentNodeId);

            AnsiConsole.MarkupLine("\n[green]===== СТАТУС ИГРОКА =====[/]");
            AnsiConsole.MarkupLine($"[green]  Уровень:  {_state.Level}[/]");
            AnsiConsole.MarkupLine($"[green]  Очки:     {_state.Score}[/]");
            AnsiConsole.MarkupLine($"[green]  Узел:     {_state.CurrentNodeId} ({node.DisplayName})[/]");
            AnsiConsole.MarkupLine($"[green]  Локация:  {_state.GetCurrentPath()}[/]");

            if (_state.DecryptedFiles.Count > 0)
                AnsiConsole.MarkupLine($"[green]  Расшифровано файлов: {_state.DecryptedFiles.Count}[/]");

            if (_state.FoundKeys.Count > 0)
                AnsiConsole.MarkupLine($"[green]  Найдено ключей доступа: {_state.FoundKeys.Count}[/]");

            if (_state.KnownNodes.Count > 0)
                AnsiConsole.MarkupLine($"[green]  Известно узлов сети: {_state.KnownNodes.Count}[/]");

            if (_state.GameCompleted)
            {
                string endingLabel = _state.EndingAchieved ?? "неизвестно";
                AnsiConsole.MarkupLine($"[green]  Статус: ИГРА ЗАВЕРШЕНА — концовка: {endingLabel}[/]");
            }

            AnsiConsole.MarkupLine("[green]=========================[/]\n");
        }

        static void CommandConnect(string argument)
        {
            if (_state!.GameCompleted)
            {
                AnsiConsole.MarkupLine("[yellow]Игра уже завершена. Удали save.json, чтобы начать заново.[/]");
                return;
            }

            if (string.IsNullOrEmpty(argument))
            {
                AnsiConsole.MarkupLine("[red]Укажи узел. Пример: connect nortech-core[/]");
                return;
            }

            string targetId = argument.Trim().ToLower();

            if (!QuestLoader.Nodes.ContainsKey(targetId))
            {
                AnsiConsole.MarkupLine($"[red]Узел '{argument}' не найден. Проверь network.txt.[/]");
                return;
            }

            if (targetId == _state.CurrentNodeId)
            {
                AnsiConsole.MarkupLine("[yellow]Ты уже подключён к этому узлу.[/]");
                return;
            }

            if (!_state.KnownNodes.Contains(targetId))
            {
                AnsiConsole.MarkupLine($"[red]Узел '{argument}' неизвестен. Попробуй 'scan', чтобы обнаружить доступные узлы.[/]");
                return;
            }

            var targetNode = QuestLoader.GetNode(targetId);

            if (_state.Level < targetNode.RequiredLevel)
            {
                AnsiConsole.MarkupLine("[red]Недостаточно доступа для подключения к этому узлу.[/]");
                return;
            }

            if (!string.IsNullOrEmpty(targetNode.RequiredKeyToConnect)
                && !_state.FoundKeys.Contains(targetNode.RequiredKeyToConnect))
            {
                AnsiConsole.MarkupLine("[red]Нужен код доступа для этого узла. Поищи его в системе.[/]");
                return;
            }

            TypePrint($"\nУстановка соединения с узлом: {targetId}", 25);
            Thread.Sleep(300);
            Console.WriteLine();
            TypePrint("Проверка учётных данных", 20);
            Terminal.Dots(3, 300);
            Thread.Sleep(400);
            AnsiConsole.MarkupLine("\n[green]Соединение установлено.[/]");

            EnsureNodeFileSystem(targetId);
            _state.CurrentNodeId = targetId;
            _state.CurrentDirectory = _state.CurrentNodeRoot;
            _state.VisitedNodes.Add(targetId);

            AnsiConsole.MarkupLine($"[green]Ты теперь на узле: {targetNode.DisplayName}[/]\n");

            SaveSystem.Save(_state);
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

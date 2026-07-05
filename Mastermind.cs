using System;
using System.Diagnostics;
using Spectre.Console;

namespace HackerTerminal
{
    internal enum MastermindResult
    {
        Success,
        OutOfAttempts,
        Traced
    }

    // Итог попытки взлома: как именно она завершилась и с каким запасом
    // по попыткам/времени — от этого зависит бонус очков и качество концовки.
    internal class MastermindOutcome
    {
        public MastermindResult Result { get; set; }
        public int AttemptsUsed { get; set; }
        public double SecondsUsed { get; set; }
        public int MaxAttempts { get; set; }
        public int PursuitSeconds { get; set; }
    }

    // Мини-игра Mastermind: игрок подбирает секретный код из цифр 1-6.
    // После каждой попытки выводится обратная связь:
    //   'x' — цифра и позиция угаданы точно
    //   'o' — цифра есть в коде, но на другой позиции
    // Ограничена числом попыток И реальным таймером слежки службы
    // безопасности — если он истекает раньше, чем код подобран,
    // взлом считается проваленным независимо от оставшихся попыток.
    internal static class Mastermind
    {
        public static MastermindOutcome Play(string secretCode, int maxAttempts, int pursuitSeconds)
        {
            var outcome = new MastermindOutcome
            {
                MaxAttempts = maxAttempts,
                PursuitSeconds = pursuitSeconds
            };

            int codeLength = secretCode.Length;

            AnsiConsole.MarkupLine($"\n[green]=== ВЗЛОМ: подбор кода доступа ({codeLength} цифры, от 1 до 6) ===[/]");
            AnsiConsole.MarkupLine("[green]После каждой попытки: 'x' — цифра и позиция верны, 'o' — цифра есть, позиция неверна.[/]");
            AnsiConsole.MarkupLine($"[yellow]Внимание: служба безопасности засекает канал. Таймер слежки: {pursuitSeconds} сек.[/]\n");

            var stopwatch = Stopwatch.StartNew();
            int attempt = 0;

            while (attempt < maxAttempts)
            {
                if (stopwatch.Elapsed.TotalSeconds >= pursuitSeconds)
                {
                    outcome.Result = MastermindResult.Traced;
                    outcome.AttemptsUsed = attempt;
                    outcome.SecondsUsed = stopwatch.Elapsed.TotalSeconds;
                    return outcome;
                }

                double remaining = pursuitSeconds - stopwatch.Elapsed.TotalSeconds;
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write($"[слежка ~{remaining:0}с] ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"Попытка {attempt + 1}/{maxAttempts}, код ({codeLength} цифр, 1-6): ");
                Console.ResetColor();

                string? guess = Console.ReadLine()?.Trim();

                if (stopwatch.Elapsed.TotalSeconds >= pursuitSeconds)
                {
                    outcome.Result = MastermindResult.Traced;
                    outcome.AttemptsUsed = attempt + 1;
                    outcome.SecondsUsed = stopwatch.Elapsed.TotalSeconds;
                    return outcome;
                }

                if (string.IsNullOrEmpty(guess) || guess.Length != codeLength || !IsAllDigitsInRange(guess))
                {
                    AnsiConsole.MarkupLine($"[red]Некорректный ввод. Нужно ровно {codeLength} цифр от 1 до 6.[/]\n");
                    continue; // ошибка формата не тратит попытку
                }

                attempt++;

                if (guess == secretCode)
                {
                    outcome.Result = MastermindResult.Success;
                    outcome.AttemptsUsed = attempt;
                    outcome.SecondsUsed = stopwatch.Elapsed.TotalSeconds;
                    return outcome;
                }

                var (exact, partial) = Score(secretCode, guess);
                string feedback = new string('x', exact) + new string('o', partial);
                if (feedback.Length == 0) feedback = "----";
                AnsiConsole.MarkupLine($"[blue]  Результат: {feedback}[/] [grey](точных: {exact}, частичных: {partial})[/]\n");
            }

            outcome.Result = MastermindResult.OutOfAttempts;
            outcome.AttemptsUsed = attempt;
            outcome.SecondsUsed = stopwatch.Elapsed.TotalSeconds;
            return outcome;
        }

        private static bool IsAllDigitsInRange(string s)
        {
            foreach (char c in s)
            {
                if (c < '1' || c > '6')
                    return false;
            }
            return true;
        }

        // Классический подсчёт Mastermind без двойного учёта одной и той же цифры.
        private static (int exact, int partial) Score(string secret, string guess)
        {
            int len = secret.Length;
            bool[] secretUsed = new bool[len];
            bool[] guessUsed = new bool[len];
            int exact = 0;

            for (int i = 0; i < len; i++)
            {
                if (secret[i] == guess[i])
                {
                    exact++;
                    secretUsed[i] = true;
                    guessUsed[i] = true;
                }
            }

            int partial = 0;
            for (int i = 0; i < len; i++)
            {
                if (guessUsed[i]) continue;

                for (int j = 0; j < len; j++)
                {
                    if (secretUsed[j]) continue;

                    if (guess[i] == secret[j])
                    {
                        partial++;
                        secretUsed[j] = true;
                        break;
                    }
                }
            }

            return (exact, partial);
        }
    }
}

using System;
using Spectre.Console;

namespace HackerTerminal
{
    // Управляет уровнями игры. Проверяет условия перехода на следующий
    // уровень по ключам доступа, найденным игроком (не привязано к
    // конкретным строковым командам — работает с любыми узлами квестов).
    internal static class LevelManager
    {
        public static void CheckLevelUp(GameState state)
        {
            switch (state.Level)
            {
                case 0:
                    // Уровень 0 → 1: расшифрован secret.txt на домашнем ПК, найден shadow42.
                    if (state.FoundKeys.Contains("shadow42"))
                    {
                        LevelUp(state, 1,
                            "Ты нашёл первый пароль. Система открывает новый раздел...",
                            100);
                    }
                    break;

                case 1:
                    // Уровень 1 → 2: mainframe взломан, получен код администратора.
                    if (state.FoundKeys.Contains("admin1234"))
                    {
                        LevelUp(state, 2,
                            "Доступ получен. Ты проникаешь глубже в сеть Nortech...",
                            200);
                    }
                    break;

                case 2:
                    // Уровень 2 → 3 (финал): архивное хранилище взломано и слито наружу.
                    if (state.FoundKeys.Contains("archive_leaked"))
                    {
                        LevelUp(state, 3,
                            "Доступ к архиву Nortech получен. Миссия почти завершена...",
                            300);
                    }
                    break;
            }
        }

        private static void LevelUp(GameState state, int newLevel, string message, int scoreReward)
        {
            state.Level = newLevel;
            state.Score += scoreReward;

            Console.WriteLine();
            Terminal.TypeLine($"*** УРОВЕНЬ {newLevel} ***", 15);
            Terminal.TypeLine(message, 15);
            AnsiConsole.MarkupLine($"[green]+{scoreReward} очков! Всего: {state.Score}[/]\n");
        }
    }
}

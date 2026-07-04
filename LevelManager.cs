using Spectre.Console;

namespace HackerTerminal
{
    // Управляет уровнями игры.
    // Проверяет условия перехода на следующий уровень.
    internal static class LevelManager
    {
        // Проверяет, выполнено ли условие текущего уровня,
        // и если да — повышает уровень и выдаёт награду.
        public static void CheckLevelUp(GameState state)
        {
            switch (state.Level)
            {
                case 0:
                    // Уровень 0 → 1: нужно расшифровать secret.txt
                    if (state.DecryptedFiles.Contains("secret.txt"))
                    {
                        LevelUp(state, 1,
                            "Ты нашёл первый пароль. Система открывает новый раздел...",
                            100);
                    }
                    break;

                case 1:
                    // Уровень 1 → 2: нужно взломать узел (hack) с паролем shadow42
                    if (state.FoundKeys.Contains("shadow42"))
                    {
                        LevelUp(state, 2,
                            "Доступ получен. Ты проникаешь глубже в систему...",
                            200);
                    }
                    break;

                case 2:
                    // Уровень 2 → 3 (финал): нужно подключиться к nortech-core
                    // с кодом администратора (connect nortech-core)
                    if (state.FoundKeys.Contains("admin1234"))
                    {
                        LevelUp(state, 3,
                            "Доступ к архиву Nortech получен. Миссия почти завершена...",
                            300);
                        state.GameCompleted = true;
                    }
                    break;
            }
        }

        private static void LevelUp(GameState state, int newLevel, string message, int scoreReward)
        {
            state.Level = newLevel;
            state.Score += scoreReward;

            AnsiConsole.MarkupLine($"\n[green]*** УРОВЕНЬ {newLevel} ***[/]");
            AnsiConsole.MarkupLine($"[green]{message}[/]");
            AnsiConsole.MarkupLine($"[green]+{scoreReward} очков! Всего: {state.Score}[/]\n");
        }
    }
}
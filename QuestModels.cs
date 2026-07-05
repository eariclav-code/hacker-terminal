using System.Collections.Generic;

namespace HackerTerminal.Quests
{
    // Манифест перечисляет узлы сети (файлы квестов) и стартовый узел игры.
    internal class QuestManifest
    {
        public string StartNode { get; set; } = "";
        public List<string> Nodes { get; set; } = new();
    }

    // Описание одного файла внутри узла сети.
    // Content всегда хранится в открытом виде — если Encrypted == true,
    // построитель ФС сам зашифрует его сдвигом CipherShift.
    internal class QuestFileData
    {
        public string Name { get; set; } = "";
        public string Content { get; set; } = "";
        public bool Encrypted { get; set; } = false;
        public int CipherShift { get; set; } = 0;

        // Ключ, который игрок получает сразу после прочтения файла (cat).
        public string GrantsKeyOnRead { get; set; } = "";

        // Ключ, который игрок получает после успешной расшифровки (decrypt).
        public string GrantsKeyOnDecrypt { get; set; } = "";
    }

    // Описание одной папки узла сети (путь относительно корня узла).
    internal class QuestDirectoryData
    {
        // "" — корень узла, "home" — /home, "system/secret" — вложенная папка.
        public string Path { get; set; } = "";
        public bool Hidden { get; set; } = false;
        public List<QuestFileData> Files { get; set; } = new();
    }

    // Параметры взлома цели на узле: сколько нужно знать заранее,
    // секретный код мини-игры Mastermind, лимит попыток и таймер слежки.
    internal class HackTargetData
    {
        public string TargetName { get; set; } = "";
        public string RequiredKey { get; set; } = "";
        public string Code { get; set; } = "";
        public int MaxAttempts { get; set; } = 10;
        public int PursuitSeconds { get; set; } = 90;
        public string RevealsDirectory { get; set; } = "";
        public string GrantsKey { get; set; } = "";
    }

    // Один из возможных вариантов концовки для узла с IsEnding == true.
    internal class EndingOptionData
    {
        public string Id { get; set; } = "";
        public string Condition { get; set; } = "";
        public string Title { get; set; } = "";
        public string Text { get; set; } = "";
    }

    // Узел сети — "машина", к которой можно подключиться командой connect.
    internal class NetworkNodeData
    {
        public string Id { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public int RequiredLevel { get; set; } = 0;
        public string RequiredKeyToConnect { get; set; } = "";
        public List<string> ScanHints { get; set; } = new();
        public List<QuestDirectoryData> Directories { get; set; } = new();
        public HackTargetData? Hack { get; set; }
        public List<string> Connections { get; set; } = new();
        public bool IsEnding { get; set; } = false;
        public List<EndingOptionData> Endings { get; set; } = new();
    }
}

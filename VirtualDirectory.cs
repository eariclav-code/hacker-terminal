using System.Collections.Generic;

namespace HackerTerminal
{
    // Представляет папку в фейковой файловой системе.
    // Содержит вложенные папки, файлы и ссылку на родителя (для команды cd ..).
    internal class VirtualDirectory
    {
        public string Name { get; }
        public VirtualDirectory? Parent { get; }

        public Dictionary<string, VirtualDirectory> Subdirectories { get; } = new();
        public Dictionary<string, VirtualFile> Files { get; } = new();

        // Скрытая папка не показывается в ls, пока её не "открыли" (например, после hack).
        public bool IsHidden { get; private set; }

        public VirtualDirectory(string name, VirtualDirectory? parent = null, bool isHidden = false)
        {
            Name = name;
            Parent = parent;
            IsHidden = isHidden;
        }

        public void AddSubdirectory(VirtualDirectory directory)
        {
            Subdirectories[directory.Name] = directory;
        }

        public void AddFile(VirtualFile file)
        {
            Files[file.Name] = file;
        }

        // Открывает скрытую папку (например, после успешного hack).
        public void Reveal()
        {
            IsHidden = false;
        }

        // Удобный метод для вывода в ls: только видимые подпапки и все файлы.
        public IEnumerable<VirtualDirectory> VisibleSubdirectories()
        {
            foreach (var dir in Subdirectories.Values)
            {
                if (!dir.IsHidden)
                    yield return dir;
            }
        }
    }
}
using System.Collections.Generic;
using HackerTerminal.Quests;

namespace HackerTerminal
{
    // Строит виртуальную файловую систему одного узла сети по его
    // JSON-описанию (список папок с путями и файлами). Файлы, помеченные
    // encrypted:true, шифруются на лету при построении — в JSON хранится
    // открытый текст, чтобы не приходилось вручную считать шифр Цезаря.
    internal static class NodeFileSystemBuilder
    {
        public static VirtualDirectory Build(NetworkNodeData node)
        {
            var root = new VirtualDirectory("/");
            var lookup = new Dictionary<string, VirtualDirectory> { [""] = root };

            var dirsSorted = new List<QuestDirectoryData>(node.Directories);
            dirsSorted.Sort((a, b) => Depth(a.Path).CompareTo(Depth(b.Path)));

            foreach (var dirData in dirsSorted)
            {
                string path = Normalize(dirData.Path);

                if (path.Length == 0)
                {
                    AddFiles(root, dirData);
                    continue;
                }

                if (lookup.ContainsKey(path))
                {
                    // Папка уже создана как промежуточная — просто дополняем файлами.
                    AddFiles(lookup[path], dirData);
                    continue;
                }

                string parentPath = ParentOf(path);
                string name = NameOf(path);
                var parent = EnsurePath(lookup, parentPath);

                var dir = new VirtualDirectory(name, parent, isHidden: dirData.Hidden);
                AddFiles(dir, dirData);
                parent.AddSubdirectory(dir);
                lookup[path] = dir;
            }

            return root;
        }

        // Гарантирует, что вся цепочка родительских папок для path существует,
        // создавая промежуточные пустые папки, если в JSON они не были описаны явно.
        private static VirtualDirectory EnsurePath(Dictionary<string, VirtualDirectory> lookup, string path)
        {
            if (lookup.TryGetValue(path, out var existing))
                return existing;

            string parentPath = ParentOf(path);
            string name = NameOf(path);
            var parent = EnsurePath(lookup, parentPath);

            var dir = new VirtualDirectory(name, parent);
            parent.AddSubdirectory(dir);
            lookup[path] = dir;
            return dir;
        }

        private static void AddFiles(VirtualDirectory dir, QuestDirectoryData data)
        {
            foreach (var f in data.Files)
            {
                string content = f.Content;
                if (f.Encrypted)
                {
                    content = CaesarCipher.Encrypt(f.Content, f.CipherShift);
                }

                string? grantsRead = string.IsNullOrEmpty(f.GrantsKeyOnRead) ? null : f.GrantsKeyOnRead;
                string? grantsDecrypt = string.IsNullOrEmpty(f.GrantsKeyOnDecrypt) ? null : f.GrantsKeyOnDecrypt;

                dir.AddFile(new VirtualFile(f.Name, content, f.Encrypted, f.CipherShift, grantsRead, grantsDecrypt));
            }
        }

        private static string Normalize(string path) => (path ?? "").Trim('/');

        private static int Depth(string path)
        {
            string p = Normalize(path);
            return p.Length == 0 ? 0 : p.Split('/').Length;
        }

        private static string ParentOf(string path)
        {
            int idx = path.LastIndexOf('/');
            return idx < 0 ? "" : path.Substring(0, idx);
        }

        private static string NameOf(string path)
        {
            int idx = path.LastIndexOf('/');
            return idx < 0 ? path : path.Substring(idx + 1);
        }
    }
}

using LuaInterface;
using System.Text;

namespace LuaObfuscator;

public static class LuaObfuscator
{
    private static Lua luaState = null!;
    private static string originalDir = null!;
    private static double allFiles;
    private static double completedFiles;

    private const string o_54b23f86700cdd0d671bbeaab0542ce5 = """
        function obf()
            local a = [[]];
            a = a .. codeTable;
            return a:gsub('.', function(b) return '\\' .. b:byte() end) or a .. '\"'
        end
        """;

    static void Main(string[] args)
    {
        foreach (var arg in args)
        {
            ObfuscateFiles(arg);
        }
    }

    public static string EncryptCode(string code)
    {
        StringBuilder encryptedCode = new();
        foreach (char c in code)
        {
            int charCode = (int)c;
            charCode += 1;
            encryptedCode.Append($"\"{charCode}\",");
        }
        return encryptedCode.ToString();
    }

    public static void ObfuscateFiles(string directoryPath)
    {
        originalDir = directoryPath;
        var startMS = GetTime();
        CountFiles(directoryPath);

        if (allFiles <= 0)
        {
            Console.WriteLine("There are no files to obfuscate in that directory!");
            TryExit();
            return;
        }

        luaState = new Lua();
        luaState.DoString(o_54b23f86700cdd0d671bbeaab0542ce5);
        FileLoop(directoryPath);
        luaState.Close();
        var newMS = GetTime();

        Console.WriteLine($"Finished obfuscating {allFiles} file(s) in {(newMS - startMS).TotalMilliseconds}ms.");
        TryExit();
    }

    public static IEnumerable<string> EnumerateFileSystemEntries(string directoryPath)
    {
        var files = Directory.GetFiles(directoryPath);
        var directories = Directory.GetDirectories(directoryPath);

        foreach (var file in files)
        {
            yield return file;
        }

        foreach (var directory in directories)
        {
            yield return directory;

            foreach (var entry in EnumerateFileSystemEntries(directory))
            {
                yield return entry;
            }
        }
    }

    private static void CountFiles(string directoryPath)
    {
        foreach (var entry in EnumerateFileSystemEntries(directoryPath))
        {
            if (Directory.Exists(entry))
            {
                CountFiles(entry);
                continue;
            }

            if (IsInvalidFile(entry))
            {
                continue;
            }

            allFiles++;
        }
    }

    private static void FileLoop(string directoryPath)
    {
        foreach (var entry in EnumerateFileSystemEntries(directoryPath))
        {
            if (Directory.Exists(entry))
            {
                FileLoop(entry);
                continue;
            }

            if (IsInvalidFile(entry))
            {
                continue;
            }

            var lines = File.ReadAllLines(entry);
            var newLines = lines.Where(line => line != null && !string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith("--")).ToList();
            var newCode = string.Join("\n", newLines.ToArray());

            luaState["codeTable"] = newCode;

            var obfCode = luaState.GetFunction("obf").Call().First().ToString();

            using (var obfFile = new StreamWriter(entry))
            {
                obfFile.WriteLine("local loading = load or loadstring\nloading(\"" + obfCode + "\")()");
            }

            completedFiles++;
            var folderPath = entry.Replace(originalDir, "");
            if (originalDir.StartsWith("C:\\") || originalDir.StartsWith("C:/"))
            {
                folderPath = folderPath.Substring(1);
            }

            Console.WriteLine($"({Math.Floor(completedFiles / allFiles * 100)}%) Successfully obfuscated {folderPath}");
        }
    }

    private static void TryExit()
    {
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
        Environment.Exit(0);
    }

    private static bool Replace(ref string str, string from, string to)
    {
        var startIdx = str.IndexOf(from);
        if (startIdx == -1)
            return false;

        str = str.Remove(startIdx, from.Length).Insert(startIdx, to);
        return true;
    }

    private static bool EndsWith(string mainStr, string toMatch)
    {
        return mainStr.Length >= toMatch.Length && mainStr.Substring(mainStr.Length - toMatch.Length) == toMatch;
    }

    private static bool IsInvalidFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        return Path.GetExtension(filePath) != ".lua" || fileName.StartsWith("__resource") || fileName.StartsWith("fxmanifest");
    }

    private static DateTime GetTime()
    {
        return DateTime.UtcNow;
    }
}

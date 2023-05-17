using LuaInterface;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using System.Text.RegularExpressions;

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

    private static void CountFiles(string directoryPath)
    {
        foreach (var entry in Directory.EnumerateFileSystemEntries(directoryPath))
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
        foreach (var entry in Directory.EnumerateFileSystemEntries(directoryPath))
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
            var newLines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
            var newCode = string.Join(" ", newLines.ToArray());

            luaState["codeTable"] = newCode;

            var obfCode = luaState.GetFunction("obf").Call().First().ToString();

            using (var obfFile = new StreamWriter(entry))
            {
                obfFile.WriteLine("(load or loadstring)(\"" + obfCode + "\")()");
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

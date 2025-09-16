using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

class Program
{
    // Directories to ignore
    private static readonly HashSet<string> IgnoredDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Migrations",
        "bin",
        "obj"
    };

    // File extensions to count
    private static readonly HashSet<string> IncludedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".cs",
        ".razor"
    };

    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: BlazorLineCounter <directoryPath>");
            return;
        }

        string rootDir = args[0];
        if (!Directory.Exists(rootDir))
        {
            Console.WriteLine($"Error: Directory '{rootDir}' does not exist.");
            return;
        }

        int totalLines = 0;

        foreach (var file in EnumerateCodeFiles(rootDir))
        {
            int fileLines = CountCodeLines(file);
            Console.WriteLine($"{file}: {fileLines}");
            totalLines += fileLines;
        }

        Console.WriteLine("\nNote: Lines counted exclude blank lines, comments, and lines with only braces.");
        Console.WriteLine($"\nTotal files counted: {EnumerateCodeFiles(rootDir).Count():N0}");
        Console.WriteLine($"\nTotal lines of code: {totalLines:N0}");
        Console.WriteLine($"\nEstimated development time (at 3 LOC/hour): {totalLines / 3.0:N0} hours");

    }

    private static IEnumerable<string> EnumerateCodeFiles(string rootDir)
    {
        var stack = new Stack<string>();
        stack.Push(rootDir);

        while (stack.Count > 0)
        {
            string currentDir = stack.Pop();

            // Skip ignored directories
            string dirName = Path.GetFileName(currentDir);
            if (IgnoredDirectories.Contains(dirName))
                continue;

            string[] files = Array.Empty<string>();
            try
            {
                files = Directory.GetFiles(currentDir, "*.*", SearchOption.TopDirectoryOnly)
                                 .Where(f => IncludedExtensions.Contains(Path.GetExtension(f)))
                                 .ToArray();
            }
            catch { }

            foreach (var file in files)
                yield return file;

            string[] subDirs = Array.Empty<string>();
            try
            {
                subDirs = Directory.GetDirectories(currentDir);
            }
            catch { }

            foreach (var subDir in subDirs)
                stack.Push(subDir);
        }
    }

    private static int CountCodeLines(string filePath)
    {
        int count = 0;
        bool inBlockComment = false;

        foreach (string rawLine in File.ReadLines(filePath))
        {
            string line = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (inBlockComment)
            {
                if (line.Contains("*/"))
                {
                    inBlockComment = false;
                    line = line.Substring(line.IndexOf("*/") + 2).Trim();
                    if (string.IsNullOrEmpty(line))
                        continue;
                }
                else
                {
                    continue;
                }
            }

            if (line.StartsWith("/*"))
            {
                inBlockComment = true;
                if (line.EndsWith("*/") && line.Length > 3) // inline /* ... */
                {
                    inBlockComment = false;
                }
                continue;
            }

            if (line.StartsWith("//"))
                continue;

            if (line == "{" || line == "}")
                continue;

            count++;
        }

        return count;
    }
}

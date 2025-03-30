namespace Jaket.IO;

using System.Collections.Generic;
using System.IO;

/// <summary> Set of different tools for working with files. </summary>
public static class Files
{
    /// <summary> Path to the root directory. </summary>
    public static string Root => Path.GetDirectoryName(Plugin.Instance.Location);
    /// <summary> Path to the logs directory. </summary>
    public static string Logs => Path.Combine(Root, "logs");
    /// <summary> Path to the bundles directory. </summary>
    public static string Bundles => Path.Combine(Root, "bundles");
    /// <summary> Path to the sprays directory. </summary>
    public static string Sprays => Path.Combine(Root, "../../sprays");

    /// <summary> Returns the path to the file in the given directory. </summary>
    public static string GetFile(string dir, string file) => Path.Combine(dir, file);

    /// <summary> Returns the name of the file without extension. </summary>
    public static string GetName(string file) => Path.GetFileNameWithoutExtension(file);

    /// <summary> Returns whether the given file exists. </summary>
    public static bool Exists(string file) => File.Exists(file);

    /// <summary> Makes sure that the given directory exists. </summary>
    public static void MakeSureExists(string dir) => Directory.CreateDirectory(dir);

    /// <summary> Iterates all files that match the given patterns in the directory. </summary>
    public static void IterAll(Cons<string> cons, string dir, params string[] patterns) => patterns.Each(p => Directory.EnumerateFiles(dir, p).Each(cons));

    /// <summary> Moves all files that match the given patterns from the source to the destination directory. </summary>
    public static void MoveAll(string source, string destination, params string[] patterns) => IterAll(f =>
    {
        var dest = GetFile(destination, Path.GetFileName(f));

        File.Delete(dest);
        File.Move(f, dest);

    }, source, patterns);

    /// <summary> Returns the size of the given file in bytes. </summary>
    public static long Size(string file) => new FileInfo(file).Length;

    /// <summary> Deletes the given file. </summary>
    public static void Delete(string file) => File.Delete(file);

    /// <summary> Asynchronously appends the lines to the given file. </summary>
    public static void Append(string file, IEnumerable<string> lines) => File.AppendAllLinesAsync(file, lines);

    /// <summary> Synchronously reads all bytes from the given file. </summary>
    public static byte[] ReadBytes(string file) => File.ReadAllBytes(file);

    /// <summary> Synchronously reads all lines from the given file. </summary>
    public static string[] ReadLines(string file) => File.ReadAllLines(file);

    /// <summary> Opens a file stream and creates a binary writer. </summary>
    public static void Write(string file, Cons<BinaryWriter> w)
    {
        using var stream = File.OpenWrite(file);
        w(new BinaryWriter(stream));
    }

    /// <summary> Opens a file stream and creates a binary reader. </summary>
    public static void Read(string file, Cons<BinaryReader> r)
    {
        using var stream = File.OpenRead(file);
        r(new BinaryReader(stream));
    }
}

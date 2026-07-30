namespace Jaket.IO;

using System.Collections.Generic;
using System.IO;
using System.Reflection;

/// <summary> Set of different tools for working with files. </summary>
public static class Files
{
    #region paths

    /// <summary> Path to the root directory. </summary>
    public static string Root => Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
    /// <summary> Path to the slot directory. </summary>
    public static string Slot => Path.GetDirectoryName(GameProgressSaver.SavePath + "/");
    /// <summary> Path to the logs directory. </summary>
    public static string Logs => Join(Root, "logs");
    /// <summary> Path to the bundles directory. </summary>
    public static string Bundles => Join(Root, "bundles");
    /// <summary> Path to the sprays directory. </summary>
    public static string Sprays => Join(Root, "../../sprays");

    /// <summary> Path to the purchases file. </summary>
    public static string Purchases => Join(Slot, "purchases.jaket");
    /// <summary> Path to the progress file. </summary>
    public static string Progress => Join(Slot, "progress.jaket");

    #endregion
    #region files

    /// <summary> Returns the path of the file. </summary>
    public static string Join(string dir, string file) => Path.Combine(dir, file);

    /// <summary> Returns the name of the file. </summary>
    public static string Name(string file) => Path.GetFileNameWithoutExtension(file);

    /// <summary> Returns the size of the file. </summary>
    public static long   Size(string file) => new FileInfo(file).Length;

    /// <summary> Whether the file exists. </summary>
    public static bool Exists(string file) => File.Exists(file);

    /// <summary> Deletes the file. </summary>
    public static void Delete(string file) => File.Delete(file);

    #endregion
    #region directories

    /// <summary> Creates the directory. </summary>
    public static void MakeDir(string dir) => Directory.CreateDirectory(dir);

    /// <summary> Iterates all files that match the patterns. </summary>
    public static void IterAll(string dir, Cons<string> cons, params string[] patterns) => patterns.Each(p => Directory.EnumerateFiles(dir, p).Each(cons));

    /// <summary> Moves all files that match the patterns. </summary>
    public static void MoveAll(string source, string destination, params string[] patterns) => IterAll(source, f =>
    {
        File.Copy(f, Join(destination, Path.GetFileName(f)), true);
        File.Delete(f);
    }, patterns);

    #endregion
    #region io

    /// <summary> Asynchronously writes the lines to the file. </summary>
    public static void Append(string file, IEnumerable<string> lines) => File.AppendAllLinesAsync(file, lines).ContinueWith(_ => Events.InternalFlushFinish());

    /// <summary> Synchronously reads all bytes from the file. </summary>
    public static byte[] ReadBytes(string file) => File.ReadAllBytes(file);

    /// <summary> Synchronously reads all lines from the file. </summary>
    public static string[] ReadLines(string file) => File.ReadAllLines(file);

    /// <summary> Opens the file and creates a binary writer. </summary>
    public static void Write(string file, Cons<BinaryWriter> w)
    {
        using var stream = File.OpenWrite(file);
        w(new BinaryWriter(stream));
    }

    /// <summary> Opens the file and creates a binary reader. </summary>
    public static void Read(string file, Cons<BinaryReader> r)
    {
        using var stream = File.OpenRead(file);
        r(new BinaryReader(stream));
    }

    #endregion
}

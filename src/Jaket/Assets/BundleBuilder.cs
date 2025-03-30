#if UNITY_EDITOR

using System.IO;
using UnityEditor;

/// <summary> Utility for packing the required assets into bundles. </summary>
public class BundleBuilder
{
    /// <summary> Directory to store bundles in. </summary>
    public const string BundleDir = "Assets/Bundles";

    /// <summary> Builds a bundle and saves it in the corresponding directory. </summary>
    [MenuItem("Assets/Build Bundle")]
    public static void BuildBundle()
    {
        // create a directory if it doesn't already exist
        if (!Directory.Exists(BundleDir)) Directory.CreateDirectory(BundleDir);

        // build a bundle for further loading by the mod
        BuildPipeline.BuildAssetBundles(BundleDir, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
    }
}

#endif

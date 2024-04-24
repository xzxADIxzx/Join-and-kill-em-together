#if UNITY_EDITOR

using System.IO;
using UnityEditor;

/// <summary> Class for creating bundle with player doll prefab. </summary>
public class BundleBuilder
{
    /// <summary> Directory where all bundles will be stored. </summary>
    public const string BundleDir = "Assets/Bundles";

    /// <summary> Creates a bundle and saves it in the bundles folder. </summary>
    [MenuItem("Assets/Build Bundles")]
    public static void BuildBundles()
    {
        // create a directory if it doesn't already exist
        if (!Directory.Exists(BundleDir)) Directory.CreateDirectory(BundleDir);

        // build bundles for further loading from RemotePlayer
        BuildPipeline.BuildAssetBundles(BundleDir, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
    }
}

#endif

#if UNITY_EDITOR

using System.IO;
using UnityEditor;

/// <summary> Asset bundles builder. </summary>
public class Builder
{
    [MenuItem("Assets/Build Bundles")]
    public static void Build()
    {
        // directory to store bundles in
        string path = "Assets/Bundles";

        Directory.CreateDirectory(path);
        BuildPipeline.BuildAssetBundles(path, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
    }
}

#endif

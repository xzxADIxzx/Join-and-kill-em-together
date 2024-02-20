namespace Jaket;

using UnityEngine;

/// <summary> Set of different tools for simplifying life and systematization of code. </summary>
public class Tools
{
    /// <summary> System namespace also has Object class, so I added this to avoid conflicts. </summary>
    public static void Destroy(Object obj) => Object.Destroy(obj);
    public static void DestroyImmediate(Object obj) => Object.DestroyImmediate(obj);

    #region resources

    /// <summary> Just shortcut. </summary>
    public static T[] ResFind<T>() where T : Object => Resources.FindObjectsOfTypeAll<T>();

    /// <summary> Iterates all objects of the type that predicate for the criterion. </summary>
    public static void ResFind<T>(System.Predicate<T> pred, System.Action<T> cons) where T : Object
    {
        foreach (var item in ResFind<T>()) if (pred(item)) cons(item);
    }

    #endregion
}

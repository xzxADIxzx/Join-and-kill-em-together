namespace Jaket;

using HarmonyLib;
using System.Reflection;
using UnityEngine;

/// <summary> Set of different tools for simplifying life and systematization of code. </summary>
public class Tools
{
    /// <summary> Name of the current scene. </summary>
    public static string Scene => SceneHelper.CurrentScene;

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

    /// <summary> Just shortcut. </summary>
    public static T ObjFind<T>() where T : Object => Object.FindObjectOfType<T>();
    public static GameObject ObjFind(string name) => GameObject.Find(name);

    #endregion
    #region harmony

    /// <summary> Returns information about the class field. </summary>
    public static FieldInfo Field<T>(string name) => AccessTools.Field(typeof(T), name);
    public static FieldInfo Field(string name, object obj) => AccessTools.Field(obj.GetType(), name);

    /// <summary> Returns information about the class property. </summary>
    public static PropertyInfo Property<T>(string name) => AccessTools.Property(typeof(T), name);
    public static PropertyInfo Property(string name, object obj) => AccessTools.Property(obj.GetType(), name);

    /// <summary> Calls the class method with the given arguments. </summary>
    public static void Invoke<T>(string name, T obj, params object[] args) => AccessTools.Method(typeof(T), name).Invoke(obj, args);

    #endregion
}

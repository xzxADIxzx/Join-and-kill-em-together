namespace Jaket;

using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

/// <summary> Set of different tools for simplifying life and systematization of code. </summary>
public class Tools
{
    #region scene

    /// <summary> Name of the current scene. </summary>
    public static string Scene => SceneHelper.CurrentScene;

    /// <summary> Loads the given scene. </summary>
    public static void Load(string scene) => SceneHelper.LoadScene(scene);

    /// <summary> Whether the given object is on a scene or is it just an asset. </summary>
    public static bool IsReal(GameObject obj) => obj.scene.name != null;
    public static bool IsReal(Component comp) => IsReal(comp.gameObject);

    #endregion
    #region create & destroy

    /// <summary> Creates a new game object and assigns it to the given transform. </summary>
    public static GameObject Create(string name, Transform parent = null)
    {
        GameObject obj = new(name);
        obj.transform.SetParent(parent ?? Plugin.Instance.transform, false);
        return obj;
    }
    public static T Create<T>(string name, Transform parent = null) where T : Component => Create(name, parent).AddComponent<T>();

    /// <summary> System namespace also has Object class, so I added this to avoid conflicts. </summary>
    public static void Destroy(Object obj) => Object.Destroy(obj);
    public static void DestroyImmediate(Object obj) => Object.DestroyImmediate(obj);

    #endregion
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

    /// <summary> Returns the event of pressing the button. </summary>
    public static UnityEvent GetClick(GameObject btn)
    {
        var pointer = btn.GetComponents<MonoBehaviour>()[2]; // so much pain over the private class ControllerPointer
        return Property("OnPressed", pointer).GetValue(pointer) as UnityEvent;
    }

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

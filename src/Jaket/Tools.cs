namespace Jaket;

using HarmonyLib;
using Steamworks;
using Steamworks.Data;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

using Jaket.IO;

/// <summary> Set of different tools for simplifying life and systematization of code. </summary>
public class Tools
{
    #region networking

    /// <summary> Steam id of the local player. </summary>
    public static SteamId Id => SteamClient.SteamId;
    /// <summary> Account id of the local player. </summary>
    public static uint AccId;

    /// <summary> How could I know that Steamworks do not cache this value? </summary>
    public static void CacheAccId() => AccId = Id.AccountId;
    /// <summary> Returns the name of the player with the given AccountId. </summary>
    public static string Name(uint id) => new Friend(id | 76561197960265728u).Name;

    /// <summary> Shortcut needed in order to track statistics and errors. </summary>
    public static void Send(Connection? con, System.IntPtr data, int size)
    {
        if (con == null)
        {
            Log.Warning("An attempt to send data to the connection equal to null.");
            return;
        }

        con.Value.SendMessage(data, size);
        Stats.Write += size;
    }

    #endregion
    #region scene

    /// <summary> Name of the current scene. </summary>
    public static string Scene => SceneHelper.CurrentScene;
    /// <summary> Name of the loading scene. </summary>
    public static string Pending => SceneHelper.PendingScene;

    /// <summary> Loads the given scene. </summary>
    public static void Load(string scene) => SceneHelper.LoadScene(scene);

    /// <summary> Whether the given object is on a scene or is it just an asset. </summary>
    public static bool IsReal(GameObject obj) => obj.scene.name != null;
    public static bool IsReal(Component comp) => IsReal(comp.gameObject);

    #endregion
    #region create, instantiate & destroy

    /// <summary> Creates a new game object and assigns it to the given transform. </summary>
    public static GameObject Create(string name, Transform parent = null)
    {
        GameObject obj = new(name);
        obj.transform.SetParent(parent ?? Plugin.Instance?.transform, false);
        return obj;
    }
    public static T Create<T>(string name, Transform parent = null) where T : Component => Create(name, parent).AddComponent<T>();

    // system namespace also has Object class, so I added this to avoid conflicts

    public static GameObject Instantiate(GameObject obj) => Object.Instantiate(obj);
    public static GameObject Instantiate(GameObject obj, Transform parent) => Object.Instantiate(obj, parent);
    public static GameObject Instantiate(GameObject obj, Vector3 position, Quaternion? rotation = null) => Object.Instantiate(obj, position, rotation ?? Quaternion.identity);

    public static void Destroy(Object obj) => Object.Destroy(obj);
    public static void DestroyImmediate(Object obj) => Object.DestroyImmediate(obj);

    #endregion
    #region resources

    public static T[] ResFind<T>() where T : Object => Resources.FindObjectsOfTypeAll<T>();

    /// <summary> Iterates all objects of the type that predicate for the criterion. </summary>
    public static void ResFind<T>(System.Predicate<T> pred, System.Action<T> cons) where T : Object
    {
        foreach (var item in ResFind<T>()) if (pred(item)) cons(item);
    }

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
    public static void Invoke(string name, object obj, params object[] args) => AccessTools.Method(obj.GetType(), name).Invoke(obj, args);

    #endregion
    #region within

    /// <summary> Whether the vector a is within the given distance from vector b. </summary>
    public static bool Within(Vector3 a, Vector3 b, float dst = 1f) => (a - b).sqrMagnitude < dst * dst;
    public static bool Within(Transform a, Vector3 b, float dst = 1f) => Within(a.position, b, dst);
    public static bool Within(Transform a, Transform b, float dst = 1f) => Within(a.position, b.position, dst);
    public static bool Within(GameObject a, Vector3 b, float dst = 1f) => Within(a.transform.position, b, dst);
    public static bool Within(GameObject a, GameObject b, float dst = 1f) => Within(a.transform.position, b.transform.position, dst);

    #endregion
}

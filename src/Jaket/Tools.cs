global using static Jaket.Tools;

global using IntPtr = System.IntPtr;
global using Array = System.Array;
global using Exception = System.Exception;

namespace Jaket;

using HarmonyLib;
using Steamworks;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

using Object = UnityEngine.Object;

using Jaket.Assets;
using Jaket.IO;
using Jaket.Net;
using Jaket.Net.Types;

/// <summary> Set of different tools for simplifying life and systematization of code. </summary>
public static class Tools
{
    #region steam

    /// <summary> Steam id of the local player. </summary>
    public static SteamId Id => SteamClient.SteamId;
    /// <summary> Account id of the local player. </summary>
    public static uint AccId;

    /// <summary> How could I know that Steamworks do not cache this value? </summary>
    public static void CacheAccId() => AccId = Id.AccountId;
    /// <summary> Returns the name of the player with the given AccountId. </summary>
    public static string Name(uint id) => new Friend(id | 76561197960265728u).Name;

    #endregion
    #region scene

    /// <summary> Name of the current scene. </summary>
    public static string Scene => SceneHelper.CurrentScene;
    /// <summary> Name of the loading scene. </summary>
    public static string Pending => SceneHelper.PendingScene;

    /// <summary> Loads the given scene. </summary>
    public static void LoadScn(string scene) => SceneHelper.LoadScene(scene);

    /// <summary> Whether the given object is on a scene or is it just an asset. </summary>
    public static bool IsReal(GameObject obj) => obj.scene.name != null && obj.scene.name != "DontDestroyOnLoad";
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
    /// <summary> Creates a new game object and adds a component of the given type to it. </summary>
    public static T Create<T>(string name, Transform parent = null) where T : Component => Create(name, parent).AddComponent<T>();

    public static GameObject Inst(GameObject obj) => Object.Instantiate(obj);
    public static GameObject Inst(GameObject obj, Transform parent) => Object.Instantiate(obj, parent);
    public static GameObject Inst(GameObject obj, Vector3 position, Quaternion? rotation = null) => Object.Instantiate(obj, position, rotation ?? Quaternion.identity);

    public static void Dest(Object obj) => Object.Destroy(obj);
    public static void DestImmediate(Object obj) => Object.DestroyImmediate(obj);
    public static void DontDest(Object obj) => Object.DontDestroyOnLoad(obj);

    #endregion
    #region resources

    /// <summary> Returns all objects of the given type. </summary>
    public static T[] ResFind<T>() where T : Object => Resources.FindObjectsOfTypeAll<T>();

    /// <summary> Iterates all objects of the given type. </summary>
    public static void ResFind<T>(Cons<T> cons) where T : Object
    {
        foreach (var item in ResFind<T>()) cons(item);
    }

    /// <summary> Iterates all objects of the given type that are suitable for the given predicate. </summary>
    public static void ResFind<T>(Pred<T> pred, Cons<T> cons) where T : Object
    {
        foreach (var item in ResFind<T>()) if (pred(item)) cons(item);
    }

    /// <summary> Finds object of the given type. </summary>
    public static T ObjFind<T>() where T : Object => Object.FindObjectOfType<T>();

    /// <summary> Finds object with the given name. </summary>
    public static GameObject ObjFind(string name) => GameObject.Find(name);

    #endregion
    #region reflection

    /// <summary> Returns the information about a field with the given name. </summary>
    public static FieldInfo Field<T>(string name) => AccessTools.Field(typeof(T), name);

    /// <summary> Gets the value of a field with the given name. </summary>
    public static object Get<T>(string name, T t) => Field<T>(name).GetValue(t);
    /// <summary> Sets the value of a field with the given name. </summary>
    public static void Set<T>(string name, T t, object value) => Field<T>(name).SetValue(t, value);

    /// <summary> Calls a method with the given name. </summary>
    public static void Call<T>(string name, T t, params object[] args) => AccessTools.Method(typeof(T), name).Invoke(t, args);
    /// <summary> Calls a method with the given name and a single boolean argument. </summary>
    public static void Call<T>(string name, T t, bool arg) => AccessTools.Method(typeof(T), name, new[] { typeof(bool) }).Invoke(t, new object[] { arg });

    /// <summary> Returns the event of pressing the button. </summary>
    public static UnityEvent GetClick(GameObject btn)
    {
        var pointer = btn.GetComponents<MonoBehaviour>()[2]; // so much pain over the private class ControllerPointer
        return AccessTools.Property(pointer.GetType(), "OnPressed").GetValue(pointer) as UnityEvent;
    }

    #endregion
    #region iteration

    /// <summary> Iterates each object in the given enumerable. </summary>
    public static void Each<T>(this System.Collections.Generic.IEnumerable<T> seq, Cons<T> cons)
    {
        foreach (var item in seq) cons(item);
    }

    /// <summary> Iterates each object in the given enumerable that are suitable for the given predicate.. </summary>
    public static void Each<T>(this System.Collections.Generic.IEnumerable<T> seq, Pred<T> pred, Cons<T> cons)
    {
        foreach (var item in seq) if (pred(item)) cons(item);
    }

    #endregion
    #region within

    public static bool Within(Vector3 a, Vector3 b, float dst = 1f) => (a - b).sqrMagnitude < dst * dst;
    public static bool Within(Transform a, Vector3 b, float dst = 1f) => Within(a.position, b, dst);
    public static bool Within(Transform a, Transform b, float dst = 1f) => Within(a.position, b.position, dst);
    public static bool Within(GameObject a, Vector3 b, float dst = 1f) => Within(a.transform.position, b, dst);
    public static bool Within(GameObject a, GameObject b, float dst = 1f) => Within(a.transform.position, b.transform.position, dst);

    #endregion
    #region debug

    /// <summary> Spawns a dummy at the position of the local player. </summary>
    public static RemotePlayer Dummy()
    {
        var dummy = ModAssets.CreateDoll();
        dummy.name = "Dummy";

        // pass the data of the local player to the dummy
        Writer.Write(Networking.LocalPlayer.Write, (data, size) => Reader.Read(data, size, dummy.Read), 48);

        return dummy;
    }

    #endregion
}

/// <summary> Performs an abstract action without any arguments or return value. </summary>
public delegate void Action();

/// <summary> Consumes one value. </summary>
public delegate void Cons<T>(T t);

/// <summary> Consumes two values. </summary>
public delegate void Cons<T, K>(T t, K k);

/// <summary> Predicate that consumes one value. </summary>
public delegate bool Pred<T>(T t);

/// <summary> Provider of one value. </summary>
public delegate T Prov<T>();

/// <summary> Function that consumes one value and returns another one. </summary>
public delegate K Func<T, K>(T t);

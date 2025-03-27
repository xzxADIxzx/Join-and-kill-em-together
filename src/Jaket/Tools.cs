global using static Jaket.Tools;
global using Ptr = System.IntPtr;

namespace Jaket;

using HarmonyLib;
using Steamworks;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

using Jaket.Assets;
using Jaket.Content;
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

    /// <summary> How could I know that Steamworks does not cache this value? </summary>
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

    public static bool IsReal(GameObject obj) => obj.scene.name != null && obj.scene.name != "DontDestroyOnLoad";
    public static bool IsReal(Component comp) => IsReal(comp.gameObject);

    #endregion
    #region build

    /// <summary> Creates a new game object and assigns it to the given transform. </summary>
    public static GameObject Create(string name, Transform parent = null)
    {
        GameObject obj = new(name);
        obj.transform.SetParent(parent ?? Plugin.Instance?.transform, false);
        return obj;
    }

    /// <summary> Creates a new game object and adds a component of the given type to it. </summary>
    public static T Create<T>(string name, Transform parent = null) where T : Component => Create(name, parent).AddComponent<T>();

    /// <summary> Adds a component of the given type to the given game object and returns it. </summary>
    public static T Component<T>(GameObject obj, Cons<T> cons) where T : Component
    {
        var t = obj.AddComponent<T>();
        cons(t);
        return t;
    }

    /// <summary> Gets and optionally sets material properties. </summary>
    public static void Properties(this Renderer renderer, Cons<MaterialPropertyBlock> cons, bool set = false)
    {
        MaterialPropertyBlock block = new();
        renderer.GetPropertyBlock(block);

        cons(block);
        if (set) renderer.SetPropertyBlock(block);
    }

    #endregion
    #region instantiate & destroy

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
    public static void ResFind<T>(Cons<T> cons) where T : Object // TODO remove as it's deprecated and replace with enumerable methods
    {
        foreach (var item in ResFind<T>()) cons(item);
    }

    /// <summary> Iterates all objects of the given type that are suitable for the given predicate. </summary>
    public static void ResFind<T>(Pred<T> pred, Cons<T> cons) where T : Object // TODO remove as it's deprecated and replace with enumerable methods
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
    #region enumerable

    /// <summary> Returns the index of the object in the given enumerable. </summary>
    public static int IndexOf<T>(this IEnumerable<T> seq, T obj)
    {
        int index = 0;
        foreach (var item in seq)
        {
            if (item.Equals(obj)) return index;
            index++;
        }
        return -1;
    }

    /// <summary> Returns the index of the object in the given enumerable that is suitable for the given predicate. </summary>
    public static int IndexOf<T>(this IEnumerable<T> seq, Pred<T> pred)
    {
        int index = 0;
        foreach (var item in seq)
        {
            if (pred(item)) return index;
            index++;
        }
        return -1;
    }

    /// <summary> Iterates each object in the given enumerable. </summary>
    public static void Each<T>(this IEnumerable<T> seq, Cons<T> cons)
    {
        foreach (var item in seq) cons(item);
    }

    /// <summary> Iterates each object in the given enumerable that are suitable for the given predicate. </summary>
    public static void Each<T>(this IEnumerable<T> seq, Pred<T> pred, Cons<T> cons)
    {
        foreach (var item in seq) if (pred(item)) cons(item);
    }

    /// <summary> Whether all of the elements match the given predicate. </summary>
    public static bool All<T>(this IEnumerable<T> seq, Pred<T> pred)
    {
        foreach (var item in seq) if (!pred(item)) return false;
        return true;
    }

    /// <summary> Whether any of the elements match the given predicate. </summary>
    public static bool Any<T>(this IEnumerable<T> seq, Pred<T> pred)
    {
        foreach (var item in seq) if (pred(item)) return true;
        return false;
    }

    /// <summary> Returns the object in the given enumerable that is suitable for the given predicate or default. </summary>
    public static T Find<T>(this IEnumerable<T> seq, Pred<T> pred, Prov<T> defaultProv = null)
    {
        foreach (var item in seq) if (pred(item)) return item;
        return defaultProv == null ? default : defaultProv();
    }

    /// <summary> Clears the given enumerable array by filling it with default values. </summary>
    public static void Clear<T>(this T[] seq) => System.Array.Clear(seq, 0, seq.Length);

    #endregion
    #region delegates

    /// <summary> Performs an abstract action without any arguments or return value. </summary>
    public delegate void Runnable();

    /// <summary> Consumes one value. </summary>
    public delegate void Cons<T>(T t);

    /// <summary> Consumes two values. </summary>
    public delegate void Cons<T, K>(T t, K k);

    /// <summary> Predicate that consumes one value. </summary>
    public delegate bool Pred<T>(T t);

    /// <summary> Provider that returns one value. </summary>
    public delegate T Prov<T>();

    /// <summary> Function that consumes one value and returns another. </summary>
    public delegate K Func<T, K>(T t);

    #endregion
    #region entities

    /// <summary> Whether the type is a player. </summary>
    public static bool IsPlayer(this EntityType type) => type == EntityType.Player;



    /// <summary> Whether the type is an enemy. </summary>
    public static bool IsEnemy(this EntityType type) => type >= EntityType.Filth && type <= EntityType.Brain;

    /// <summary> Whether the type is an enemy and can be spawn only once. </summary>
    public static bool IsHuge(this EntityType type) => type >= EntityType.FleshPrison && type <= EntityType.SisyphusPrime;

    /// <summary> Whether the type is an enemy and can be shot by a coin. </summary>
    public static bool IsTargetable(this EntityType type) => IsEnemy(type) && type != EntityType.Idol && type != EntityType.CancerousRodent;



    /// <summary> Whether the type is an item. </summary>
    public static bool IsItem(this EntityType type) => type >= EntityType.BlueSkull && type <= EntityType.Sowler;

    /// <summary> Whether the type is a bait or fish. </summary>
    public static bool IsFish(this EntityType type) => type >= EntityType.AppleBait && type <= EntityType.BurntStuff;

    /// <summary> Whether the type is a plushie. </summary>
    public static bool IsPlushie(this EntityType type) => type >= EntityType.Hakita && type <= EntityType.Sowler;



    /// <summary> Whether the type is a bullet. </summary>
    public static bool IsBullet(this EntityType type) => type >= EntityType.Coin;

    #endregion
}

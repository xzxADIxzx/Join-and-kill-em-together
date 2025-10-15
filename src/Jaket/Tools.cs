global using static Jaket.Tools;
global using Ptr = System.IntPtr;

namespace Jaket;

using HarmonyLib;
using Steamworks;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

using Jaket.Content;

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

    /// <summary> Adds or gets a component of the given type to the object and returns it. </summary>
    public static T Component<T>(GameObject obj, Cons<T> cons, bool get = false) where T : Component
    {
        var t = get ? obj.GetComponent<T>() : obj.AddComponent<T>();
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
    #region unity

    public static GameObject Inst(GameObject obj) => Object.Instantiate(obj);
    public static GameObject Inst(GameObject obj, Transform parent) => Object.Instantiate(obj, parent);
    public static GameObject Inst(GameObject obj, Vector3 position, Quaternion? rotation = null) => Object.Instantiate(obj, position, rotation ?? Quaternion.identity);

    public static void Dest(Object obj) => Object.Destroy(obj);
    public static void Imdt(Object obj) => Object.DestroyImmediate(obj);
    public static void Keep(Object obj) => Object.DontDestroyOnLoad(obj);

    public static void Each(this Transform parent, Cons<Transform> cons)
    {
        foreach (Transform child in parent) cons(child);
    }
    public static void Dest(Transform transform) => Dest(transform.gameObject);

    public static T[] ResFind<T>() where T : Object => Resources.FindObjectsOfTypeAll<T>();
    public static T   ObjFind<T>() where T : Object => Object.FindObjectOfType<T>();
    public static GameObject ObjFind(string name) => GameObject.Find(name);

    #endregion
    #region world

    /// <summary> Default environment raycast mask. </summary>
    public static readonly int EnvMask = LayerMaskDefaults.Get(LMD.Environment);

    /// <summary> Whether the item is placed on an altar. </summary>
    public static bool Placed(this ItemIdentifier itemId) => itemId.transform.parent?.gameObject.layer == 22;

    public static bool Within(Vector3 a,   Vector3 b,   float dst = 1f) => (a - b).sqrMagnitude < dst * dst;
    public static bool Within(Vector3 a,   Transform b, float dst = 1f) => Within(a, b.position, dst);
    public static bool Within(Vector3 a,   Component b, float dst = 1f) => Within(a, b.transform.position, dst);
    public static bool Within(Transform a, Vector3 b,   float dst = 1f) => Within(a.position, b, dst);
    public static bool Within(Transform a, Transform b, float dst = 1f) => Within(a.position, b.position, dst);
    public static bool Within(Transform a, Component b, float dst = 1f) => Within(a.position, b.transform.position, dst);
    public static bool Within(Component a, Vector3 b,   float dst = 1f) => Within(a.transform.position, b, dst);
    public static bool Within(Component a, Transform b, float dst = 1f) => Within(a.transform.position, b.position, dst);
    public static bool Within(Component a, Component b, float dst = 1f) => Within(a.transform.position, b.transform.position, dst);

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

    /// <summary> Returns the amount of objects in the given enumerable. </summary>
    public static int Count<T>(this IEnumerable<T> seq)
    {
        int amount = 0;
        foreach (var item in seq) amount++;
        return amount;
    }

    /// <summary> Returns the amount of objects in the given enumerable that are suitable for the given predicate. </summary>
    public static int Count<T>(this IEnumerable<T> seq, Pred<T> pred)
    {
        int amount = 0;
        foreach (var item in seq) if (pred(item)) amount++;
        return amount;
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

    /// <summary> Inserts objects into the given enumerable array at the specified index. </summary>
    public static void Insert<T>(ref T[] seq, int index, params T[] obj)
    {
        if (index == -1) index = seq.Length;

        System.Array.Resize(ref seq, seq.Length + obj.Length);
        System.Array.Copy(seq, index, seq, index + obj.Length, seq.Length - obj.Length - index);
        System.Array.Copy(obj, 0, seq, index, obj.Length);
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

    /// <summary> Whether the type is an enemy. </summary>
    public static bool IsEnemy(this EntityType type) => type >= EntityType.Filth && type <= EntityType.Brain;

    /// <summary> Whether the type is an enemy and can be spawn only once. </summary>
    public static bool IsHuge(this EntityType type) => type >= EntityType.FleshPrison && type <= EntityType.SisyphusPrime;

    /// <summary> Whether the type is an enemy and can be shot by a coin. </summary>
    public static bool IsTargetable(this EntityType type) => IsEnemy(type) && type != EntityType.Idol && type != EntityType.CancerousRodent;

    /// <summary> Whether the type is an item. </summary>
    public static bool IsItem(this EntityType type) => type >= EntityType.SkullBlue && type <= EntityType.Sowler;

    /// <summary> Whether the type is a fish. </summary>
    public static bool IsFish(this EntityType type) => type >= EntityType.FishFunny && type <= EntityType.FishBurnt;

    /// <summary> Whether the type is a plushie. </summary>
    public static bool IsPlushie(this EntityType type) => type >= EntityType.Hakita && type <= EntityType.Sowler;

    /// <summary> Whether the type is a weapon. </summary>
    public static bool IsWeapon(this EntityType type) => type >= EntityType.RevolverBlue && type <= EntityType.RocketlRed;

    /// <summary> Whether the type is a projectile. </summary>
    public static bool IsProjectile(this EntityType type) => type >= EntityType.Core && type <= EntityType.Cannonball;

    /// <summary> Whether the type is an explosion. </summary>
    public static bool IsExplosion(this EntityType type) => type >= EntityType.Shockwave && type <= EntityType.HammerParticleHeavy;

    #endregion
}

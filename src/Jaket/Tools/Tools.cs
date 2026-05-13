global using Jaket.Tools; // delegates
global using static Jaket.Tools.Enumy;
global using static Jaket.Tools.Tools;
global using Ptr = System.IntPtr;
global using Ins = System.Collections.Generic.IEnumerable<HarmonyLib.CodeInstruction>;

namespace Jaket.Tools;

using HarmonyLib;
using Steamworks;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

using Jaket.Harmony;
using Jaket.Net;

/// <summary> Set of different tools for simplifying life and systematization of code. </summary>
public static class Tools
{
    #region steam

    /// <summary> Steam's identifier of the player. </summary>
    public static SteamId Id => SteamClient.SteamId;
    /// <summary> Account identifier of the player. </summary>
    public static uint AccId;

    extension(uint id)
    {
        /// <summary> Name of the member. </summary>
        public string Name => new Friend(id | 76561197960265728u).Name;
    }

    extension(Friend member)
    {
        /// <summary> Account identifier of the member. </summary>
        public uint AccId => member.Id.AccountId;
    }

    #endregion
    #region scene

    /// <summary> Name of the current scene. </summary>
    public static string Scene   => SceneHelper.CurrentScene;
    /// <summary> Name of the loading scene. </summary>
    public static string Pending => SceneHelper.PendingScene;

    /// <summary> Loads a scene by its name. </summary>
    public static void LoadScn(string scene) => SceneHelper.LoadScene(scene);

    /// <summary> Whether the gameobject is in the current scene. </summary>
    public static bool IsReal(GameObject obj) => obj.scene.name != null && obj.scene.name != "DontDestroyOnLoad";
    /// <summary> Whether the component is in the current scene. </summary>
    public static bool IsReal(Component comp) => IsReal(comp.gameObject);

    #endregion
    #region build

    /// <summary> Creates a new object and assigns it to the given transform. </summary>
    public static GameObject Create(string name, Transform parent = null)
    {
        GameObject obj = new(name);
        obj.transform.SetParent(parent, false);
        return obj;
    }

    /// <summary> Creates a new object and assigns it to the given transform. </summary>
    public static T Create<T>(string name, Transform parent = null) where T : Component => Create(name, parent ?? Plugin.Instance?.transform).AddComponent<T>();

    /// <summary> Adds or gets a component of the given type and consumes it. </summary>
    public static T Component<T>(GameObject obj, Cons<T> cons, bool get = false) where T : Component
    {
        var t = get ? obj.GetComponent<T>() : obj.AddComponent<T>();
        cons(t);
        return t;
    }

    /// <summary> Adds or gets a component of the given type and consumes it. </summary>
    public static T Component<T>(this Component comp, Cons<T> cons, bool get = false) where T : Component => Component(comp.gameObject, cons, get);

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

    public static GameObject Inst(GameObject obj)                                                => Object.Instantiate(obj                                           );
    public static GameObject Inst(GameObject obj, Transform parent)                              => Object.Instantiate(obj, parent                                   );
    public static GameObject Inst(GameObject obj, Vector3 position, Quaternion? rotation = null) => Object.Instantiate(obj, position, rotation ?? Quaternion.identity);

    public static void Dest(Object obj) => Object.Destroy          (obj);
    public static void Imdt(Object obj) => Object.DestroyImmediate (obj);
    public static void Keep(Object obj) => Object.DontDestroyOnLoad(obj);

    public static void Each(this Transform parent, Cons<Transform> cons) { foreach (Transform child in parent) cons(child); }
    public static void Dest(Transform transform) => Dest(transform.gameObject);
    public static void Imdt(Transform transform) => Imdt(transform.gameObject);

    public static T[] ResFind<T>() where T : Object => Resources.FindObjectsOfTypeAll<T>();
    public static GameObject ObjFind(string name) => GameObject.Find(name);

    #endregion
    #region world

    /// <summary> Default environment raycast mask. </summary>
    public static readonly int EnvMask = LayerMaskDefaults.Get(LMD.Environment);

    /// <summary> Gets an entity of the given type. </summary>
    public static bool TryGetEntity<T>(this Component comp, out T entity) where T : Entity => (entity = comp.TryGetComponent(out Entity.Agent a) && a.Patron is T t ? t : null) != null;

    /// <summary> Whether the item is placed on an altar. </summary>
    public static bool Placed(this ItemIdentifier itemId) => itemId.transform.parent?.gameObject.layer == 22;

    /// <summary> Path of the component in the hierarchy. </summary>
    public static string Path(this Component comp) => $"{comp.transform.parent?.name}/{comp.name}";

    #endregion
    #region reflection

    /// <summary> Returns metadata of a field. </summary>
    public static FieldInfo Field<T>(string name) => AccessTools.Field(typeof(T), name);
    /// <summary> Returns metadata of a method. </summary>
    public static MethodInfo Method<T>(string name) => AccessTools.Method(typeof(T), name);

    /// <summary> Iterates all attributes of static methods.  </summary>
    public static void Attributes(Cons<MethodInfo, IEnumerable<System.Attribute>> cons) => Assembly.GetCallingAssembly().GetTypes().Each(t =>
    {
        t.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.NonPublic).Each(m => cons(m, m.GetCustomAttributes()));
    });

    /// <summary> Applies all patches from the attributes. </summary>
    public static void Apply<T>(MethodInfo method, IEnumerable<System.Attribute> attrs, Harmony harmony) where T : Jaket.Harmony.Patch => attrs.Each(a =>
    {
        if (a is T t)
        {
            if (attrs.Any(a => a is Prefix    )) harmony.Patch(t.Target, prefix:     t.GetPatch(method));
            if (attrs.Any(a => a is Postfix   )) harmony.Patch(t.Target, postfix:    t.GetPatch(method));
            if (attrs.Any(a => a is Transpiler)) harmony.Patch(t.Target, transpiler: t.GetPatch(method));
        }
    });

    #endregion
}

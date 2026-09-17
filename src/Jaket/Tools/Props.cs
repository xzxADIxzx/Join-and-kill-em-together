namespace Jaket.Tools;

using UnityEngine;

using Jaket.Net;

/// <summary> Set of different tools for simplifying life and systematization of code. </summary>
public static class Props
{
    #region components

    /// <summary> Adds or gets a component of the given type and consumes it. </summary>
    public static T Component<T>(GameObject obj, Cons<T> cons, bool get = false) where T : Component
    {
        var comp = get ? obj.GetComponent<T>() : obj.AddComponent<T>();
        cons(comp);
        return comp;
    }

    /// <summary> Adds a component of the given type and consumes it. </summary>
    public static T Add<T>(this GameObject obj, Cons<T> cons) where T : Component => Component(obj, cons);

    /// <summary> Adds a component of the given type and consumes it. </summary>
    public static T Add<T>(this Component comp, Cons<T> cons) where T : Component => Component(comp.gameObject, cons);

    /// <summary> Gets a component of the given type and consumes it. </summary>
    public static T Get<T>(this GameObject obj, Cons<T> cons) where T : Component => Component(obj, cons, true);

    /// <summary> Gets a component of the given type and consumes it. </summary>
    public static T Get<T>(this Component comp, Cons<T> cons) where T : Component => Component(comp.gameObject, cons, true);

    #endregion
    #region properties

    /// <summary> Sets or gets properties of the given renderer and materials. </summary>
    public static void Properties(Renderer renderer, Cons<MaterialPropertyBlock> cons, bool set = false)
    {
        MaterialPropertyBlock block = new();

        renderer.GetPropertyBlock(block);
        cons(block);
        if (set) renderer.SetPropertyBlock(block);
    }

    /// <summary> Sets properties of the given renderer and materials. </summary>
    public static void Set(this Renderer renderer, Cons<MaterialPropertyBlock> cons) => Properties(renderer, cons, true);

    /// <summary> Gets properties of the given renderer and materials. </summary>
    public static void Get(this Renderer renderer, Cons<MaterialPropertyBlock> cons) => Properties(renderer, cons);

    #endregion
    #region instances

    /// <summary> Instantiates the object. </summary>
    public static T Inst<T>(T obj) where T : Object => Object.Instantiate(obj);

    /// <summary> Instantiates the object. </summary>
    public static T Inst<T>(T obj, Transform parent) where T : Object => Object.Instantiate(obj, parent);

    /// <summary> Instantiates the object at the specified world position. </summary>
    public static T Inst<T>(T obj, Vector3 position, Quaternion? rotation = null) where T : Object => Object.Instantiate(obj, position, rotation ?? Quaternion.identity);

    /// <summary> Instantiates the object at the specified world position. </summary>
    public static T Inst<T>(T obj, Vector3 position, Vector3 rotation) where T : Object => Object.Instantiate(obj, position, Quaternion.Euler(rotation));

    /// <summary> Reliably destroys the object. </summary>
    public static void Dest(Object obj)
    {
        if (obj is Transform transform)
            Object.Destroy(transform.gameObject);
        else
            Object.Destroy(obj);
    }

    /// <summary> Immediately destroys the object. </summary>
    public static void Imdt(Object obj)
    {
        if (obj is Transform transform)
            Object.DestroyImmediate(transform.gameObject);
        else
            Object.DestroyImmediate(obj);
    }

    /// <summary> Keeps the object. </summary>
    public static void Keep(Object obj) => Object.DontDestroyOnLoad(obj);

    /// <summary> Iterates the children. </summary>
    public static void Each(this Transform parent, Cons<Transform> cons) { foreach (Transform child in parent) cons(child); }

    #endregion
    #region entities

    /// <summary> Gets an identifier of the object. </summary>
    public static bool HasIdentifier(this GameObject obj, out Entity.Identifier identifier) => obj.TryGetComponent(out identifier);

    /// <summary> Gets an identifier of the object. </summary>
    public static bool HasIdentifier(this Component comp, out Entity.Identifier identifier) => comp.TryGetComponent(out identifier);

    /// <summary> Gets an identifier of the object. </summary>
    public static bool HasIdentifier(this GameObject obj) => HasAgent(obj, out _);

    /// <summary> Gets an identifier of the object. </summary>
    public static bool HasIdentifier(this Component comp) => HasAgent(comp, out _);

    /// <summary> Gets an agent of any entity type. </summary>
    public static bool HasAgent(this GameObject obj, out Entity.Agent agent) => obj.TryGetComponent(out agent);

    /// <summary> Gets an agent of any entity type. </summary>
    public static bool HasAgent(this Component comp, out Entity.Agent agent) => comp.TryGetComponent(out agent);

    /// <summary> Gets an agent of any entity type. </summary>
    public static bool HasAgent(this GameObject obj) => HasAgent(obj, out _);

    /// <summary> Gets an agent of any entity type. </summary>
    public static bool HasAgent(this Component comp) => HasAgent(comp, out _);

    /// <summary> Gets an entity of the given type. </summary>
    public static bool HasEntity<T>(this GameObject obj, out T entity) where T : Entity => (entity = obj.TryGetComponent(out Entity.Agent a) ? a.Patron as T : null) != null;

    /// <summary> Gets an entity of the given type. </summary>
    public static bool HasEntity<T>(this Component comp, out T entity) where T : Entity => (entity = comp.TryGetComponent(out Entity.Agent a) ? a.Patron as T : null) != null;

    /// <summary> Gets an entity of the given type. </summary>
    public static bool HasEntity<T>(this GameObject obj) where T : Entity => HasEntity<T>(obj, out _);

    /// <summary> Gets an entity of the given type. </summary>
    public static bool HasEntity<T>(this Component comp) where T : Entity => HasEntity<T>(comp, out _);

    #endregion
    #region searches

    /// <summary> Finds all objects of the given type. </summary>
    public static T[] ResFind<T>() where T : Object => Resources.FindObjectsOfTypeAll<T>();

    /// <summary> Finds game object by the given path. </summary>
    public static GameObject ObjFind(string path) => GameObject.Find(path);

    /// <summary> Finds a child by the given path. </summary>
    public static GameObject ObjFind(this GameObject obj, string path) => obj.transform.Find(path)?.gameObject;

    /// <summary> Finds a child by the given path. </summary>
    public static GameObject ObjFind(this Component comp, string path) => comp.transform.Find(path)?.gameObject;

    /// <summary> Finds a child by the given path. </summary>
    public static Transform DefFind(this GameObject obj, string path) => obj.transform.Find(path);

    /// <summary> Finds a child by the given path. </summary>
    public static Transform DefFind(this Component comp, string path) => comp.transform.Find(path);

    #endregion
}

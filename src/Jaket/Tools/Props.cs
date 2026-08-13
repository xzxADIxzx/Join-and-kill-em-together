namespace Jaket.Tools;

using UnityEngine;

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

    #endregion
}

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
}

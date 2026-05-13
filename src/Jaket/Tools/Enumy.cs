namespace Jaket.Tools;

using System;
using System.Collections.Generic;

/// <summary> Set of different tools for simplifying life and systematization of code. </summary>
public static class Enumy
{
    #region index

    /// <summary> Returns the index of the object in the given enumerable. </summary>
    public static int IndexOf<T>(this IEnumerable<T> seq, T t)
    {
        int index = 0;
        foreach (var item in seq)
        {
            if (item.Equals(t)) return index;
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

    #endregion
    #region count

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

    #endregion
    #region each

    /// <summary> Iterates each object in the given enumerable. </summary>
    public static void Each<T>(this IEnumerable<T> seq, Cons<T> cons)
    {
        foreach (var item in seq) cons(item);
    }

    /// <summary> Iterates each object in the given enumerable that is suitable for the given predicate. </summary>
    public static void Each<T>(this IEnumerable<T> seq, Pred<T> pred, Cons<T> cons)
    {
        foreach (var item in seq) if (pred(item)) cons(item);
    }

    #endregion
    #region each deviratives

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

    /// <summary> Whether any of the elements match the given object. </summary>
    public static bool Has<T>(this IEnumerable<T> seq, T t)
    {
        foreach (var item in seq) if (item.Equals(t)) return true;
        return false;
    }

    #endregion
    #region cast

    /// <summary> Casts each object in the given enumerable. </summary>
    public static IEnumerable<K> Cast<T, K>(this IEnumerable<T> seq, Func<T, K> cast)
    {
        foreach (var item in seq) yield return cast(item);
    }

    /// <summary> Casts each object in the given enumerable that is suitable for the given predicate. </summary>
    public static IEnumerable<K> Cast<T, K>(this IEnumerable<T> seq, Pred<T> pred, Func<T, K> cast)
    {
        foreach (var item in seq) if (pred(item)) yield return cast(item);
    }

    #endregion
    #region find

    /// <summary> Returns the object in the given enumerable that is suitable for the given predicate. </summary>
    public static T Find<T>(this IEnumerable<T> seq, Pred<T> pred, Prov<T> defaultProv = null)
    {
        foreach (var item in seq) if (pred(item)) return item;
        return defaultProv == null
            ? default
            : defaultProv();
    }

    #endregion
    #region array

    /// <summary> Inserts objects into the given array at the specified index. </summary>
    public static void Insert<T>(ref T[] seq, int index, params T[] obj)
    {
        if (index == -1) index = seq.Length;

        Array.Resize(ref seq, seq.Length + obj.Length);
        Array.Copy(seq, index, seq, index + obj.Length, seq.Length - obj.Length - index);
        Array.Copy(obj, 0, seq, index, obj.Length);
    }

    /// <summary> Clears the given array. </summary>
    public static void Clear<T>(this T[] seq) => Array.Clear(seq, 0, seq.Length);

    #endregion
}

namespace Jaket;

using HarmonyLib;
using Steamworks;
using Steamworks.Data;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

using Object = UnityEngine.Object;

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
    public static string ChatStr(string msg) => msg.Replace("[", "\\[").Replace("]", "\\]");
    public static bool IsDifficultyUKMD(string difficulty) {
        if (difficulty.ToLower().Contains("ukmd") || difficulty.ToLower().Contains("ultrakill"))
        {
            return true;
        }

        if (difficulty == "5") return true;

        return false;
    }
    public static void SetDifficulty(byte val) => PrefsManager.Instance.SetInt("difficulty", val);
    public static byte GetDifficulty() => (byte)PrefsManager.Instance.GetInt("difficulty");
    public static string GetDifficultyName(byte val) => val switch {
        0 => "Harmless",
        1 => "Lenient",
        2 => "Standard",
        3 => "Violent",
        4 => "Brutal",
        5 => "UKMD",
        _ => ""
    };

    public static bool ValidateDifficulty(string val) {
        if (GetDifficultyFromName(val) != 42) return true;
        
        return val switch {
            "0" => true,
            "1" => true,
            "2" => true,
            "3" => true,
            "4" => true,
            "5" => true,
            _   => false
        };
    }

    public static byte GetDifficultyFromName(string name)
    {
        if (name.ToLower().Contains("harmless")) return 0;
        if (name.ToLower().Contains("lenient")) return 1;
        if (name.ToLower().Contains("standard") || name.ToLower().Contains("normal")) return 2;
        if (name.ToLower().Contains("violent")) return 3;
        if (name.ToLower().Contains("brutal")) return 4;

        if (name.ToLower().Contains("ukmd") || name.ToLower().Contains("ultrakill"))
        {
            return 5;
        }

        return 42;
    }

    /// <summary> Shortcut needed in order to track statistics and errors. </summary>
    public static void Send(Connection? con, IntPtr data, int size)
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
    /// <summary> Creates a new game object and adds a component of the given type to it. </summary>
    public static T Create<T>(string name, Transform parent = null) where T : Component => Create(name, parent).AddComponent<T>();

    public static GameObject Instantiate(GameObject obj, Transform parent) => Object.Instantiate(obj, parent);
    public static GameObject Instantiate(GameObject obj, Vector3 position, Quaternion? rotation = null) => Object.Instantiate(obj, position, rotation ?? Quaternion.identity);

    public static void Destroy(Object obj) => Object.Destroy(obj);
    public static void DestroyImmediate(Object obj) => Object.DestroyImmediate(obj);

    #endregion
    #region resources

    public static T[] ResFind<T>() where T : Object => Resources.FindObjectsOfTypeAll<T>();

    /// <summary> Iterates all objects of the type that predicate for the criterion. </summary>
    public static void ResFind<T>(Predicate<T> pred, Action<T> cons) where T : Object
    {
        foreach (var item in ResFind<T>()) if (pred(item)) cons(item);
    }

    public static T ObjFind<T>() where T : Object => Object.FindObjectOfType<T>();
    public static GameObject ObjFind(string name) => GameObject.Find(name);

    /// <summary> Returns the event of pressing the button. </summary>
    public static UnityEvent GetClick(GameObject btn)
    {
        var pointer = btn.GetComponents<MonoBehaviour>()[2]; // so much pain over the private class ControllerPointer
        return AccessTools.Property(pointer.GetType(), "OnPressed").GetValue(pointer) as UnityEvent;
    }

    #endregion
    #region harmony

    /// <summary> Returns the information about a field with the given name. </summary>
    public static FieldInfo Field<T>(string name) => AccessTools.Field(typeof(T), name);

    /// <summary> Gets the value of a field with the given name. </summary>
    public static object Get<T>(string name, T t) => Field<T>(name).GetValue(t);
    /// <summary> Sets the value of a field with the given name. </summary>
    public static void Set<T>(string name, T t, object value) => Field<T>(name).SetValue(t, value);

    /// <summary> Calls a method with the given name. </summary>
    public static void Invoke<T>(string name, T t, params object[] args) => AccessTools.Method(typeof(T), name).Invoke(t, args);
    /// <summary> Calls a method with the given name and a single boolean argument. </summary>
    public static void Invoke<T>(string name, T t, bool arg) => AccessTools.Method(typeof(T), name, new[] { typeof(bool) }).Invoke(t, new object[] { arg });

    #endregion
    #region within

    public static bool Within(Vector3 a, Vector3 b, float dst = 1f) => (a - b).sqrMagnitude < dst * dst;
    public static bool Within(Transform a, Vector3 b, float dst = 1f) => Within(a.position, b, dst);
    public static bool Within(Transform a, Transform b, float dst = 1f) => Within(a.position, b.position, dst);
    public static bool Within(GameObject a, Vector3 b, float dst = 1f) => Within(a.transform.position, b, dst);
    public static bool Within(GameObject a, GameObject b, float dst = 1f) => Within(a.transform.position, b.transform.position, dst);

    #endregion
}

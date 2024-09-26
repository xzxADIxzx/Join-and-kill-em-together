namespace Jaket.World;

using System;
using UnityEngine;

using Jaket.Assets;
using Jaket.Net;

/// <summary> Abstract action performed in the world. </summary>
public class WorldAction
{
    /// <summary> Level for which the action is intended. </summary>
    public readonly string Level;
    /// <summary> The action itself. </summary>
    public readonly Action Action;

    public WorldAction(string level, Action action) { Level = level; Action = action; World.Actions.Add(this); }

    /// <summary> Runs the action if the level matches the desired one. </summary>
    public void Run()
    {
        if (Tools.Scene == Level) Action();
    }
}

/// <summary> Action that runs immediately when a level is loaded. </summary>
public class StaticAction : WorldAction
{
    public StaticAction(string level, Action action) : base(level, action) { }

    /// <summary> Creates a static action that duplicates torches. </summary>
    public static void PlaceTorches(string level, Vector3 pos, float radius) => new StaticAction(level, () =>
    {
        var obj = GameAssets.Torch();
        for (float angle = 360f * 6f / 7f; angle >= 0f; angle -= 360f / 7f)
        {
            float rad = angle * Mathf.Deg2Rad;
            Tools.Instantiate(obj, pos + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * radius, Quaternion.Euler(0f, angle / 7f, 0f));
        }
    });

    /// <summary> Creates a static action that finds an object. </summary>
    public static void Find(string level, string name, Vector3 position, Action<GameObject> action) => new StaticAction(level, () =>
    {
        Tools.ResFind(obj => Tools.IsReal(obj) && obj.transform.position == position && obj.name == name, action);
    });
    /// <summary> Creates a static action that adds a component to an object. </summary>
    public static void Patch(string level, string name, Vector3 position) => Find(level, name, position, obj => obj.AddComponent<ObjectActivator>().events = new());
    /// <summary> Creates a static action that enables an object. </summary>
    public static void Enable(string level, string name, Vector3 position) => Find(level, name, position, obj => obj.SetActive(true));
    /// <summary> Creates a static action that destroys an object. </summary>
    public static void Destroy(string level, string name, Vector3 position) => Find(level, name, position, Tools.Destroy);
}

/// <summary> Action that can be launched remotely. </summary>
public class NetAction : WorldAction
{
    /// <summary> Name of the synchronized object. </summary>
    public string Name;
    /// <summary> Position of the object used to find it. </summary>
    public Vector3 Position;

    public NetAction(string level, string name, Vector3 position, Action action) : base(level, action) { Name = name; Position = position; }

    /// <summary> Creates a net action that synchronizes an object activator component. </summary>
    public static void Sync(string level, string name, Vector3 position, Action<Transform> action = null) => new NetAction(level, name, position, () =>
        Tools.ResFind<ObjectActivator>(
            obj => Tools.IsReal(obj) && Tools.Within(obj.transform, position) && obj.name == name,
            obj =>
            {
                obj.gameObject.SetActive(true);
                obj.ActivateDelayed(obj.delay);

                action?.Invoke(obj.transform);
            }
        ));

    /// <summary> Creates a net action that synchronizes a limbo switch component. </summary>
    public static void SyncLimbo(string level, Vector3 position) => Sync(level, "GameObject", position, obj => obj.GetComponentInParent<LimboSwitch>().Pressed());

    /// <summary> Creates a net action that synchronizes clicks on a button. </summary>
    public static void SyncButton(string level, string name, Vector3 position, Action<RectTransform> action = null)
    {
        StaticAction.Find(level, name, position, obj => Tools.GetClick(obj).AddListener(() =>
        {
            if (LobbyController.Online) World.SyncAction(obj);
        }));
        new NetAction(level, name, position, () => Tools.ResFind<RectTransform>(
            obj => Tools.IsReal(obj) && Tools.Within(obj, position) && obj.name == name,
            obj =>
            {
                Tools.GetClick(obj.gameObject).Invoke();
                action?.Invoke(obj);
            }
        ));
    }
}

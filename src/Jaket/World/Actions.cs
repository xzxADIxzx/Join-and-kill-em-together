namespace Jaket.World;

using System;
using UnityEngine;

using Jaket.Assets;
using Jaket.Net;

/// <summary> Abstract action performed on the world. </summary>
public class WorldAction
{
    /// <summary> Level for which the activator is intended. </summary>
    public readonly string Level;
    /// <summary> The action itself. </summary>
    public readonly Action Action;

    public WorldAction(string level, Action action) { this.Level = level; this.Action = action; World.Actions.Add(this); }

    /// <summary> Runs the action if the scene matches the desired one. </summary>
    public void Run()
    {
        if (Tools.Scene == Level) Action();
    }
}

/// <summary> Action that runs immediately when a level is loaded. </summary>
public class StaticAction : WorldAction
{
    public StaticAction(string level, Action action) : base(level, action) { }

    /// <summary> Creates a static action that duplicates an object in the world. </summary>
    public static StaticAction PlaceTorches(string level, Vector3 pos, float radius) => new(level, () =>
    {
        // there are already 8 torches on the map, no more needed
        if (Tools.ResFind<Torch>().Length >= 8) return;

        var obj = GameAssets.Torch();
        for (float angle = 360f * 6f / 7f; angle >= 0f; angle -= 360f / 7f)
        {
            float rad = angle * Mathf.Deg2Rad;
            GameObject.Instantiate(obj, pos + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * radius, Quaternion.Euler(0f, angle / 7f, 0f))
                .GetComponentInChildren<Light>().intensity = 3f; // lower the brightness so that the place with torches doesn't glow like the sun
        }
    });
    /// <summary> Creates a static action that finds an object in the world. </summary>
    public static StaticAction Find(string level, string name, Vector3 position, Action<GameObject> action) => new(level, () =>
    {
        Tools.ResFind(obj => Tools.IsReal(obj) && obj.transform.position == position && obj.name == name, action);
    });
    /// <summary> Creates a static action that adds an object activation component to an object in the world. </summary>
    public static StaticAction Patch(string level, string name, Vector3 position) => Find(level, name, position, obj => obj.AddComponent<ObjectActivator>().events = new());
    /// <summary> Creates a static action that enables an object in the world. </summary>
    public static StaticAction Enable(string level, string name, Vector3 position) => Find(level, name, position, obj => obj.SetActive(true));
    /// <summary> Creates a static action that destroys an object in the world. </summary>
    public static StaticAction Destroy(string level, string name, Vector3 position) => Find(level, name, position, Tools.Destroy);
}

/// <summary> Action that can be launched remotely. </summary>
public class NetAction : WorldAction
{
    /// <summary> Name of the synchronized object. </summary>
    public string Name;
    /// <summary> Position of the object used to find it. </summary>
    public Vector3 Position;

    public NetAction(string level, string name, Vector3 position, Action action) : base(level, action) { this.Name = name; this.Position = position; }

    /// <summary> Creates a net action that enables an object that has an ObjectActivator component. </summary>
    public static NetAction Sync(string level, string name, Vector3 position, Action<GameObject> action = null) => new(level, name, position, () =>
        Tools.ResFind<GameObject>(
            obj => Tools.IsReal(obj) && obj.transform.position == position && obj.name == name,
            obj => { obj.SetActive(true); action?.Invoke(obj); }
        ));

    /// <summary> Creates a net action that synchronizes clicks on a button. </summary>
    public static NetAction SyncButton(string level, string name, Vector3 position, Action<GameObject> action = null)
    {
        // synchronize clicks on the given button
        var net = Sync(level, name, position, obj => { Tools.GetClick(obj).Invoke(); action?.Invoke(obj); });

        // patch the button to sync press on it if it was not already pressed by anyone
        StaticAction.Find(level, name, position, obj => Tools.GetClick(obj).AddListener(() =>
        {
            if (LobbyController.IsOwner || !World.Instance.Activated.Contains((byte)World.Actions.IndexOf(net))) World.SyncActivation(net);
        }));
        return net;
    }
}

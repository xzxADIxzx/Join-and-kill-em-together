namespace Jaket.World;

using System;
using UnityEngine;

using Jaket.Assets;

/// <summary> Abstract action performed on the world. </summary>
public class WorldAction
{
    /// <summary> Level for which the activator is intended. </summary>
    public readonly string Level;
    /// <summary> The action itself. </summary>
    public readonly Action Action;

    public WorldAction(string level, Action action) { this.Level = level; this.Action = action; }

    /// <summary> Runs the action if the scene matches the desired one. </summary>
    public void Run()
    {
        if (SceneHelper.CurrentScene == Level) Action();
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
        if (Resources.FindObjectsOfTypeAll<Torch>().Length >= 8) return;

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
        foreach (var obj in Resources.FindObjectsOfTypeAll<GameObject>())
            if (obj.gameObject.scene != null && obj.transform.position == position && obj.name == name) action(obj);
    });
    /// <summary> Creates a static action that adds an object activation component to an object in the world. </summary>
    public static StaticAction Patch(string level, string name, Vector3 position) => Find(level, name, position, obj => obj.AddComponent<ObjectActivator>());
    /// <summary> Creates a static action that enables an object in the world. </summary>
    public static StaticAction Enable(string level, string name, Vector3 position) => Find(level, name, position, obj => obj.SetActive(true));
    /// <summary> Creates a static action that destroys an object in the world. </summary>
    public static StaticAction Destroy(string level, string name, Vector3 position) => Find(level, name, position, GameObject.Destroy);
}

/// <summary> Action that can be launched remotely. </summary>
public class NetAction : WorldAction
{
    /// <summary> Name of the synchronized object. </summary>
    public string Name;
    /// <summary> Position of the object used to find it. </summary>
    public Vector3 Position;

    public NetAction(string level, Action action, string name, Vector3 position) : base(level, action) { this.Name = name; this.Position = position; }

    /// <summary> Creates a net action that enables an object that has an ObjectActivator component. </summary>
    public static NetAction Sync(string level, string name, Vector3 position, Action<GameObject> action = null) => new(level, () =>
    {
        action ??= obj => obj.SetActive(true);
        foreach (var obj in Resources.FindObjectsOfTypeAll<ObjectActivator>())
            if (obj.gameObject.scene != null && obj.transform.position == position && obj.name == name) action(obj.gameObject);
    }, name, position);
}

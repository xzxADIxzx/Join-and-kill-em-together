namespace Jaket.World;

using System;
using UnityEngine;

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

    /// <summary> Creates a static action that finds an object in the world. </summary>
    public static StaticAction Find(string level, string path, Action<GameObject> cons) => new(level, () =>
    {
        var obj = GameObject.Find(path);
        if (obj) cons(obj);
    });
    /// <summary> Creates a static action that destroys an object in the world. </summary>
    public static StaticAction Destroy(string level, string path) => new(level, () => GameObject.Destroy(GameObject.Find(path)));
}

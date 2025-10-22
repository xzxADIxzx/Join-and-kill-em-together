namespace Jaket.World;

using UnityEngine;

/// <summary> Abstract action that can be performed in the world. </summary>
public class Action
{
    /// <summary> Scene that the action is meant to be performed in. </summary>
    public readonly string Scene;
    /// <summary> Actual action to perform in the scene. </summary>
    public readonly Cons<Vector2> Perform;

    /// <summary> Whether the action can be performed at any time. </summary>
    public readonly bool Dynamic;
    /// <summary> Whether the action can be performed multiple times. </summary>
    public readonly bool Reperformable;

    /// <summary> Path of the object to perform the action on. </summary>
    public readonly string Path;
    /// <summary> Number of objects that have the same path. </summary>
    public int Collisions => ResFind<Transform>().Count(o => $"{o.parent?.name}/{o.name}" == Path);

    public Action(string scene, string path, bool dynamic, bool reperformable, Cons<Vector2> perform)
    {
        Scene = scene;
        Perform = perform;
        Dynamic = dynamic;
        Reperformable = reperformable;
        Path = path;
    }

    public Action(string scene, string path, bool dynamic, bool reperformable, Runnable perform) : this(scene, path, dynamic, reperformable, _ => perform()) { }
}

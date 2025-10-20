namespace Jaket.World;

using UnityEngine;

using Jaket.Assets;

/// <summary> Tangible actions of multiple types. </summary>
public static class ActionType
{
    /// <summary> Creates an action that is performed on scene load. </summary>
    public static void Run(string scene, Runnable perform) => ActionList.Add(new(scene, null, false, false, perform));

    /// <summary> Creates an actions that places a bunch of torches. </summary>
    public static void Torches(string scene, Vector3 position, float radius) => Run(scene, () =>
    {
        GameAssets.Prefab("Levels/Interactive/Altar (Torch) Variant.prefab", p => Events.Post(() => // damn Unity crashes w/o this
        {
            for (float angle = Mathf.PI * 12f / 7f; angle > 0f; angle -= Mathf.PI * 2f / 7f)
            {
                Inst(p, position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius, Quaternion.Euler(0f, -angle * 180f / Mathf.PI, 0f));
            }
        }));
    });

    /// <summary> Creates an action that finds an object. </summary>
    public static void Find(string scene, string path, Cons<Transform> perform) => ActionList.Add(new(scene, path, false, false, () =>
    {
        ResFind<Transform>().Each(o => IsReal(o) && $"{o.parent?.name}/{o.name}" == path, perform);
    }));

    /// <summary> Creates an action that turns an object on. </summary>
    public static void Turn(string scene, string path) => Find(scene, path, t => t.gameObject.SetActive(true));

    /// <summary> Creates an action that destroys an object. </summary>
    public static void Dest(string scene, string path) => Find(scene, path, t => Tools.Dest(t.gameObject));
}

namespace Jaket.World;

using UnityEngine;

using Jaket.Assets;
using Jaket.Net;

/// <summary> Tangible actions of multiple types. </summary>
public static class ActionType
{
    #region static

    /// <summary> Creates an action that is performed on scene load. </summary>
    public static void Run(string scene, Runnable perform) => ActionList.Add(new(scene, null, false, false, perform));

    /// <summary> Creates an actions that places a bunch of torches. </summary>
    public static void Torches(string scene, Vector3 position) => Run(scene, () =>
    {
        GameAssets.Prefab("Levels/Interactive/Altar (Torch) Variant.prefab", p => Events.Post(() => // damn Unity crashes w/o this
        {
            for (float angle = Mathf.PI * 12f / 7f; angle > 0f; angle -= Mathf.PI * 2f / 7f)
            {
                Inst(p, position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 2f, Quaternion.Euler(0f, -angle * 180f / Mathf.PI, 0f))
                    .GetComponentsInChildren<ItemIdentifier>().Each(i =>
                    {
                        if (!LobbyController.IsOwner) Tools.Dest(i.gameObject);
                    });
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

    #endregion
    #region dynamic

    /// <summary> Creates an action that finds an object of the given type. </summary>
    public static void Find<T>(string scene, string path, Cons<T> perform) where T : Component => ActionList.Add(new(scene, path, true, true, pos =>
    {
        ResFind<T>().Each(t => IsReal(t) && t.transform.position.x == pos.x && t.transform.position.z == pos.y, perform);
    }));

    /// <summary> Creates an action that synchronizes all statues. </summary>
    public static void Statue(string scene) => Find<StatueActivator>(scene, "statue", s => s.gameObject.SetActive(true));

    /// <summary> Creates an action that synchronizes all switches. </summary>
    public static void Switch(string scene) => Find<LimboSwitch>(scene, "switch", s => s.Pressed());

    /// <summary> Creates an action that synchronizes all flammables. </summary>
    public static void Flammable(string scene) => Find<Flammable>(scene, "flammable", f => f.Burn(4f));

    #endregion
}

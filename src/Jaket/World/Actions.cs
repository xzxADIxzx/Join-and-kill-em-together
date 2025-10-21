namespace Jaket.World;

using UnityEngine;

/// <summary> Action that can be launched remotely. </summary>
public class NetAction
{
    /// <summary> Name of the synchronized object. </summary>
    public string Name;
    /// <summary> Position of the object used to find it. </summary>
    public Vector3 Position;

    public NetAction(string level, string name, Vector3 position, Runnable action) { Name = name; Position = position; }

    /// <summary> Creates a net action that synchronizes an object activator component. </summary>
    public static void Sync(string level, string name, Vector3 position, Cons<Transform> action = null) => new NetAction(level, name, position, () =>
        ResFind<ObjectActivator>().Each(
            obj => IsReal(obj) && Within(obj, position) && obj.name == name,
            obj =>
            {
                obj.gameObject.SetActive(true);
                obj.ActivateDelayed(obj.delay);

                action?.Invoke(obj.transform);
            }
        ));

    /// <summary> Creates a net action that synchronizes clicks on a button. </summary>
    public static void SyncButton(string level, string name, Vector3 position, Cons<RectTransform> action = null)
    {
        /* questionable
        StaticAction.Find(level, name, position, obj => GetClick(obj).AddListener(() =>
        {
            if (LobbyController.Online) World.SyncAction(obj);
        }));
        */
        new NetAction(level, name, position, () => ResFind<RectTransform>().Each(
            obj => IsReal(obj) && Within(obj, position) && obj.name == name,
            obj =>
            {
                GetClick(obj.gameObject).Invoke();
                action?.Invoke(obj);
            }
        ));
    }
}

namespace Jaket.World;

using UnityEngine;
using UnityEngine.Events;

using Jaket.Assets;
using Jaket.Net;
using Jaket.Sam;
using Jaket.UI;

using static Jaket.UI.Rect;

/// <summary> List of all interactions with the level needed by the multiplayer. </summary>
public class WorldActionsList
{
    // NEVER DO DESTROY IMMEDIATE IN STATIC ACTION
    public static void Load()
    {
        string l; // just for focusing attention
        #region 0-5
        l = "Level 0-5";

        StaticAction.Find(l, "Cube", new(182f, 4f, 382f), obj => obj.SetActive(LobbyController.IsOwner));

        NetAction.Sync(l, "Cube", new(182f, 4f, 382f));
        NetAction.Sync(l, "StatueActivator", new(212.5f, -6.5f, 394.5f));
        NetAction.Sync(l, "StatueActivator", new(212.5f, -6.5f, 369.5f));
        NetAction.Sync(l, "DelayedDoorActivation", new(175f, -6f, 382f));

        #endregion
        #region 1-2
        l = "Level 1-2";

        // there is a door within the Very Cancerous Rodent
        NetAction.Sync(l, "Cube (1)", new(-61f, -21.5f, 400.5f));

        #endregion
        #region 1-4
        l = "Level 1-4";

        // disable boss fight launch trigger for clients in order to sync the cutscene
        StaticAction.Find(l, "Cube", new(0f, 11f, 612f), obj =>
        {
            obj.SetActive(LobbyController.IsOwner);
            Tools.Destroy(obj.GetComponent<DoorController>());
        });
        StaticAction.Find(l, "V2", new(0f, 6f, 648.5f), obj => { if (!LobbyController.IsOwner) Tools.Destroy(obj); });

        NetAction.Sync(l, "Cube", new(0f, -19f, 612f), obj => obj.GetComponent<ObjectActivator>().Activate());

        #endregion
    }
}

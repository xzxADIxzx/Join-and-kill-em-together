namespace Jaket.World;

using UnityEngine;
using UnityEngine.Events;

using Jaket.Content;
using Jaket.Net;
using Jaket.Net.Types;
using Jaket.Sam;
using Jaket.UI;

using static Jaket.UI.Rect;

/// <summary> List of all interactions with the level needed by the multiplayer. </summary>
public class WorldActionsList
{
    /// <summary> Funny text for the Gate Control Terminal™. </summary>
    public const string BASEMENT_TERMILA_TEXT =
@"MACHINE ID: V3#{0}
LOCATION: TERMINAL
CURRENT OBJECTIVE: FUN

AUTHORIZATION... <color=#32CD32>DONE</color>

> Hello?
<color=#FF341C>UNKNOWN COMMAND</color>

> Please let me in :3
OPENING ALL DOORS... <color=#32CD32>DONE</color>";

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
        #region 7-2
        l = "Level 7-2";

        void Fill(string text, int size, bool def, Transform screen, params string[] toDestroy)
        {
            var canvas = screen.GetChild(0);
            foreach (var name in toDestroy) Tools.Destroy(canvas.Find(name).gameObject);

            UIB.Text(text, canvas, Size(964f, 964f), size: size, align: def ? TextAnchor.MiddleCenter : TextAnchor.UpperLeft)
               .transform.localScale = Vector3.one / 8f;
        }

        StaticAction.Find(l, "Intro -> Outdoors", new(-115f, 55f, 419.5f), obj =>
        {
            var door = obj.GetComponent<Door>();
            door?.onFullyOpened.AddListener(() =>
            {
                door.onFullyOpened = new(); // clear listeners

                HudMessageReceiver.Instance?.SendHudMessage("What?", silent: true);
                SamAPI.TryPlay("What?", Networking.LocalPlayer.Voice);
            });
        });
        StaticAction.Find(l, "9A", new(-23.5f, 37.75f, 806.25f), obj =>
        {
            // well, actions aren't perfect
            if (obj.transform.parent.name == "9 Nonstuff") return;

            // open all of the doors and disable the Gate Control Terminal™
            for (int i = 1; i < obj.transform.childCount; i++) Tools.Destroy(obj.transform.GetChild(i).gameObject);

            var text = string.Format(BASEMENT_TERMILA_TEXT, Networking.LocalPlayer.Id.ToString().Substring(0, 3));
            Fill(text, 64, false, obj.transform.Find("PuzzleScreen"), "Text (TMP) (1)", "Button A", "Button B", "Button C", "Button D");
        });
        // don't block the path of the roomba once the fight starts
        StaticAction.Find(l, "Trigger", new(-218.5f, 65f, 836.5f), obj => Tools.Destroy(obj.GetComponent<ObjectActivator>()));
        StaticAction.Find(l, "PuzzleScreen (1)", new(-230.5f, 31.75f, 813.5f), obj =>
        {
            Fill("UwU", 256, true, obj.transform, "Text (TMP)", "Button (Closed)");
        });
        StaticAction.Find(l, "PuzzleScreen (1)", new(-317.75f, 55.25f, 605.25f), obj =>
        {
            if (!LobbyController.IsOwner) Fill("Only the host can do this!", 120, true, obj.transform, "Text (TMP)", "UsableButtons");
        });

        // enable the track points at the level
        StaticAction.Enable(l, "0 - Door 1", new(46.5f, 26.75f, 753.75f));
        StaticAction.Enable(l, "1.25 - Door 2", new(46.5f, 26.75f, 788.75f));
        StaticAction.Enable(l, "2.25 - Door 3", new(46.5f, 26.75f, 823.75f));
        StaticAction.Enable(l, "3.5 - Door 4", new(46.5f, 26.75f, 858.75f));

        NetAction.Sync(l, "TowerDestruction", new(-119.75f, 34f, 552.25f));
        NetAction.Sync(l, "DelayToClaw", new(-305.75f, 30f, 620.5f), obj => obj.transform.parent.Find("BayDoor").GetComponent<Door>().SimpleOpenOverride());

        #endregion
        #region 7-4
        l = "Level 7-4";

        // security system fight
        StaticAction.Find(l, "Trigger", new(0f, 495.25f, 713.25f), obj => obj.SetActive(LobbyController.IsOwner));
        StaticAction.Find(l, "SecuritySystem", new(0f, 0f, 8.25f), obj =>
        {
            var e = obj.AddComponent<ObjectActivator>().events = new();
            (e.onActivate = new()).AddListener(() =>
            {
                var b = obj.GetComponent<CombinedBossBar>();
                for (int i = 0; i < b.enemies.Length; i++)
                {
                    var s = World.Instance.SecuritySystem[i] = b.enemies[i].gameObject.AddComponent<SecuritySystem>();
                    s.Type = EntityType.SecuritySystemOffset + i;
                }
            });
        });

        StaticAction.Destroy(l, "ArenaWalls", new(-26.5f, 470f, 763.75f));

        NetAction.Sync(l, "Trigger", new(0f, 495.25f, 713.25f), obj => obj.GetComponent<ObjectActivator>().Activate());
        NetAction.Sync(l, "ShieldDeactivator", new(0f, 477.5f, 724.25f));
        NetAction.Sync(l, "DeathSequence", new(-2.5f, 472.5f, 724.25f));
        NetAction.SyncButton(l, "Button", new(0f, 476.5f, 717.15f));

        // insides
        StaticAction.Find(l, "EntryTrigger", new(0f, 458.5f, 649.75f), obj => obj.SetActive(LobbyController.IsOwner));
        StaticAction.Find(l, "BrainFightTrigger", new(6.9999f, 841.5f, 610.7503f), obj => obj.SetActive(LobbyController.IsOwner));

        NetAction.Sync(l, "EntryTrigger", new(0f, 458.5f, 649.75f), obj =>
        {
            obj.GetComponent<ObjectActivator>().Activate();
            NewMovement.Instance.transform.position = new(0f, 460f, 650f);
        });
        NetAction.Sync(l, "Deactivator", new(0.75f, 550.5f, 622.75f));
        NetAction.Sync(l, "BrainFightTrigger", new(6.999941f, 841.5f, 610.7503f), obj =>
        {
            NewMovement.Instance.transform.position = new(0f, 826.5f, 610f);
        });

        #endregion
        #region cyber grind
        l = "Endless";

        // move the death zone, because entities spawn at the origin
        StaticAction.Find(l, "Cube", new(-40f, 0.5f, 102.5f), obj => obj.transform.position = new(-40f, -10f, 102.5f));

        #endregion
    }
}

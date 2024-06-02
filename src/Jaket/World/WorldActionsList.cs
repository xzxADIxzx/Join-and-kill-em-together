namespace Jaket.World;

using UnityEngine;

using Jaket.Content;
using Jaket.Net;
using Jaket.Net.Types;
using Jaket.Sam;
using Jaket.UI;
using Jaket.UI.Fragments;

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
        #region 0-1
        l = "Level 0-1";

        NetAction.Sync(l, "Cube (2)", new(202f, 73f, 421f)); // boss

        #endregion
        #region 0-2
        l = "Level 0-2";

        StaticAction.Destroy(l, "Invisible Wall", new(0f, -7f, 163.5f));
        StaticAction.Destroy(l, "Invisible Wall", new(-45f, -8f, 287.5f));
        StaticAction.Destroy(l, "SwordMachine (1)", new(13f, -10f, 173f));
        StaticAction.Destroy(l, "Activator", new(-44.5f, 0f, 157f));
        StaticAction.Destroy(l, "SwordsMachine", new(-45f, -11f, 268f));
        StaticAction.Destroy(l, "SwordsMachine", new(-55f, -11f, 293f));

        NetAction.Sync(l, "Activator", new(-81f, 9f, 339.5f)); // boss

        #endregion
        #region 0-3
        l = "Level 0-3";

        NetAction.Sync(l, "Cube (1)", new(-89.5f, 9.363636f, 413f)); // boss

        #endregion
        #region 0-5
        l = "Level 0-5";

        StaticAction.Find(l, "Cube", new(182f, 4f, 382f), obj => obj.GetComponent<ObjectActivator>().events.toDisActivateObjects[0] = null);

        NetAction.Sync(l, "Cube", new(182f, 4f, 382f)); // boss
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
        #region 5-1
        l = "Level 5-1";

        StaticAction.Destroy(l, "HudMessage", new(0f, -100f, 295.5f));
        StaticAction.Destroy(l, "Door", new(218.5f, -41f, 234.5f));

        // there is a checkpoint deactivator, the deactivation of which needs to be synchronized, and some metro doors
        NetAction.Sync(l, "CheckPointsUndisabler", new(0f, -50f, 350f));
        NetAction.Sync(l, "DelayedActivator", new(-15f, 36f, 698f));
        NetAction.Sync(l, "DelayedActivator", new(-15f, 38f, 778f));

        #endregion
        #region 7-2
        l = "Level 7-2";

        void Fill(string text, int size, bool def, Transform screen, params string[] toDestroy)
        {
            var canvas = screen.GetChild(0);
            foreach (var name in toDestroy) Tools.Destroy(canvas.Find(name)?.gameObject);

            UIB.Text(text, canvas, Size(964f, 964f), size: size, align: def ? TextAnchor.MiddleCenter : TextAnchor.UpperLeft).transform.localScale /= 8f;
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

            var text = string.Format(BASEMENT_TERMILA_TEXT, Tools.AccId.ToString().Substring(0, 3));
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
        #region 7-3
        l = "Level 7-3";

        // wtf?! why is there a torch???
        StaticAction.Find(l, "1 - Dark Path", new(0f, -10f, 300f), obj => Tools.Destroy(obj.transform.Find("Altar (Torch) Variant").GetChild(0).gameObject));
        StaticAction.Find(l, "Door 1", new(-55.5f, -2.5f, 618.5f), obj => obj.GetComponent<Door>().Unlock());
        StaticAction.Find(l, "Door 2", new(-75.5f, -12.5f, 568.5f), obj => obj.GetComponent<Door>().Unlock());
        StaticAction.Find(l, "Door 1", new(-75.5f, -12.5f, 578.5f), obj => obj.GetComponent<Door>().Unlock());
        StaticAction.Find(l, "12 - Grand Hall", new(-212.5f, -35f, 483.75f), obj =>
        {
            // teleport players to the final room once the door is opened
            obj.GetComponent<ObjectActivator>().events.onActivate.AddListener(() => Teleporter.Teleport(new(-189f, -33.5f, 483.75f)));
        });
        StaticAction.Find(l, "ViolenceHallDoor", new(-148f, 7.5f, 276.25f), obj => Tools.Destroy(obj.GetComponent<Collider>()));

        StaticAction.Destroy(l, "Door 2", new(-95.5f, 7.5f, 298.75f));
        StaticAction.Destroy(l, "ViolenceHallDoor (1)", new(-188f, 7.5f, 316.25f));

        NetAction.Sync(l, "Opener", new(-170.5f, 0.5f, 480.75f));
        NetAction.Sync(l, "Opener", new(-170.5f, 0.5f, 490.75f), obj => Tools.ObjFind("Outdoors Areas/6 - Interior Garden/NightSkyActivator").SetActive(true));
        NetAction.Sync(l, "BigDoorOpener", new(-145.5f, -10f, 483.75f), obj => obj.transform.parent.gameObject.SetActive(true));

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
                    var s = World.SecuritySystem[i] = b.enemies[i].gameObject.AddComponent<SecuritySystem>();
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
        StaticAction.Find(l, "BrainFightTrigger", new(6.999941f, 841.5f, 610.7503f), obj => obj.SetActive(LobbyController.IsOwner));

        NetAction.Sync(l, "EntryTrigger", new(0f, 458.5f, 649.75f), obj =>
        {
            obj.GetComponent<ObjectActivator>().Activate();
            Teleporter.Teleport(new(0f, 460f, 650f));
        });
        NetAction.Sync(l, "Deactivator", new(0.75f, 550.5f, 622.75f));
        NetAction.Sync(l, "BrainFightTrigger", new(6.999941f, 841.5f, 610.7503f), obj =>
        {
            obj.GetComponent<ObjectActivator>().Activate();
            Teleporter.Teleport(new(0f, 826.5f, 610f));
        });
        NetAction.Sync(l, "DelayedIdolSpawner", new(14.49993f, 914.25f, 639.7503f), obj =>
        {
            obj.transform.parent.gameObject.SetActive(true);
            obj.transform.parent.parent.gameObject.SetActive(true);
        });

        #endregion
        #region cyber grind
        l = "Endless";

        // move the death zone, because entities spawn at the origin
        StaticAction.Find(l, "Cube", new(-40f, 0.5f, 102.5f), obj => obj.transform.position = new(-40f, -10f, 102.5f));

        #endregion
    }
}

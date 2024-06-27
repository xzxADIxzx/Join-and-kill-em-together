namespace Jaket.World;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.Net;
using Jaket.Net.Types;
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

        NetAction.Sync(l, "Activator", new(-81f, 9f, 339.5f), obj => obj.parent.parent.gameObject.SetActive(true)); // boss

        #endregion
        #region 0-3
        l = "Level 0-3";

        NetAction.Sync(l, "Cube (1)", new(-89.5f, 9.3f, 413f)); // boss

        #endregion
        #region 0-5
        l = "Level 0-5";

        StaticAction.Find(l, "Cube", new(182f, 4f, 382f), obj => obj.GetComponent<ObjectActivator>().events.toDisActivateObjects[0] = null); // corridor

        NetAction.Sync(l, "Cube", new(182f, 4f, 382f)); // boss
        NetAction.Sync(l, "DelayedDoorActivation", new(175f, -6f, 382f));

        #endregion
        #region 1-2
        l = "Level 1-2";

        // secret statue
        NetAction.Sync(l, "Cube", new(0f, -19f, 442f));
        NetAction.Sync(l, "Cube", new(15f, -15f, 417f));

        // Very Cancerous Rodent™
        NetAction.Sync(l, "Cube", new(-61f, -16.5f, 388.5f));
        NetAction.Sync(l, "Cube (1)", new(-61f, -21.5f, 400.5f));

        #endregion
        #region 1-3
        l = "Level 1-3";

        StaticAction.Find(l, "Trigger", new(0f, 9.5f, 412f), obj => obj.GetComponent<ObjectActivator>().events.toDisActivateObjects[0] = null); // corridor

        #endregion
        #region 1-4
        l = "Level 1-4";

        StaticAction.Find(l, "Cube", new(0f, 11f, 612f), obj => Tools.Destroy(obj.GetComponent<DoorController>()));

        NetAction.Sync(l, "Cube", new(0f, -19f, 612f)); // boss

        #endregion
        #region 2-4
        l = "Level 2-4";

        StaticAction.Find(l, "DoorsActivator", new(425f, -10f, 650f), obj =>
        {
            obj.GetComponent<ObjectActivator>().events.onActivate.AddListener(() =>
            {
                Teleporter.Teleport(new(500f, 14f, 570f));
            });
        });
        StaticAction.Destroy(l, "Cube", new(130f, 13f, 702f));

        NetAction.Sync(l, "BossActivator", new(142.5f, 13f, 702.5f));
        NetAction.Sync(l, "DeadMinos", new(279.5f, -599f, 575f), obj =>
        {
            obj.parent.Find("GlobalLights (2)/MetroWall (10)").gameObject.SetActive(false);
            obj.parent.Find("BossMusic").gameObject.SetActive(false);
        });

        #endregion
        #region 3-1
        l = "Level 3-1";

        NetAction.Sync(l, "Trigger", new(-203f, -72.5f, 563f)); // lightning
        NetAction.Sync(l, "Deactivator", new(-203f, -72.5f, 528f));
        NetAction.Sync(l, "End Lights", new(-203f, -72.5f, 528f));

        #endregion
        #region 3-2
        l = "Level 3-2";

        NetAction.Sync(l, "Cube", new(-5f, -121f, 965f), obj => Teleporter.Teleport(new(-5f, -159.5f, 970f))); // boss

        #endregion
        #region 4-1
        l = "Level 4-1";

        NetAction.SyncLimbo(l, new(-290.25f, 24.5f, 814.75f));

        #endregion
        #region 4-2
        l = "Level 4-2";

        NetAction.SyncLimbo(l, new(-150.75f, 33f, 953.1049f));

        StaticAction.Enable(l, "6A - Indoor Garden", new(-19f, 35f, 953.9481f));
        StaticAction.Enable(l, "6B - Outdoor Arena", new(35f, 35f, 954f));

        StaticAction.Destroy(l, "6A Activator", new(-79f, 45f, 954f));
        StaticAction.Destroy(l, "6B Activator", new(116f, 19.5f, 954f));

        NetAction.Sync(l, "DoorOpeners", new(-1.5f, -18f, 774.5f));
        NetAction.Sync(l, "DoorsOpener", new(40f, 5f, 813.5f));

        #endregion
        #region 4-3
        l = "Level 4-3";

        StaticAction.PlaceTorches(l, new(0f, -10f, 310f), 3f);
        StaticAction.Destroy(l, "Doorblocker", new(-59.5f, -35f, 676f));

        NetAction.Sync(l, "DoorActivator", new(2.5f, -40f, 628f));
        NetAction.Sync(l, "Trigger (Intro)", new(-104f, -20f, 676f)); // boss
        NetAction.Sync(l, "Secret Tablet", new(-116.4f, -39.5f, 675.9f), obj => MusicManager.Instance.StopMusic());

        #endregion
        #region 4-4
        l = "Level 4-4";

        StaticAction.Find(l, "SecondVersionActivator", new(117.5f, 663.5f, 323f), obj => obj.GetComponent<ObjectActivator>().events.onActivate.AddListener(() =>
        {
            Networking.EachEntity(e => e.Type == EntityType.V2_GreenArm, e => e.gameObject.SetActive(true));
        }));

        NetAction.Sync(l, "Trigger", new(117.5f, 678.5f, 273f)); // boss
        NetAction.Sync(l, "ExitTrigger", new(172.5f, 668.5f, 263f), obj =>
        {
            Networking.EachEntity(e => e.Type == EntityType.V2_GreenArm, e => e.gameObject.SetActive(false));
        });
        NetAction.Sync(l, "BossOutro", new(117.5f, 663.5f, 323f));
        NetAction.Sync(l, "ExitBuilding Raise", new(1027f, 261f, 202.5f), obj =>
        {
            var next = Tools.ObjFind("TutorialMessage").transform.Find("DeactivateMessage").gameObject;
            if (next.activeSelf) return;
            next.SetActive(true);

            var exit = obj.parent.Find("ExitBuilding");
            exit.GetComponent<Door>().Close();
            exit.Find("GrapplePoint (2)").gameObject.SetActive(true);
        });

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
        #region 5-2
        l = "Level 5-2";

        StaticAction.Destroy(l, "SkullBlue", new(-3.700458f, -1.589029f, 950.6616f));
        StaticAction.Destroy(l, "Arena 1", new(87.5f, -53f, 1240f));
        StaticAction.Destroy(l, "Arena 2", new(87.5f, -53f, 1240f));

        NetAction.Sync(l, "Trigger 1", new(103.4f, 2.61f, 914.7f));
        NetAction.Sync(l, "Trigger 2", new(103.8f, -7.8f, 930.1f));

        NetAction.Sync(l, "FightActivator", new(-77.7f, 52.5f, 1238.9f)); // boss

        #endregion
        #region 5-3
        l = "Level 5-3";

        // there are altars that activate skulls in the mirror part of the level, but the client has these skulls destroyed
        StaticAction.Find(l, "Cube", new(-64.5f, 17.4f, 390.5f), obj =>
        {
            if (obj.TryGetComponent(out ItemPlaceZone zone)) zone.activateOnSuccess = new[] { zone.activateOnSuccess[1] };
        });
        StaticAction.Find(l, "Cube", new(-64.5f, 17.4f, 398.5f), obj =>
        {
            if (obj.TryGetComponent(out ItemPlaceZone zone)) zone.activateOnSuccess = new[] { zone.activateOnSuccess[1] };
        });

        #endregion
        #region 5-4
        l = "Level 5-4";

        NetAction.Sync(l, "Activator", new(641.2f, 690f, 521.7f), obj => // boss
        {
            obj.gameObject.scene.GetRootGameObjects().Do(o =>
            {
                if (o.name == "Underwater") o.SetActive(false);
                if (o.name == "Surface") o.SetActive(true);
            });
            Teleporter.Teleport(new(641.25f, 691.5f, 522f));
        });

        #endregion
        #region 6-1
        l = "Level 6-1";

        StaticAction.Find(l, "Trigger", new(0f, -10f, 590.5f), obj => obj.GetComponent<ObjectActivator>().events.toDisActivateObjects[0] = null);
        StaticAction.Destroy(l, "Cube (5)", new(-40f, -10f, 548.5f));

        StaticAction.Find(l, "Door", new(168.5f, -36.62495f, 457f), obj => obj.GetComponent<Door>().closedPos = new(0f, 13.3751f, -15f));
        StaticAction.Destroy(l, "Cage", new(168.5f, -130f, 140f));
        StaticAction.Destroy(l, "Cube", new(102f, -165f, -503f));

        NetAction.Sync(l, "EnemyActivatorTrigger", new(168.5f, -125f, -438f));

        #endregion
        #region 6-2
        l = "Level 6-2";

        StaticAction.Destroy(l, "Door", new(-179.5f, 20f, 350f));

        NetAction.Sync(l, "Trigger", new(-290f, 40f, 350f)); // boss

        #endregion
        #region 7-1
        l = "Level 7-1";

        // secret boss
        StaticAction.Destroy(l, "ViolenceArenaDoor", new(-120f, 0f, 530.5f));
        NetAction.Sync(l, "Trigger", new(-120f, 5f, 591f));

        // garden
        StaticAction.Find(l, "Cube", new(0f, 3.4f, 582.5f), obj => obj.transform.position = new(0f, 7.4f, 582.5f));
        StaticAction.Destroy(l, "Cube", new(-66.25f, 9.9f, 485f));

        StaticAction.Destroy(l, "ViolenceArenaDoor", new(0f, 12.5f, 589.5f));
        StaticAction.Destroy(l, "Walkway Arena -> Stairway Up", new(80f, -25f, 590f));

        NetAction.Sync(l, "Closer", new(0f, 20f, 579f));
        NetAction.SyncLimbo(l, new(96.75f, 26f, 545f));

        // tunnel
        StaticAction.Patch(l, "Wave 2", new(-242.5f, 0f, 0f));

        NetAction.SyncButton(l, "Forward Button", new(-242.5f, -112.675f, 310.2799f), obj =>
        {
            obj.parent.parent.parent.parent.parent.parent.parent.parent.gameObject.SetActive(true);
            Teleporter.Teleport(new(-242.5f, -112.5f, 314f));
        });
        NetAction.Sync(l, "Wave 2", new(-242.5f, 0f, 0f));
        NetAction.Sync(l, "Wave 3", new(-242.5f, 0f, 0f));
        NetAction.Sync(l, "PlayerTeleportActivator", new(-242.5f, 0f, 0f));

        // outro
        StaticAction.Find(l, "FightStart", new(-242.5f, 120f, -399.75f), obj => obj.GetComponent<ObjectActivator>().events.toDisActivateObjects[2] = null);
        NetAction.Sync(l, "FightStart", new(-242.5f, 120f, -399.75f)); // boss

        #endregion
        #region 7-2
        l = "Level 7-2";

        // other world
        void Fill(string text, int size, TextAnchor align, Transform canvas)
        {
            for (int i = 3; i < canvas.childCount; i++) Tools.Destroy(canvas.GetChild(i).gameObject);
            UIB.Text(text, canvas, Size(964f, 964f), null, size, align).transform.localScale /= 8f;
        }
        StaticAction.Find(l, "Intro -> Outdoors", new(-115f, 55f, 419.5f), obj =>
        {
            var door = obj.GetComponent<Door>();
            door?.onFullyOpened.AddListener(() =>
            {
                door.onFullyOpened = null;
                HudMessageReceiver.Instance?.SendHudMessage("<size=48>What?</size>", silent: true);
            });
        });
        StaticAction.Find(l, "9A", new(-23.5f, 37.75f, 806.25f), obj =>
        {
            // well, actions aren't perfect
            if (obj.transform.parent.name == "9 Nonstuff") return;

            // open all of the doors
            for (int i = 1; i < obj.transform.childCount; i++) Tools.Destroy(obj.transform.GetChild(i).gameObject);

            // disable the Gate Control Terminal™
            Fill(string.Format(BASEMENT_TERMILA_TEXT, Tools.AccId), 64, TextAnchor.UpperLeft, obj.transform.Find("PuzzleScreen/Canvas"));
        });
        StaticAction.Find(l, "PuzzleScreen (1)", new(-230.5f, 31.75f, 813.5f), obj => Fill("UwU", 256, TextAnchor.MiddleCenter, obj.transform.Find("Canvas")));

        StaticAction.Find(l, "Trigger", new(-218.5f, 65f, 836.5f), obj => Tools.Destroy(obj.GetComponent<ObjectActivator>()));
        StaticAction.Find(l, "BayDoor", new(-305.75f, 49.75f, 600.5f), obj =>
        {
            ObjectActivator trigger;
            obj.GetComponent<Door>().activatedRooms = new[] { (trigger = Tools.Create<ObjectActivator>("Trigger", obj.transform)).gameObject };

            trigger.gameObject.SetActive(false);
            trigger.reactivateOnEnable = true;

            trigger.events = new() { onActivate = new() };
            trigger.events.onActivate.AddListener(() =>
            {
                var root = obj.transform.parent.Find("UsableScreen (1)/PuzzleScreen (1)/Canvas/UsableButtons/");
                root.Find("Button (Closed)").gameObject.SetActive(false);
                root.Find("Button (Open)").gameObject.SetActive(true);
            });
            trigger.events.toDisActivateObjects = new[] { trigger.gameObject };
        });

        // enable the track points at the level
        StaticAction.Enable(l, "0 - Door 1", new(46.5f, 26.75f, 753.75f));
        StaticAction.Enable(l, "1.25 - Door 2", new(46.5f, 26.75f, 788.75f));
        StaticAction.Enable(l, "2.25 - Door 3", new(46.5f, 26.75f, 823.75f));
        StaticAction.Enable(l, "3.5 - Door 4", new(46.5f, 26.75f, 858.75f));

        NetAction.Sync(l, "Trigger", new(-115f, 50f, 348.5f));
        NetAction.Sync(l, "TowerDestruction", new(-119.75f, 34f, 552.25f));

        // library
        StaticAction.Find(l, "Enemies", new(88.5f, 5.75f, 701.25f), obj =>
        {
            if (!LobbyController.IsOwner) foreach (var act in obj.GetComponents<MonoBehaviour>()) Tools.Destroy(act);
        });
        NetAction.Sync(l, "Arena Start", new(133.5f, 45.75f, 701.25f));

        #endregion
        #region 7-3
        l = "Level 7-3";

        // why is there a torch???
        StaticAction.Find(l, "1 - Dark Path", new(0f, -10f, 300f), obj => Tools.Destroy(obj.transform.Find("Altar (Torch) Variant/Cube").gameObject));

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

        NetAction.Sync(l, "Trigger", new(-145.5f, 5f, 483.75f), obj => Teleporter.Teleport(new(-131f, -14.5f, 483.75f)));
        NetAction.Sync(l, "Opener", new(-170.5f, 0.5f, 480.75f));
        NetAction.Sync(l, "Opener", new(-170.5f, 0.5f, 490.75f), obj => Tools.ObjFind("Outdoors Areas/6 - Interior Garden/NightSkyActivator").SetActive(true));
        NetAction.Sync(l, "BigDoorOpener", new(-145.5f, -10f, 483.75f), obj => obj.transform.parent.gameObject.SetActive(true));

        #endregion
        #region 7-4
        l = "Level 7-4";

        // security system fight
        StaticAction.Find(l, "Trigger", new(0f, 495.25f, 713.25f), obj => obj.GetComponent<ObjectActivator>()?.events.onActivate.AddListener(() =>
        {
            var b = obj.transform.parent.GetComponentInChildren<CombinedBossBar>(true);
            for (int i = 0; i < b.enemies.Length; i++)
                (World.SecuritySystem[i] = b.enemies[i].gameObject.AddComponent<SecuritySystem>()).Type = EntityType.SecuritySystemOffset + i;
        }));
        NetAction.Sync(l, "Trigger", new(0f, 495.25f, 713.25f), obj => Teleporter.Teleport(new(0f, 472f, 745f), false));
        NetAction.Sync(l, "ShieldDeactivator", new(0f, 477.5f, 724.25f));
        NetAction.Sync(l, "DeathSequence", new(-2.5f, 472.5f, 724.25f));
        NetAction.SyncButton(l, "Button", new(0f, 476.5f, 717.15f));

        // insides
        StaticAction.Find(l, "BrainFightTrigger", new(6.999941f, 841.5f, 610.7503f), obj => obj.GetComponent<ObjectActivator>()?.events.onActivate.AddListener(() =>
        {
            if (World.Brain) World.Brain.IsFightActive = true;
        }));
        NetAction.Sync(l, "EntryTrigger", new(0f, 458.5f, 649.75f), obj => Teleporter.Teleport(new(0f, 460f, 650f)));
        NetAction.Sync(l, "Deactivator", new(0.75f, 550.5f, 622.75f));
        NetAction.Sync(l, "BrainFightTrigger", new(6.999941f, 841.5f, 610.7503f), obj => Teleporter.Teleport(new(0f, 826.5f, 610f)));
        NetAction.Sync(l, "DelayedIdolSpawner", new(14.49993f, 914.25f, 639.7503f), obj =>
        {
            obj.transform.parent.gameObject.SetActive(true);
            obj.transform.parent.parent.gameObject.SetActive(true);
        });

        #endregion
        #region P-1
        l = "Level P-1";

        #endregion
        #region P-2
        l = "Level P-2";

        #endregion
        #region cyber grind
        l = "Endless";

        // move the death zone, because entities spawn at the origin
        StaticAction.Find(l, "Cube", new(-40f, 0.5f, 102.5f), obj => obj.transform.position = new(-40f, -10f, 102.5f));

        #endregion
    }
}

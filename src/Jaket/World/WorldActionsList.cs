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

        NetAction.Sync(l, "Activator", new(-81f, 9f, 339.5f), obj => obj.transform.parent.parent.gameObject.SetActive(true)); // boss

        #endregion
        #region 0-3
        l = "Level 0-3";

        NetAction.Sync(l, "Cube (1)", new(-89.5f, 9.363636f, 413f)); // boss

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
            obj.transform.parent.Find("GlobalLights (2)").Find("MetroWall (10)").gameObject.SetActive(false);
            obj.transform.parent.Find("BossMusic").gameObject.SetActive(false);
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

        StaticAction.Destroy(l, "Door", new(-10f, -161f, 955f));
        StaticAction.Destroy(l, "Backwall", new(-65f, -205f, 934f));

        NetAction.Sync(l, "Cube", new(-5f, -121f, 965f)); // boss

        #endregion
        #region 4-1
        l = "Level 4-1";

        NetAction.Sync(l, "GameObject", new(-290.25f, 24.5f, 814.75f), obj => obj.GetComponentInParent<LimboSwitch>().Pressed());

        #endregion
        #region 4-2
        l = "Level 4-2";

        NetAction.Sync(l, "GameObject", new(-150.75f, 33f, 953.1049f), obj => obj.GetComponentInParent<LimboSwitch>().Pressed());

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
        NetAction.Sync(l, "Secret Tablet", new(-116.425f, -39.593f, 675.9866f), obj => MusicManager.Instance.StopMusic());

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
            var exit = obj.transform.parent.Find("ExitBuilding");
            exit.GetComponent<Door>().Close();
            exit.Find("GrapplePoint (2)").gameObject.SetActive(true);

            Tools.ObjFind("TutorialMessage").transform.Find("DeactivateMessage").gameObject.SetActive(true);
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
        #region 6-2
        l = "Level 6-2";

        StaticAction.Destroy(l, "Door", new(-179.5f, 20f, 350f));

        NetAction.Sync(l, "Trigger", new(-290f, 40f, 350f)); // boss

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

        // duplicate torches at levels 4-3 and P-1

        StaticAction.PlaceTorches("Level P-1", new(-0.84f, -10f, 16.4f), 2f);

        // for some reason this object cannot be found located
        StaticAction.Find("Level 5-2", "6 (Secret)", new(-3.5f, -3f, 940.5f), obj =>
        {
            Tools.Destroy(obj.transform.Find("Altar (Blue Skull) Variant").GetChild(0).gameObject);
        });
        // fix the red altar at the very beggining of the level
        StaticAction.Find("Level 7-1", "Cube", new(0f, 3.4f, 582.5f), obj => obj.transform.position = new(0f, 7.4f, 582.5f));
        // disable the roomba panel for clients
        StaticAction.Find("Level 7-1", "ScreenActivator", new(-242.5f, -112f, 311f), obj =>
        {
            if (!LobbyController.IsOwner) obj.SetActive(false);
        });
        // boss fight unloads the previous location
        StaticAction.Find("Level 7-1", "FightStart", new(-242.5f, 120f, -399.75f), obj =>
        {
            obj.GetComponent<ObjectActivator>().events.toDisActivateObjects[2] = null;
        });
        // disable door blocker
        StaticAction.Find("Level P-1", "Trigger", new(360f, -568.5f, 110f), obj =>
        {
            obj.GetComponent<ObjectActivator>().events.toActivateObjects[4] = null;
        });
        StaticAction.Find("Level P-2", "FightActivator", new(-102f, -61.25f, -450f), obj =>
        {
            var act = obj.GetComponent<ObjectActivator>();
            act.events.onActivate = new(); // gothic door
            act.events.toActivateObjects[2] = null; // wall collider
            act.events.toDisActivateObjects[1] = null; // entry collider
            act.events.toDisActivateObjects[2] = null; // elevator
        });

        // crutches everywhere, crutches all the time
        StaticAction.Patch("Level 7-1", "Blockers", new(-242.5f, -115f, 314f));
        StaticAction.Patch("Level 7-1", "Wave 2", new(-242.5f, 0f, 0f));


        // destroy objects in any way interfering with multiplayer


        StaticAction.Destroy("Level 5-2", "Arena 1", new(87.5f, -53f, 1240f));
        StaticAction.Destroy("Level 5-2", "Arena 2", new(87.5f, -53f, 1240f));
        StaticAction.Destroy("Level 6-1", "Cage", new(168.5f, -130f, 140f));
        StaticAction.Destroy("Level 6-1", "Cube", new(102f, -165f, -503f));
        StaticAction.Destroy("Level 7-1", "SkullRed", new(-66.25f, 9.8f, 485f));
        StaticAction.Destroy("Level 7-1", "ViolenceArenaDoor", new(-120f, 0f, 530.5f));
        StaticAction.Destroy("Level 7-1", "Walkway Arena -> Stairway Up", new(80f, -25f, 590f));



        // boss fight roomba logic
        NetAction.Sync("Level 7-1", "Blockers", new(-242.5f, -115f, 314f), obj =>
        {
            // enable the level in case the player is somewhere else
            obj.transform.parent.parent.parent.parent.gameObject.SetActive(true);

            var btn = obj.transform.parent.Find("Screen").GetChild(0).GetChild(0).GetChild(0).GetChild(0);
            Tools.GetClick(btn.gameObject).Invoke();

            // teleport the player to the roomba so that they are not left behind
            Teleporter.Teleport(obj.transform.position with { y = -112.5f });
        });
        NetAction.Sync("Level 7-1", "Wave 2", new(-242.5f, 0f, 0f));
        NetAction.Sync("Level 7-1", "Wave 3", new(-242.5f, 0f, 0f));
        NetAction.Sync("Level 7-1", "PlayerTeleportActivator", new(-242.5f, 0f, 0f));

        // Minos & Sisyphus have unique cutscenes and non-functional level exits
        NetAction.Sync("Level P-1", "MinosPrimeIntro", new(405f, -598.5f, 110f));
        NetAction.Sync("Level P-1", "End", new(405f, -598.5f, 110f), obj =>
        {
            // obj.SetActive(true);
            obj.transform.parent.Find("Cube (2)").gameObject.SetActive(false);

            Tools.ObjFind("Music 3").SetActive(false);
            obj.transform.parent.Find("Lights").gameObject.SetActive(false);

            StatsManager.Instance.StopTimer();
        });
        NetAction.Sync("Level P-2", "PrimeIntro", new(-102f, -61.25f, -450f));
        NetAction.Sync("Level P-2", "Outro", new(-102f, -61.25f, -450f), obj =>
        {
            // obj.SetActive(true);
            obj.transform.parent.Find("Backwall").gameObject.SetActive(false);

            Tools.ObjFind("BossMusics/Sisyphus").SetActive(false);
            Tools.ObjFind("IntroObjects/Decorations").SetActive(false);
            Tools.ObjFind("Rain").SetActive(false);

            StatsManager.Instance.StopTimer();
        });

        // TODO: 6-1 remove the pillar within the red skull and the wall that appears after entering the big room with a lot of pillars
    }
}

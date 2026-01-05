namespace Jaket.World;

using Logic;
using UnityEngine;
using UnityEngine.UI;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net;
using Jaket.UI.Lib;

/// <summary> List of all interactions with the world. Will replenish over time. </summary>
public static class ActionList
{
    /// <summary> All actions, including vanilla and custom ones. </summary>
    private static Action[] all;
    /// <summary> Identifiers of last vanilla and custom actions. </summary>
    private static int vanilla, custom;
    /// <summary> Whether all of the vanilla actions are loaded. </summary>
    private static bool locked;

    /// <summary> Adds the given action to the global list, taking vanilla and custom identifiers into account. </summary>
    public static void Add(Action action)
    {
        all ??= new Action[byte.MaxValue + 1];
        int i = locked ? vanilla + custom++ : vanilla++;

        if (i < all.Length)
            (all[i] = action).Identifier = i;
        else
            Log.Warning($"[WRLD] Out of identifiers for actions");
    }

    /// <summary> Iterates each action in the global list that is suitable for the current scene and predicate. </summary>
    public static void Each(Pred<Action> pred, Cons<Action> cons)
    {
        for (int i = 0; i < vanilla + custom; i++) if (all[i].Valid && pred(all[i])) cons(all[i]);
    }

    /// <summary> Returns a dynamic action with the given identifier. </summary>
    public static Action At(int id) => all[id].Valid && all[id].Dynamic ? all[id] : null;

    /// <summary> Adds vanilla actions to the global list, and locks their index to prevent custom ones from interfering. </summary>
    public static void Load()
    {
        string l; // decreases the size of further lines

        #region 0-1
        l = "Level 0-1";

        ActionType.Window(l);

        #endregion
        #region 0-2
        l = "Level 0-2";

        ActionType.Window(l);

        #endregion
        #region 0-3
        l = "Level 0-3";

        ActionType.Window(l);

        #endregion
        #region 0-4
        l = "Level 0-4";

        ActionType.Window(l);

        #endregion
        #region 0-5
        l = "Level 0-5";

        #endregion
        #region 0-S
        l = "Level 0-S";

        ActionType.Find(l, "PauseMenu/Restart Mission", b => b.GetComponent<Button>().interactable = LobbyController.IsOwner);

        ActionType.Run(l, () => NewMovement.Instance.modNoJump = true);

        #endregion
        #region 1-1
        l = "Level 1-1";

        ActionType.Window(l);
        ActionType.Switch(l);

        ActionType.Dest(l, "11 Nonstuff/Altar"); // duplicate

        #endregion
        #region 1-2
        l = "Level 1-2";

        ActionType.Statue(l);
        ActionType.Switch(l);

        #endregion
        #region 1-3
        l = "Level 1-3";

        ActionType.Window(l);
        ActionType.Statue(l);
        ActionType.Switch(l);

        // TODO sync R1 - Courtyard/R1 Stuff(Clone)/Enemies/Wave 3/Trigger 'cause there are two statues

        #endregion
        #region 1-4
        l = "Level 1-4";

        ActionType.Switch(l);

        #endregion
        #region 1-S
        l = "Level 1-S";

        #endregion
        #region 2-1
        l = "Level 2-1";

        ActionType.Window(l);
        ActionType.Statue(l);

        #endregion
        #region 2-2
        l = "Level 2-2";

        ActionType.Statue(l);

        #endregion
        #region 2-3
        l = "Level 2-3";

        ActionType.Window(l);

        #endregion
        #region 2-4
        l = "Level 2-4";

        ActionType.Window(l);

        #endregion
        #region 2-S
        l = "Level 2-S";

        #endregion
        #region 3-1
        l = "Level 3-1";

        ActionType.Statue(l);

        #endregion
        #region 3-2
        l = "Level 3-2";

        #endregion
        #region 4-1
        l = "Level 4-1";

        ActionType.Switch(l);
        ActionType.Flammable(l);

        ActionType.Turn(l, "GreedTorch (2)/Flammable"); // for some reason you cannot set it on fire in vanilla game

        #endregion
        #region 4-2
        l = "Level 4-2";

        ActionType.Window(l);
        ActionType.Statue(l);
        ActionType.Switch(l);

        #endregion
        #region 4-3
        l = "Level 4-3";

        ActionType.Statue(l);
        ActionType.Flammable(l);

        ActionType.Torches(l, new(0f, -10f, 310f));

        #endregion
        #region 4-4
        l = "Level 4-4";

        #endregion
        #region 4-S
        l = "Level 4-S";

        #endregion
        #region 5-1
        l = "Level 5-1";

        ActionType.Statue(l);

        #endregion
        #region 5-2
        l = "Level 5-2";

        ActionType.Statue(l);

        #endregion
        #region 5-3
        l = "Level 5-3";

        ActionType.Window(l);
        ActionType.Statue(l);

        #endregion
        #region 5-4
        l = "Level 5-4";

        #endregion
        #region 5-S
        l = "Level 5-S";

        #endregion
        #region 6-1
        l = "Level 6-1";

        #endregion
        #region 6-2
        l = "Level 6-2";

        #endregion
        #region 7-1
        l = "Level 7-1";

        ActionType.Statue(l);
        ActionType.Switch(l);

        #endregion
        #region 7-2
        l = "Level 7-2";

        ActionType.Window(l);
        ActionType.Statue(l);
        ActionType.Switch(l);

        #endregion
        #region 7-3
        l = "Level 7-3";

        #endregion
        #region 7-4
        l = "Level 7-4";

        ActionType.Window(l);

        #endregion
        #region 7-S
        l = "Level 7-S";

        ActionType.Statue(l);

        #endregion
        #region endless
        l = "Endless";

        ActionType.Find(l, "PauseMenu/Restart Mission", b => b.GetComponent<Button>().interactable = LobbyController.IsOwner);

        #endregion
        #region museum
        l = "CreditsMuseum2";

        ActionType.Run(l, () => ResFind<MapIntSetter>().Each(IsReal, Dest));
        ActionType.Dest(l, "/__Gianni nightmare world");

        #endregion
        #region all
        l = "All";

        ActionType.Arena(l);
        ActionType.Final(l);

        ActionType.Find(l, "PauseMenu/Restart Checkpoint", b =>
        {
            if (Gameflow.Mode.NoRestarts())
            {
                b.GetComponentsInParent<PauseMenu>(true).Each(Dest);
                b.GetComponent<Button>().interactable = false;
            }
        });
        ActionType.Find(l, "PauseMenu/Restart Mission", b =>
        {
            if (Gameflow.Mode.NoRestarts()) b.GetComponent<Button>().interactable = LobbyController.IsOwner;
        });

        #endregion
        #region beta
        l = "Main Menu";

        string info =
@"[31][b][red]!!! WARNING !!! WARNING !!! WARNING !!![][][]

This is a public [orange]beta test[] of Jaket. A lot of features are either still in development or subjects to change. Full release of the mod should be expected [orange]after the fraud[] update.

At this moment, the only gamemode available is [orange]Versus[], which is forced for every lobby. This gamemode includes team based health points, bonus healing after killing a player and disabled enemies spawn. More gamemodes are gonna be released in the near future. [12][gray]if i ain't fail examination session[][]

[red]Thank you for your patience :heart:[]
[12][gray]i know it's been 1.5 years since the last update, bruh[][]";

        ActionType.Find(l, "Main Menu (1)/V1", r =>
        {
            var root = Builder.Rect("Warning", r, UI.Lib.Rect.Fill);

            Builder.Image(root, Tex.Back, Pal.semi, Image.Type.Sliced);

            var text = Builder.Rect("Text", root, new(0f, 0f, -32f, -32f, Vector2.zero, Vector2.one));

            Builder.Text(text, Bundle.Parse(info), 24, Pal.white, TextAnchor.UpperLeft);
        });

        // TODO remove later
        Each(a => a.Scene == "Main Menu", a => a.Perform(default));

        #endregion

        locked = true;

        Log.Info($"[WRLD] Loaded {vanilla} actions");
    }
}

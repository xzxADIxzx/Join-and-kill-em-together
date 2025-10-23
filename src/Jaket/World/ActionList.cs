namespace Jaket.World;

using Logic;

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
        for (int i = 0; i < vanilla + custom; i++) if (all[i].Scene == Scene && pred(all[i])) cons(all[i]);
    }

    /// <summary> Returns a dynamic action with the given identifier. </summary>
    public static Action At(int id) => all[id].Scene == Scene && all[id].Dynamic ? all[id] : null;

    /// <summary> Adds vanilla actions to the global list, and locks their index to prevent custom ones from interfering. </summary>
    public static void Load()
    {
        string l; // decreases the size of further lines

        #region 0-1
        l = "Level 0-1";

        #endregion
        #region 0-2
        l = "Level 0-2";

        #endregion
        #region 0-3
        l = "Level 0-3";

        #endregion
        #region 0-4
        l = "Level 0-4";

        #endregion
        #region 0-5
        l = "Level 0-5";

        #endregion
        #region 0-S
        l = "Level 0-S";

        #endregion
        #region 1-1
        l = "Level 1-1";

        ActionType.Switch(l);

        #endregion
        #region 1-2
        l = "Level 1-2";

        ActionType.Statue(l);
        ActionType.Switch(l);

        #endregion
        #region 1-3
        l = "Level 1-3";

        ActionType.Statue(l);
        ActionType.Switch(l);

        // TODO sync R1 - Courtyard/R1 Stuff(Clone)/Enemies/Wave 3/Trigger 'cause there are two statues

        #endregion
        #region 1-4
        l = "Level 1-4";

        ActionType.Statue(l);
        ActionType.Switch(l);

        #endregion
        #region 1-S
        l = "Level 1-S";

        #endregion
        #region 2-1
        l = "Level 2-1";

        ActionType.Statue(l);

        #endregion
        #region 2-2
        l = "Level 2-2";

        ActionType.Statue(l);

        #endregion
        #region 2-3
        l = "Level 2-3";

        #endregion
        #region 2-4
        l = "Level 2-4";

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

        ActionType.Statue(l);
        ActionType.Switch(l);

        #endregion
        #region 4-3
        l = "Level 4-3";

        ActionType.Statue(l);
        ActionType.Flammable(l);

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

        ActionType.Statue(l);
        ActionType.Switch(l);

        #endregion
        #region 7-3
        l = "Level 7-3";

        #endregion
        #region 7-4
        l = "Level 7-4";

        #endregion
        #region 7-S
        l = "Level 7-S";

        ActionType.Statue(l);

        #endregion
        #region museum
        l = "CreditsMuseum2";

        ActionType.Run(l, () => ResFind<MapIntSetter>().Each(IsReal, Dest));
        ActionType.Dest(l, "/__Gianni nightmare world");

        #endregion

        locked = true;
    }
}

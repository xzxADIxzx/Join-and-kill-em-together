namespace Jaket.World;

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
            all[i] = action;
        else
            Log.Warning($"[WRLD] Out of identifiers for actions");
    }

    /// <summary> Adds vanilla actions to the global list, and locks their index to prevent custom ones from interfering. </summary>
    public static void Load()
    {
        string l; // decreases the size of further lines
    }
}

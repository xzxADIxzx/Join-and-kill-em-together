namespace Jaket.Harmony;

using HarmonyLib;

using Jaket.Net;

/// <summary> Class responsible for managing the harmony patches. </summary>
public static class Patches
{
    /// <summary> Harmony instances that patch the source code. </summary>
    public static Harmony Dynamic, Static;
    /// <summary> Whether the source code of the game is patched. </summary>
    public static bool Patched;

    /// <summary> Subscribes to several events for proper work. </summary>
    public static void Load() => Events.OnLobbyAction += () =>
    {
        Dynamic ??= new("xzxADIxzx.Jaket.Dynamic");

        if (LobbyController.Online && !Patched)
        {
            Attributes((m, attrs) => Apply<DynamicPatch>(m, attrs, Dynamic));
            Patched = true;

            Log.Info($"[HARM] Applied {Dynamic.GetPatchedMethods().Count()} dynamic patches");
        }
        if (LobbyController.Offline && Patched)
        {
            Dynamic.UnpatchSelf();
            Patched = false;

            Log.Info($"[HARM] Unapplied all dynamic patches");
        }
    };

    /// <summary> Loads all of the static patches that won't be unapplied ever. </summary>
    public static void LoadStatic()
    {
        Static ??= new("xzxADIxzx.Jaket.Static");

        Attributes((m, attrs) => Apply<StaticPatch>(m, attrs, Static));

        Log.Info($"[HARM] Applied {Static.GetPatchedMethods().Count()} static patches");
    }
}

namespace Jaket.Harmony;

using HarmonyLib;
using System;

using Jaket.Net;
using Jaket.UI.Fragments;

/// <summary> Class responsible for managing the harmony patches. </summary>
public static class Patches
{
    /// <summary> Harmony instance that patches the source code. </summary>
    public static Harmony Harmony;
    /// <summary> Whether the source code of the game is patched. </summary>
    public static bool Patched;

    public static Type[] Types =
    {
        typeof(Spectator)
    };

    /// <summary> Subscribes to several events for proper work. </summary>
    public static void Load() => Events.OnLobbyAction += () =>
    {
        Harmony ??= new("xzxADIxzx.Jaket");

        if (LobbyController.Online && !Patched)
        {
            Types.Each(t => Harmony.CreateClassProcessor(t, true).Patch());
            Patched = true;

            Log.Info($"[HARM] Applied {Harmony.GetPatchedMethods().Count()} patches from {Types.Length} classes");
        }
        if (LobbyController.Offline && Patched)
        {
            Harmony.UnpatchSelf();
            Patched = false;

            Log.Info($"[HARM] Unapplied all patches from {Types.Length} classes");
        }
    };
}

namespace Jaket.Harmony;

using HarmonyLib;
using System;

using Jaket.Input;
using Jaket.Net;
using Jaket.Net.Types;
using Jaket.Net.Vendors;
using Jaket.UI.Elements;
using Jaket.UI.Fragments;

/// <summary> Class responsible for managing the harmony patches. </summary>
public static class Patches
{
    /// <summary> Harmony instances that patch the source code. </summary>
    public static Harmony Dynamic, Static;
    /// <summary> Whether the source code of the game is patched. </summary>
    public static bool Patched;

    public static Type[] DynamicTypes =
    {
        typeof(Item),
        typeof(Fish),
        typeof(Plushie),
        typeof(Core),
        typeof(Sawblade),
        typeof(Rocket),
        typeof(Cannon),
        typeof(Damage),
        typeof(Spectator),
    };
    public static Type[] StaticTypes =
    {
        typeof(ArmsPatch),
        typeof(GunsPatch),
        typeof(RichPresence),
        typeof(Movement),
        typeof(BestiaryEntry),
    };

    /// <summary> Subscribes to several events for proper work. </summary>
    public static void Load() => Events.OnLobbyAction += () =>
    {
        Dynamic ??= new("xzxADIxzx.Jaket.Dynamic");

        if (LobbyController.Online && !Patched)
        {
            DynamicTypes.Each(t => Dynamic.CreateClassProcessor(t, true).Patch());
            Patched = true;

            Log.Info($"[HARM] Applied {Dynamic.GetPatchedMethods().Count()} dynamic patches from {DynamicTypes.Length} classes");
        }
        if (LobbyController.Offline && Patched)
        {
            Dynamic.UnpatchSelf();
            Patched = false;

            Log.Info($"[HARM] Unapplied all dynamic patches from {DynamicTypes.Length} classes");
        }
    };

    /// <summary> Loads all of the static patches that won't be unapplied ever. </summary>
    public static void LoadStatic()
    {
        Static ??= new("xzxADIxzx.Jaket.Static");
        StaticTypes.Each(t => Static.CreateClassProcessor(t, true).Patch());

        Log.Info($"[HARM] Applied {Static.GetPatchedMethods().Count()} static patches from {StaticTypes.Length} classes");
    }
}

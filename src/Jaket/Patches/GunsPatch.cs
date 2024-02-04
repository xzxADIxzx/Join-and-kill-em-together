namespace Jaket.Patches;

using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

using Jaket.Content;
using Jaket.Net;
using Jaket.Net.Types;

[HarmonyPatch]
public class GunsPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GunControl), nameof(GunControl.SwitchWeapon), typeof(int), typeof(List<GameObject>), typeof(bool), typeof(bool), typeof(bool))]
    static void GunSwitch() => Events.OnWeaponChanged.Fire();

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GunControl), nameof(GunControl.ForceWeapon))]
    static void GunForce() => Events.OnWeaponChanged.Fire();

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GunColorGetter), nameof(GunColorGetter.UpdateColor))]
    static bool GunColor(GunColorGetter __instance) => __instance.GetComponentInParent<RemotePlayer>() == null;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(WeaponIcon), nameof(WeaponIcon.UpdateIcon))]
    static bool GunIcon(WeaponIcon __instance) => __instance.GetComponentInParent<RemotePlayer>() == null;
}

[HarmonyPatch]
public class ArmsPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Punch), "ActiveStart")]
    static void Puncn()
    {
        if (LobbyController.Lobby == null) return;

        foreach (var harpoon in NewMovement.Instance.GetComponentsInChildren<Harpoon>())
        {
            Bullets.Punch(harpoon);
            harpoon.name = "Punched";
        }

        Networking.Send(PacketType.Punch, w =>
        {
            w.Id(Networking.LocalPlayer.Id);
            w.Byte(0);

            w.Bool(Networking.LocalPlayer.Parried);
            Networking.LocalPlayer.Parried = false;
        }, size: 10);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Punch), nameof(Punch.GetParryLookTarget))]
    static void Parry() => Networking.LocalPlayer.Parried = true;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(HookArm), "Update")]
    static void Hook(ref bool ___lightTarget, bool ___forcingFistControl, Vector3 ___hookPoint)
    {
        if (LobbyController.Lobby == null) return;

        if (!LobbyController.IsOwner) ___lightTarget = false; // clients should be pulled to all enemies
        Networking.LocalPlayer.Hook = ___forcingFistControl ? ___hookPoint : Vector3.zero;
    }
}

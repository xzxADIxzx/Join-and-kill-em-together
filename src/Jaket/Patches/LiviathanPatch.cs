namespace Jaket.Patches;

using HarmonyLib;

using Jaket.Net;
using Jaket.World;

[HarmonyPatch]
public class LeviathanPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(LeviathanHead), "Update")]
    static void Head(int ___previousAttack, ref int ___projectilesLeftInBurst, ref bool ___lookAtPlayer, ref bool ___rotateBody, ref bool ___inAction)
    {
        if (LobbyController.Offline || World.Instance.Leviathan == null) return;
        if (LobbyController.IsOwner)
        {
            // update the position of the head to synchronize its attacks
            World.Instance.Leviathan.HeadPos = (byte)___previousAttack;
        }
        else
        {
            ___projectilesLeftInBurst = 100; // replenish ammo

            // disable animations and changing of attacks so that the leviathan does not shake
            ___lookAtPlayer = ___rotateBody = false;
            ___inAction = true;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LeviathanTail), nameof(LeviathanTail.ChangePosition))]
    static void Tail(int ___previousSpawnPosition)
    {
        // update the position of the tail to synchronize its attacks
        if (LobbyController.Online && LobbyController.IsOwner) World.Instance.Leviathan.TailPos = (byte)___previousSpawnPosition;
    }
}

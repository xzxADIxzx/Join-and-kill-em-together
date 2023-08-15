namespace Jaket.HarmonyPatches;

using HarmonyLib;

using Jaket.Net;
using Jaket.World;

[HarmonyPatch(typeof(LeviathanHead), "Update")]
public class HeadPatch
{
    static void Postfix(int ___previousAttack, ref int ___projectilesLeftInBurst, ref bool ___lookAtPlayer, ref bool ___rotateBody, ref bool ___inAction)
    {
        if (LobbyController.Lobby == null) return;

        if (LobbyController.IsOwner)
        {
            // update the position of the head to synchronize its attacks
            if (World.Instance.Leviathan != null) World.Instance.Leviathan.HeadPos = (byte)___previousAttack;
        }
        else
        {
            // replenish ammo
            ___projectilesLeftInBurst = 100;

            // disable animations and changing of attacks so that the leviathan does not shake
            ___lookAtPlayer = ___rotateBody = false;
            ___inAction = true;
        }
    }
}

[HarmonyPatch(typeof(LeviathanTail), nameof(LeviathanTail.ChangePosition))]
public class TailPatch
{
    static void Postfix(int ___previousSpawnPosition)
    {
        // update the position of the tail to synchronize its attacks
        if (LobbyController.Lobby != null && LobbyController.IsOwner) World.Instance.Leviathan.TailPos = (byte)___previousSpawnPosition;
    }
}

namespace Jaket.Patches;

using HarmonyLib;

using Jaket.World;

[HarmonyPatch]
public class LeviathanPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(LeviathanHead), nameof(LeviathanHead.ChangePosition))]
    static void Head(int ___previousSpawnPosition)
    {
        if (World.Leviathan && World.Leviathan.IsOwner) World.Leviathan.HeadPos = (byte)___previousSpawnPosition;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LeviathanHead), "Descend")]
    static void Head()
    {
        if (World.Leviathan && World.Leviathan.IsOwner)
        {
            World.Leviathan.HeadPos = byte.MaxValue;
            World.Leviathan.Attack = byte.MaxValue;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LeviathanTail), nameof(LeviathanTail.ChangePosition))]
    static void Tail(int ___previousSpawnPosition)
    {
        if (World.Leviathan && World.Leviathan.IsOwner) World.Leviathan.TailPos = (byte)___previousSpawnPosition;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LeviathanHead), "ProjectileBurst")]
    static void Ball()
    {
        if (World.Leviathan && World.Leviathan.IsOwner) World.Leviathan.Attack = 0;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LeviathanHead), "Bite")]
    static void Bite()
    {
        if (World.Leviathan && World.Leviathan.IsOwner) World.Leviathan.Attack = 1;
    }
}

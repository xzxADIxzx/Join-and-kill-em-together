namespace Jaket.HarmonyPatches;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net;
using Jaket.Net.EntityTypes;
using Jaket.World;

[HarmonyPatch(typeof(EnemyIdentifier), "Start")]
public class EnemyStartPatch
{
    static bool Prefix(EnemyIdentifier __instance)
    {
        // the player is not connected, nothing needs to be done
        if (LobbyController.Lobby == null) return true;

        // I don't even want to know why
        if (__instance.dead) return true;

        // level 0-2 contains several cutscenes that do not need to be removed
        if (SceneHelper.CurrentScene == "Level 0-2" && __instance.enemyType == EnemyType.Swordsmachine && __instance.GetComponent<BossHealthBar>() == null) return true;

        // levels 2-4 and 5-4 contain unique bosses that needs to be dealt with separately
        if (SceneHelper.CurrentScene == "Level 2-4" && __instance.gameObject.name == "MinosArm")
        {
            var hand = __instance.gameObject.AddComponent<Hand>();
            if (LobbyController.IsOwner) Networking.Entities[hand.Id] = hand;

            return true;
        }
        if (SceneHelper.CurrentScene == "Level 5-4" && __instance.enemyType == EnemyType.Leviathan)
        {
            var leviathan = __instance.gameObject.AddComponent<Leviathan>();
            if (LobbyController.IsOwner) Networking.Entities[leviathan.Id] = leviathan;

            return true;
        }

        // the enemy was created remotely
        if (__instance.gameObject.name == "Net")
        {
            // add spawn effect for better look
            __instance.spawnIn = true;

            // teleport the enemy so that the spawn effect does not appear at the origin
            if (__instance.TryGetComponent<Enemy>(out var enemy)) __instance.transform.position = new(enemy.x.target, enemy.y.target, enemy.z.target);

            return true;
        }

        bool boss = __instance.gameObject.GetComponent<BossHealthBar>() != null;
        if (boss) Networking.Bosses.Add(__instance);

        if (LobbyController.IsOwner)
        {
            var enemy = __instance.gameObject.AddComponent<Enemy>();
            Networking.Entities[enemy.Id] = enemy;

            return true;
        }
        else
        {
            // for some incredible reason this boss can't just be turned off
            if (__instance.gameObject.name == "Mandalore")
            {
                Object.Destroy(__instance.gameObject);
                World.Instance.Recache();
            }

            // to avoid a huge number of errors in the console, need to destroy the parent object
            if (__instance.gameObject.name == "Body" && __instance.enemyType == EnemyType.MaliciousFace)
                Object.Destroy(__instance.transform.parent.gameObject);

            if (boss)
                // will be used in the future to trigger the game's internal logic
                __instance.gameObject.SetActive(false);
            else
                // the enemy is no longer needed, so destroy it
                Object.Destroy(__instance.gameObject); // TODO ask host to spawn enemy if playing sandbox

            return false;
        }
    }
}

[HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.DeliverDamage))]
public class EnemyDamagePatch
{
    static bool Prefix(EnemyIdentifier __instance, Vector3 force, float multiplier, bool tryForExplode, float critMultiplier, GameObject sourceWeapon)
    {
        // whether the damage was dealt with a melee weapon
        bool melee = Bullets.Melee.Contains(__instance.hitter);

        // if source weapon is null, then the damage was caused by the environment
        if (LobbyController.Lobby == null || (sourceWeapon == null && !melee)) return true;

        // network bullets are needed just for the visual, damage is done through packets
        if (sourceWeapon == Bullets.SynchronizedBullet && !melee) return false;

        // if the original weapon is network damage, then the damage was received over the network
        if (sourceWeapon == Bullets.NetworkDamage) return true;

        // if the enemy doesn't have an entity component, then it was created before the lobby
        if (!__instance.TryGetComponent<Entity>(out var entity)) return true;

        // if the player dodges, then no damage is needed
        if (entity is RemotePlayer player && player.dashing) return false;

        // if the damage was caused by the player himself, then all others must be notified about this
        Networking.Redirect(Writer.Write(w =>
        {
            w.Id(entity.Id);
            w.Byte((byte)Networking.LocalPlayer.Team);
            w.Bool(melee);

            w.Vector(force);
            w.Float(multiplier);
            w.Bool(tryForExplode);
            w.Float(critMultiplier);
        }), PacketType.DamageEntity);

        return true;
    }
}

[HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.Death))]
public class EnemyDeathPatch
{
    static void Prefix(EnemyIdentifier __instance)
    {
        // only the host should report death
        if (LobbyController.Lobby == null || !LobbyController.IsOwner || __instance.dead) return;

        // if the enemy doesn't have an entity component, then it was created before the lobby
        if (!__instance.TryGetComponent<Enemy>(out var enemy)) return;

        // notify each client that the enemy has died
        byte[] enemyData = Writer.Write(w => w.Id(enemy.Id));
        LobbyController.EachMemberExceptOwner(member => Networking.Send(member.Id, enemyData, PacketType.EnemyDied));

        // notify every client that the boss has died
        if (Networking.Bosses.Contains(__instance))
        {
            byte[] bossData = Writer.Write(w => w.String(__instance.gameObject.name));
            LobbyController.EachMemberExceptOwner(member => Networking.Send(member.Id, bossData, PacketType.BossDefeated));
        }

        // destroy the component so that it is no longer synchronized over the network
        Object.Destroy(enemy);
    }
}

[HarmonyPatch(typeof(Idol), "SlowUpdate")]
public class IdolsLogicPatch
{
    // idols shouldn't have logic on clients
    static bool Prefix() => LobbyController.Lobby == null || LobbyController.IsOwner;
}

[HarmonyPatch(typeof(FerrymanFake), "Update")]
public class FakeFerrymanUpdatePatch
{
    // rotation is synchronized by the enemy component
    static bool Prefix() => LobbyController.Lobby == null || LobbyController.IsOwner;
}

[HarmonyPatch(typeof(FerrymanFake), nameof(FerrymanFake.OnLand))]
public class FakeFerrymanDeathPatch
{
    // destroying the client's fake ferryman
    static void Prefix(FerrymanFake __instance)
    {
        if (LobbyController.IsOwner && __instance.TryGetComponent<Enemy>(out var enemy)) Networking.Redirect(Writer.Write(w => w.Id(enemy.Id)), PacketType.EnemyDied);
    }
}

[HarmonyPatch(typeof(BossHealthBar), "Awake")]
public class HealthBarPatch
{
    // this is necessary to correctly display the boss's health
    static void Prefix(BossHealthBar __instance)
    {
        if (LobbyController.Lobby == null || !__instance.TryGetComponent<EnemyIdentifier>(out var enemyId)) return;

        if (LobbyController.IsOwner || enemyId.enemyType == EnemyType.Minos || enemyId.enemyType == EnemyType.Leviathan)
        {
            // get the PPP value if the player is not the host
            if (!LobbyController.IsOwner) float.TryParse(LobbyController.Lobby?.GetData("ppp"), out LobbyController.PPP);

            enemyId.ForceGetHealth(); // the health of the identifier changes, it's only an indicator of real health, so you can do whatever you want with it
            LobbyController.ScaleHealth(ref enemyId.health);

            // boss bar will do all the work
            if (__instance.healthLayers == null) return;

            if (__instance.healthLayers.Length == 0)
                __instance.healthLayers = new HealthLayer[] { new() { health = enemyId.health } };
            else
            {
                float sum = 0f; // sum of the health of all layers except the last one
                for (int i = 0; i < __instance.healthLayers.Length - 1; i++) sum += __instance.healthLayers[i].health;

                // change the health of the last bar so that it does not turn green
                __instance.healthLayers[__instance.healthLayers.Length - 1].health = enemyId.health - sum;
            }
        }
        else if (__instance.TryGetComponent<Enemy>(out var enemy)) __instance.healthLayers = enemy.haveSecondPhase
            ? new HealthLayer[] { new() { health = enemy.health.target / 2f }, new() { health = enemy.health.target / 2f } }
            : new HealthLayer[] { new() { health = enemy.health.target } };
    }
}

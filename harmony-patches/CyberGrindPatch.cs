namespace Jaket.HarmonyPatches;

using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

using Jaket.Net;
using Jaket.World;

#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable RCS1213 // Remove unused member declaration.

[HarmonyPatch(typeof(EndlessGrid), "LoadPattern")]
public class EndlessGridLoadPatternPatch
{
    static void Prefix(ref ArenaPattern pattern)
    {
        // load current pattern in client
        if (LobbyController.Lobby != null)
        {
            // send pattern if the player is the owner of the lobby
            if (LobbyController.IsOwner) CyberGrind.Instance.SendPattern(pattern);
            // replacing client pattern with server pattern on client
            else pattern = CyberGrind.Instance.CurrentPattern;
        }
    }
}

[HarmonyPatch(typeof(EndlessGrid), "Start")]
public class EndlessGridStartPatch
{
    static bool Prefix()
    {
        // getting cybergrind grid deathzone to change it later
        foreach (var deathZone in Resources.FindObjectsOfTypeAll<DeathZone>()) if (deathZone.name == "Cube") CyberGrind.Instance.GridDeathZoneInstance = deathZone;

        // don't skip original method
        return true;
    }

    // use postfix to wait object to initialize
    static void Postfix()
    {
        var cg = CyberGrind.Instance;
        // check when the player in a lobby 
        if (LobbyController.Lobby != null || LobbyController.IsOwner)
        {
            // check if current pattern is loaded and the player is the client
            if (cg.CurrentPattern != null && !LobbyController.IsOwner)
            {
                // sets as first time
                cg.LoadTimes = 0;
                // loads current pattern from the server
                cg.LoadCurrentPattern();
            }
            // send empty pattern when game starts and the player is the owner to prevent load previous cybergrind pattern
            else cg.SendPattern(new ArenaPattern());
        }
        else
        {
            // resetting values if the player is not in a lobby.
            cg.LoadTimes = 0;
            cg.CurrentWave = 0;
            cg.CurrentPattern = null;
        }
    }
}

[HarmonyPatch(typeof(EndlessGrid), "OnTriggerEnter")]
public class EndlessGridOnTriggerEnterPatch
{
    // dont allow to launch CyberGrind to client
    static bool Prefix(ref Text ___waveNumberText)
    {
        // for some reason, the wave number text is not shown on the client
        ___waveNumberText.transform.parent.parent.gameObject.SetActive(value: true);
        // don't activate trigger if the player is not the owner of the lobby
        if (LobbyController.Lobby != null && !LobbyController.IsOwner) return false;
        return true;
    }
}

[HarmonyPatch(typeof(EndlessGrid), "Update")]
public class EndlessGridUpdatePatch
{
    static bool Prefix(ref ActivateNextWave ___anw, EndlessGrid __instance)
    {
        // check if the player is not the owner of the lobby (client)
        if (LobbyController.Lobby != null && !LobbyController.IsOwner)
        {
            // set the current wave on the client to original cybergrind singleton to sync with the server
            __instance.currentWave = CyberGrind.Instance.CurrentWave;
            // set death enemies to prevent start new wave on the client to sync it
            ___anw.deadEnemies = -999;
        }
        return true;
    }

    // use postfix to change object after original object is changed
    static void Postfix(ref Text ___enemiesLeftText)
    {
        // check if the player is not the owner of the lobby (client)
        if (LobbyController.Lobby != null && !LobbyController.IsOwner)
        {
            // we broke original death enemies count, so we need to create new counter based on EnemyTracker
            var enemies = EnemyTracker.Instance.enemies;
            // remove dead enemies or null enemies from the list
            enemies.RemoveAll(e => e.dead || e is null);
            // set enemies left text on the client, replacing original
            ___enemiesLeftText.text = EnemyTracker.Instance.enemies.Count.ToString();
        }
        // change y position of cybergrind grid deathzone when lobby created or not to prevent enemies randomly dying
        var dz = CyberGrind.Instance.GridDeathZoneInstance;
        if (dz != null) dz.transform.position = dz.transform.position with { y = LobbyController.Lobby != null ? -10 : 0.5f };
    }
}
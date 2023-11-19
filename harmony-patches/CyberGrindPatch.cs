using HarmonyLib;
using Jaket.Net;
using Jaket.World;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Jaket.HarmonyPatches;

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
            if (LobbyController.IsOwner) CyberGrind.SendPattern(pattern);
            // replacing client pattern with server pattern on client
            else pattern = CyberGrind.CurrentPattern;
        }
    }
}

[HarmonyPatch(typeof(EndlessGrid), "Start")]
public class EndlessGridStartPatch
{
    static bool Prefix(EndlessGrid __instance)
    {
        // used to easily access the current endless grid instance
        CyberGrind.EndlessGridInstance = __instance;
        // change y position of death zones to prevent enemies randomly dying
        foreach (var deathZone in Resources.FindObjectsOfTypeAll<DeathZone>()) deathZone.transform.position = deathZone.transform.position with { y = deathZone.transform.position.y - 10 };
        // don't skip original method
        return true;
    }

    // use postfix to wait object to initialize
    static void Postfix()
    {
        // check when the player in a lobby 
        if (LobbyController.Lobby != null || LobbyController.IsOwner)
            // check if no current pattern is loaded and the player is the client
            if (CyberGrind.CurrentPattern != null && !LobbyController.IsOwner)
                // wait 1.5 seconds before loading the current pattern to in time to be downloaded from the server
                CyberGrind.Instance.Invoke("LoadCurrentPattern", 1.5f);
        else
        {
            // resetting values if the player is not in a lobby.
            CyberGrind.CurrentWave = 0;
            CyberGrind.CurrentPattern = null;
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
            __instance.currentWave = CyberGrind.CurrentWave;
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
            ___enemiesLeftText.text = string.Concat(EnemyTracker.Instance.enemies.Count);
        }
    }
}
namespace Jaket.Patches;

using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

using Jaket.Net;
using Jaket.World;

[HarmonyPatch(typeof(EndlessGrid))]
public class CyberGrindPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("LoadPattern")]
    static void Load(ref ArenaPattern pattern)
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

    // dont allow to launch CyberGrind to client
    [HarmonyPrefix]
    [HarmonyPatch("OnTriggerEnter")]
    static bool Enter(ref Text ___waveNumberText)
    {
        // for some reason, the wave number text is not shown on the client
        ___waveNumberText.transform.parent.parent.gameObject.SetActive(value: true);
        // don't activate trigger if the player is not the owner of the lobby
        return LobbyController.Lobby == null || LobbyController.IsOwner;
    }

    [HarmonyPostfix]
    [HarmonyPatch("Start")]
    static void Start()
    {
        // getting Cyber Grind deathzone to change it later
        foreach (var deathZone in Resources.FindObjectsOfTypeAll<DeathZone>()) if (deathZone.name == "Cube") CyberGrind.Instance.GridDeathZoneInstance = deathZone;

        var cg = CyberGrind.Instance;
        if (LobbyController.Lobby != null || LobbyController.IsOwner)
        {
            // sets as first time
            cg.LoadTimes = 0;
            // check if current pattern is loaded and the player is the client
            if (cg.CurrentPattern != null && !LobbyController.IsOwner)
                // loads current pattern from the server
                cg.LoadCurrentPattern();
            // send empty pattern when game starts and the player is the owner to prevent load previous cybergrind pattern
            else cg.SendPattern(new ArenaPattern()); // TODO looks like a crutch
        }
        else
        {
            // resetting values if the player is not in a lobby.
            cg.LoadTimes = 0;
            cg.CurrentWave = 0;
            cg.CurrentPattern = null;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("Update")]
    static bool PreUpdate(ref ActivateNextWave ___anw, EndlessGrid __instance)
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

    [HarmonyPostfix]
    [HarmonyPatch("Update")]
    static void PostUpdate(ref Text ___enemiesLeftText)
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

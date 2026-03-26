namespace Jaket.Net.Types;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.World;

/// <summary> Abstract entity of any enemy type. </summary>
public abstract class Enemy : OwnableEntity
{
    /// <summary> Whether the enemy is a boss. </summary>
    public bool IsBoss;
    /// <summary> Whether the enemy is blessed. </summary>
    public bool Blessed;

    public Enemy(uint id, EntityType type) : base(id, type) { }

    public void Heal() { } // TODO remake enemies (again)

    public abstract Transform WeakPoint { get; }

    #region harmony

    [HarmonyPatch(typeof(EnemyIdentifier), "Start")]
    [HarmonyPrefix]
    static bool Start(EnemyIdentifier __instance)
    {
        if (Gameflow.Mode.NoCommonEnemies() && !__instance.GetComponent<Agent>()) // TODO somehow skip it for wave enemies
        {
            Imdt(__instance.gameObject);
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(global::Enemy), nameof(global::Enemy.OnTravel))]
    [HarmonyPrefix]
    static bool Fraud(EnemyScript ___script) => ___script != null;

    #endregion
}

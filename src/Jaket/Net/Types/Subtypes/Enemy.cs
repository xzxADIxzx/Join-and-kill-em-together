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

    public Enemy(uint id, EntityType type) : base(id, type) { }

    public void Heal() { } // TODO remake enemies (again)

    public abstract Transform WeakPoint { get; }

    #region harmony

    [HarmonyPatch(typeof(EnemyIdentifier), "Start")]
    [HarmonyPrefix]
    static bool Start(EnemyIdentifier __instance)
    {
        if (Gameflow.Mode.NoCommonEnemies()) // TODO somehow skip it for wave enemies
        {
            Imdt(__instance.gameObject);
            return false;
        }
        return true;
    }

    #endregion
}

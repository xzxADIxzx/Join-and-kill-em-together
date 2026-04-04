namespace Jaket.Net.Types;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.World;

/// <summary> Abstract entity of any enemy type. </summary>
public abstract class Enemy : OwnableEntity
{
    Agent agent;
    Cache<RemotePlayer> player;
    EnemyIdentifier enemyId;
    global::Enemy enemy;
    BossHealthBar bossbar;

    /// <summary> Whether the enemy is a boss. </summary>
    public bool Boss;
    /// <summary> Whether the enemy is idoled. </summary>
    public bool Blessed => enemyId.Blessed;

    /// <summary> Initial health of the enemy. </summary>
    private float InitHealth;
    /// <summary> PostPPP health of the enemy. </summary>
    private float PostHealth;

    public Enemy(uint id, EntityType type) : base(id, type) { }

    #region logic

    public virtual Transform WeakPoint => enemyId.weakPoint?.transform ?? agent.transform;

    public virtual void Heal() => enemy.health = Mathf.Min(PostHealth, enemy.health + PostHealth / (LobbyController.Lobby?.MemberCount ?? 1f));

    public virtual float Rate(RemotePlayer target) => (target.Position - agent.Position).sqrMagnitude;

    public override void Assign(Agent agent)
    {
        (this.agent = agent).Patron = this;

        agent.Get(out enemyId);
        agent.Get(out enemy);
        agent.Get(out bossbar, true);

        OnTransfer = () =>
        {
            player = Owner;
            if (IsOwner)
                enemyId.target = EnemyTarget.TrackPlayer();
            else
                enemyId.target = player.Value?.Target;
        };

        Boss = bossbar; // non-owners will read this value to create a bossbar

        OnTransfer();
    }

    #endregion
    #region harmony

    [HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.Start))]
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

    [HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.UpdateTarget))]
    [HarmonyPrefix]
    static bool Focus() => false;

    [HarmonyPatch(typeof(global::Enemy), nameof(global::Enemy.OnTravel))]
    [HarmonyPrefix]
    static bool Fraud(EnemyScript ___script) => ___script != null;

    #endregion
}

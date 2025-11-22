namespace Jaket.Net.Types;

using HarmonyLib;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.IO;

/// <summary> Tangible entity of the coin type. </summary>
public class TeamCoin : OwnableEntity
{
    Agent agent;
    Float x, y, z;
    Cache<RemotePlayer> player;
    Team team => player.Value?.Team ?? Networking.LocalPlayer.Team;
    Rigidbody rb;
    Coin coin;
    Renderer[] rs;
    Collider[] cs;
    AudioSource source;

    public TeamCoin(uint id, EntityType type) : base(id, type) { }

    #region snapshot

    public override int BufferSize => 21;

    public override void Write(Writer w)
    {
        WriteOwner(ref w);

        if (IsOwner)
            w.Vector(agent.Position);
        else
            w.Floats(x, y, z);
    }

    public override void Read(Reader r)
    {
        if (ReadOwner(ref r)) return;

        r.Floats(ref x, ref y, ref z);
    }

    #endregion
    #region logic

    public override void Create() => Assign(Entities.Coins.Make(Type, new(x.Prev = x.Next, y.Prev = y.Next, z.Prev = z.Next)).AddComponent<Agent>());

    public override void Assign(Agent agent)
    {
        (this.agent = agent).Patron = this;

        agent.Get(out rb);
        agent.Get(out coin);
        agent.Get(out rs);
        agent.Get(out cs);
        agent.Get(out source);

        OnTransfer = () =>
        {
            player = Owner;

            rs.Each(r =>
            {
                if (r is TrailRenderer t)
                    t.startColor = team.Color() with { a = .4f };
                else
                {
                    r.material.mainTexture = ModAssets.CoinTexture;
                    r.material.color = team.Color();
                }
            });
        };

        Locked = false;

        OnTransfer();
    }

    public override void Update(float delta)
    {
        if (IsOwner) return;

        agent.Position = new(x.Get(delta), y.Get(delta), z.Get(delta));
    }

    public override void Damage(Reader r) { }

    public override void Killed(Reader r, int left)
    {
        // TODO a lotta work
    }

    public void Collide(Collision collision)
    {

    }

    public void Reflect(GameObject beam)
    {

    }

    public void Punch()
    {

    }

    public void Bounce()
    {

    }

    #endregion
    #region harmony

    [HarmonyPatch(typeof(Coin), "Start")]
    [HarmonyPrefix]
    static void Start(Coin __instance)
    {
        if (__instance) Entities.Coins.Sync(__instance.gameObject);
    }

    [HarmonyPatch(typeof(Coin), "OnCollisionEnter")]
    [HarmonyPrefix]
    static bool Collide(Coin __instance, Collision collision)
    {
        if (__instance.TryGetComponent(out Agent a) && a.Patron is TeamCoin c) c.Collide(collision);
        return false;
    }

    [HarmonyPatch(typeof(Coin), nameof(Coin.DelayedReflectRevolver))]
    [HarmonyPrefix]
    static bool Reflect(Coin __instance, GameObject beam)
    {
        if (__instance.TryGetComponent(out Agent a) && a.Patron is TeamCoin c) c.Reflect(beam);
        return false;
    }

    [HarmonyPatch(typeof(Coin), nameof(Coin.DelayedPunchflection))]
    [HarmonyPrefix]
    static bool Punch(Coin __instance)
    {
        if (__instance.TryGetComponent(out Agent a) && a.Patron is TeamCoin c) c.Punch();
        return false;
    }

    [HarmonyPatch(typeof(Coin), nameof(Coin.Bounce))]
    [HarmonyPrefix]
    static bool Bounce(Coin __instance)
    {
        if (__instance.TryGetComponent(out Agent a) && a.Patron is TeamCoin c) c.Bounce();
        return false;
    }

    #endregion
}

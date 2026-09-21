namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Input;
using Jaket.IO;
using Jaket.UI;

using static Jaket.UI.Lib.Pal;

/// <summary>
/// Singleton entity of the player type.
/// Responsible for the logical part of the player.
/// </summary>
public class LocalPlayer : Entity
{
    /// <summary> Singleton instance used for populating style packets. </summary>
    public Doll Doll = new();
    /// <inheritdoc cref="Doll.Team"/>
    public Team Team;
    /// <inheritdoc cref="RemotePlayer.Audio"/>
    public AudioSource[] Audio = [ null ];

    public LocalPlayer() : base(AccId = Tools.Tools.Id.AccountId, EntityType.Player) { }

    #region snapshot

    public override int BufferSize => 40;

    public override void Write(Writer w)
    {
        var nm = NewMovement.Instance;
        var cc = CameraController.Instance;
        var fc = FistControl.Instance;
        var ha = HookArm.Instance;

        w.Vector(nm.transform.position - nm.transform.up * (nm.sliding ? .375f : 1.5f));
        w.Vector(ha.state != HookState.Ready ? ha.hookPoint : Vector3.zero);

        w.Float(cc.rotationY);
        w.Float(cc.rotationX);

        w.Byte((byte)nm.hp);
        w.Byte((byte)Mathf.Floor(WeaponCharges.Instance.raicharge * 2f));

        w.State(Emotes.Current, Emotes.Rps, UI.Chat.Shown, cc.gravityRotation.eulerAngles);
        w.Bools
        (
            nm.walking,
            nm.sliding,
            !nm.gc.onGround,
            nm.gc.heavyFall,
            nm.ridingRocket,
            ha.state != HookState.Ready,
            fc.shopping
        );
    }

    public override void Read(Reader r) { }

    #endregion
    #region logic

    public override void Create() => Assign(Create<Agent>("Player"));

    public override void Assign(Agent agent)
    {
        agent.Patron = this;
        agent.Add(out Audio[0]);

        Events.OnLoad += () =>
        {
            agent.Run(Restyle, 1f);
            Recolor();
        };
        Events.OnHandChange += () =>
        {
            agent.Run(Restyle, 0f);
            Recolor();
        };
        Events.OnTeamChange += () => NewMovement.Instance.DefFind("Point Light").Get<Light>(l => l.color = LobbyController.Offline ? white : Team.Color());
    }

    public override void Update(float delta) { }

    public override void Damage(Reader r)
    {
        var nm = NewMovement.Instance;

        int damage = Mathf.CeilToInt(r.Float() * 3f);

        nm.GetHurt(damage, damage <= 3, ignoreInvincibility: damage >= 3);

        if (damage >= 4) nm.ForceAddAntiHP(damage * 2, true, true, true);

        if (Version.DEBUG) Log.Debug($"[ENTS] Received {damage} damage");
    }

    public override void Killed(Reader r, int left) { }

    #endregion
    #region other

    /// <summary> Restyles corresponding remote models. </summary>
    public void Restyle() => Networking.Send(PacketType.Style, 4 + Doll.BufferSize, w => { w.Id(Id); Doll.Write(w); });

    /// <summary> Recolors various first person models. </summary>
    public void Recolor()
    {
        var cw = GunControl.Instance.currentWeapon;
        var fc = FistControl.Instance;

        var main = cw?.DefChild(0).DefFind("RightArm");
        if (main) main.GetComponentInChildren<SkinnedMeshRenderer>().material.mainTexture = ModAssets.HandTexture(0);

        var feed = fc?.DefFind("Arm Blue(Clone)");
        if (feed) feed.GetComponentInChildren<SkinnedMeshRenderer>().material.mainTexture = ModAssets.HandTexture(1);

        var knkl = fc?.DefFind("Arm Red(Clone)");
        if (knkl) knkl.GetComponentInChildren<SkinnedMeshRenderer>().material.mainTexture = ModAssets.HandTexture(2);
    }

    #endregion
}

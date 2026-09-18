namespace Jaket.Net.Vendors;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net.Types;

using static Entities;

/// <summary> Vendor responsible for players. </summary>
public class Players : Vendor
{
    public override void Load()
    {
        Events.Post(() => ModAssets.Doll, () => Prefabs[(byte)EntityType.Player] = ModAssets.Doll);

        Fill<RemotePlayer>(EntityType.Player, EntityType.Player);
    }

    public override EntityType Type(GameObject obj) => EntityType.None;

    public override GameObject Make(EntityType type, Vector3 position = default, Transform parent = null)
    {
        if (!type.IsPlayer()) return null;

        var obj = Inst(Prefabs[(byte)type], parent);

        obj.Add<EnemyIdentifier>(e =>
        {
            e.enemyClass = EnemyClass.Machine;
            e.enemyType  = EnemyType.V2;
            e.dontUnlockBestiary = true;
            e.dontCountAsKills   = true;

            e.weaknesses      = [];
            e.burners         = [];
            e.flammables      = [];
            e.activateOnDeath = [];
        });
        obj.Add<global::Enemy>(_ => { });

        obj.DefChild(0).GetComponentsInChildren<Rigidbody>().Each(rb =>
        {
            rb.Add<EnemyIdentifierIdentifier>(_ => { });
            rb.tag = rb.tag switch
            {
                "RoomManager" => "Body",
                "Body"        => "Limb",
                "Forward"     => "Head",
                _             => rb.tag,
            };
        });
        obj.GetComponentsInChildren<Collider>().Each(c => c.Add<IgnoreDeathZones>(_ => { }));

        return obj;
    }

    public override void Sync(GameObject obj, params bool[] args) { }

    #region sounds

    public void Play(RemotePlayer player, byte type, Vector3 position, Vector3 rotation) => Events.Post(() =>
    {
        void Play(int channel, AudioClip clip, float pitch = 1f)
        {
            player.Voice[channel].clip = clip;
            player.Voice[channel].SetPitch(pitch);
            player.Voice[channel].Play(true);
        }

        var nm = NewMovement.Instance;
        var sh = SceneHelper.Instance;

        switch (type)
        {
            case 0x00: // jump
                Play(1, nm.jumpSound, 1.00f);
                break;
            case 0x01: // wall
                Play(1, nm.jumpSound, 1.25f);
                break;
            case 0x02: // wall
                Play(1, nm.jumpSound, 1.50f);
                break;
            case 0x03: // fail
                Play(1, nm.jumpSound, 1.75f);
                Play(2, nm.finalWallJump);
                break;
            case 0x04: // land - light
                Play(2, nm.landingSound);
                break;
            case 0x05: // land - heavy
                sh.CreateEnviroGibs(position, rotation, 5f, 05);
                Inst(Prefabs[(byte)EntityType.Heavywave], position).transform.forward = -rotation;
                break;
            case 0x06: // land - shock
                sh.CreateEnviroGibs(position, rotation, 5f, 10);
                Inst(Prefabs[(byte)EntityType.Shockwave], position).Get<PhysicalShockwave>(s =>
                {
                    s.force = 5000f * 2.25f * rotation.magnitude;
                    s.hasHurtPlayer = false;
                });
                break;
            case 0x07: // dash
                break;
            case 0x08: // punch - feedbacker
                break;
            case 0x09: // parry - feedbacker
                break;
            case 0x0A: // punch - knuckleblaster
                break;
            case 0x0B: // blast - knuckleblaster
                Inst(Prefabs[(byte)EntityType.Blastwave], position, rotation).GetComponentsInChildren<Explosion>().Each(e =>
                {
                    e.canHit = AffectedSubjects.All;
                    e.playerDamageOverride = player.Team.Ally() ? 0 : 12;
                });
                break;
            case 0x0C: // shotgun
                Inst(Prefabs[(byte)EntityType.ShotgunExplosion], position, rotation).GetComponentsInChildren<Explosion>().Each(e =>
                {
                    e.enemyDamageMultiplier = 1f;
                    e.damage = 50;
                    e.maxSize *= 1.5f;
                });
                break;
            default:   // hammer
                var tier = type >> 0 & 0x03;
                var chrg = type >> 2 & 0x03;
                var temp = chrg == 3 ? 0x01 : 0x00;

                Inst(Prefabs[(byte)EntityType.HammerParticleLight + tier], position, rotation);
                if (chrg == 0) break;
                Inst(Prefabs[(byte)EntityType.HammerExplosionCool + temp], position, rotation).GetComponentsInChildren<Explosion>().Each(e =>
                {
                    if (chrg <= 2)
                    {
                        e.canHit = AffectedSubjects.All;
                        e.playerDamageOverride = 0;
                    }
                    if (chrg == 2) e.maxSize *= 2f;
                });
                break;
        }
    });

    #endregion
}

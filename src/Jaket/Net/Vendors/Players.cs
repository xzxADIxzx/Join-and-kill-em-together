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
        obj.Add<Machine>(e => e.hurtSounds = []); // ???

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
}

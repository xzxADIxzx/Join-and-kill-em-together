namespace Jaket.Net.Vendors;

using HarmonyLib;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;

using static Entities;

/// <summary> Vendor responsible for hitscans. </summary>
public class Hitscans : Vendor
{
    public void Load()
    {
        EntityType counter = EntityType.Beam;
        GameAssets.Hitscans.Each(w =>
        {
            byte index = (byte)counter++;
            GameAssets.Prefab(w, p => Vendor.Prefabs[index] = p);
        });
    }

    public EntityType Type(GameObject obj) => EntityType.None; // TODO bruh

    public GameObject Make(EntityType type, Vector3 position = default, Transform parent = null)
    {
        if (!type.IsHitscan()) return null;

        var obj = Inst(Vendor.Prefabs[(byte)type], position);

        obj.name = "beam"; // this way the vendor won't be able to determine the type of the hitscan and thus oversync it

        return obj;
    }

    public GameObject Make(EntityType type, Vector3 position, Vector3 target, byte data)
    {
        var beam = Make(type, position)?.GetComponent<RevolverBeam>();
        if (beam == null) return null;

        if (type == EntityType.BeamReflected) beam.GetComponents<LineRenderer>().Each(r =>
        {
            r.startColor = r.endColor = ((Team)data).Color();
            r.SetPosition(0, position);
            r.SetPosition(1, target);
        });
        else
        {
            beam.noMuzzleflash = data == byte.MaxValue;
            beam.fake = true;
            beam.FakeShoot(target);
        }
        return beam.gameObject;
    }

    public void Sync(GameObject obj, params bool[] args)
    {
        var type = Type(obj);
        if (type == EntityType.None || !obj.TryGetComponent(out RevolverBeam beam)) return;

        Networking.Send(PacketType.Hitscan, 26, w =>
        {
            w.Enum(type);
            w.Vector(beam.transform.position);
            w.Vector(beam.alternateStartPoint);

            if (type == EntityType.BeamReflected)
                w.Byte((byte) beam.bodiesPierced);
            else
                w.Bool(args[0]);
        });
    }

    #region harmony

    [HarmonyPatch(typeof(RevolverBeam), "Start")]
    [HarmonyPostfix]
    static void Start(RevolverBeam __instance, LineRenderer ___lr)
    {
        __instance.alternateStartPoint = ___lr.GetPosition(1);
        Entities.Hitscans.Sync(__instance.gameObject, __instance.noMuzzleflash);
    }

    #endregion
}

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

    public EntityType Type(GameObject obj) => Vendor.Find
    (
        EntityType.Beam,
        EntityType.BeamHammer,
        p => p.name.Length == obj?.name.IndexOf('(') && (obj?.name.Contains(p.name) ?? false)
    );

    public GameObject Make(EntityType type, Vector3 position = default, Transform parent = null)
    {
        if (!type.IsHitscan()) return null;

        var obj = Inst(Vendor.Prefabs[(byte)type], position);

        obj.name = "beam"; // this way the vendor won't be able to determine the type of the hitscan and thus oversync it

        return obj;
    }

    public GameObject Make(EntityType type, Vector3 position, Vector3 target, bool wall, byte data)
    {
        var beam = Make(type, position)?.GetComponent<RevolverBeam>();
        if (beam == null) return null;

        if (wall && type != EntityType.BeamReflected) Inst(beam.hitParticle, target, Quaternion.LookRotation(position - target));

        beam.fake = true;
        beam.GetComponents<LineRenderer>().Each(r =>
        {
            r.SetPosition(0, position);
            r.SetPosition(1, target);

            if (type == EntityType.BeamReflected)
                r.startColor = r.endColor = ((Team)data).Color();

            else if (data == byte.MaxValue)
                r.transform.GetChild(0).gameObject.SetActive(false);

            else
            {
                r.GetComponentsInChildren<SpriteRenderer>().Each(c => c.gameObject.layer = 24); // outdoors
                r.transform.GetChild(0).LookAt(target);
            }
        });
        return beam.gameObject;
    }

    public void Sync(GameObject obj, params bool[] args)
    {
        var type = Type(obj);
        if (type == EntityType.None || !obj.TryGetComponent(out RevolverBeam beam)) return;

        // makes hitscans more visually appealing
        var correction = type <= EntityType.BeamExplosive && !args[0] ? beam.transform.forward * 1.2f : Vector3.zero;

        Networking.Send(PacketType.Hitscan, 26, w =>
        {
            w.Enum(type);
            w.Vector(beam.transform.position + correction);
            w.Vector(beam.alternateStartPoint);
            w.Bool(wall);

            if (type == EntityType.BeamReflected)
                w.Byte((byte) beam.bodiesPierced);
            else
                w.Bool(args[0]);
        });
    }

    #region harmony

    static bool wall;

    [HarmonyPatch(typeof(RevolverBeam), "Start")]
    [HarmonyPostfix]
    static void Start(RevolverBeam __instance, LineRenderer ___lr)
    {
        __instance.alternateStartPoint = ___lr.GetPosition(1);
        Entities.Hitscans.Sync(__instance.gameObject, __instance.noMuzzleflash);
        wall = false;
    }

    [HarmonyPatch(typeof(RevolverBeam), "Shoot")]
    [HarmonyPrefix]
    static void Hide() => Networking.Entities.Player(p => p.Team.Ally(), p => p.Doll.Root.gameObject.SetActive(false));

    [HarmonyPatch(typeof(RevolverBeam), "Shoot")]
    [HarmonyPostfix]
    static void Show() => Networking.Entities.Player(p => p.Team.Ally(), p => p.Doll.Root.gameObject.SetActive(true));

    [HarmonyPatch(typeof(RevolverBeam), "HitSomething")]
    [HarmonyPrefix]
    static void Wall(RaycastHit hit) => wall = !hit.transform.CompareTag("Coin");

    [HarmonyPatch(typeof(RevolverBeam), "PiercingShotCheck")]
    [HarmonyTranspiler]
    static Ins Wall(Ins instructions)
    {
        foreach (var ins in instructions)
        {
            if (ins.OperandIs(Field<RevolverBeam>("hitParticle")))
            {
                yield return new CodeInstruction(System.Reflection.Emit.OpCodes.Ldc_I4_1);
                yield return CodeInstruction.StoreField(typeof(Hitscans), "wall");
            }
            yield return ins;
        }
    }

    #endregion
}

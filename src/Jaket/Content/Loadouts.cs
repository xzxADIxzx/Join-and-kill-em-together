namespace Jaket.Content;

/// <summary> Set of loadouts used to correctly limit arsenal. </summary>
public static class Loadouts
{
    /// <summary> Empty variant with no weapons equipped. </summary>
    private static VariantSetting none => new()
    {
        blueVariant  = VariantOption.ForceOff,
        greenVariant = VariantOption.ForceOff,
        redVariant   = VariantOption.ForceOff,
    };
    /// <summary> Empty loadout with no weapons equipped. </summary>
    private static ForcedLoadout empty => new()
    {
        revolver       = none,
        altRevolver    = none,
        shotgun        = none,
        altShotgun     = none,
        nailgun        = none,
        altNailgun     = none,
        railcannon     = none,
        rocketLauncher = none,
    };

    /// <summary> Updates the current arsenal loadout. </summary>
    public static void Set(ForcedLoadout loadout)
    {
        GunSetter.Instance.forcedLoadout = loadout ?? new();
        GunSetter.Instance.ResetWeapons();

        FistControl.Instance.forcedLoadout = loadout ?? new();
        FistControl.Instance.ResetFists();
    }

    /// <summary> Makes a new loadout with or without arms equipped. </summary>
    public static ForcedLoadout Make(bool arms, Cons<ForcedLoadout> modifier = null)
    {
        var options = arms ? VariantOption.IfEquipped : VariantOption.ForceOff;
        var loadout = empty;

        loadout.arm = new() { blueVariant = options, redVariant = options, greenVariant = options };
        modifier?.Invoke(loadout);

        return loadout;
    }

    /// <summary> Merges two loadouts with equipped option priority. </summary>
    public static ForcedLoadout Merge(ForcedLoadout a, ForcedLoadout b)
    {
        static VariantOption  MergeOpt(VariantOption  a, VariantOption  b) => a == VariantOption.ForceOn || b == VariantOption.ForceOn
            ? VariantOption.ForceOn
            : VariantOption.ForceOff;
        static VariantSetting MergeSet(VariantSetting a, VariantSetting b) => new()
        {
            blueVariant    = MergeOpt(a.blueVariant   , b.blueVariant   ),
            greenVariant   = MergeOpt(a.greenVariant  , b.greenVariant  ),
            redVariant     = MergeOpt(a.redVariant    , b.redVariant    ),
        };
        return new()
        {
            revolver       = MergeSet(a.revolver      , b.revolver      ),
            altRevolver    = MergeSet(a.altRevolver   , b.altRevolver   ),
            shotgun        = MergeSet(a.shotgun       , b.shotgun       ),
            altShotgun     = MergeSet(a.altShotgun    , b.altShotgun    ),
            nailgun        = MergeSet(a.nailgun       , b.nailgun       ),
            altNailgun     = MergeSet(a.altNailgun    , b.altNailgun    ),
            railcannon     = MergeSet(a.railcannon    , b.railcannon    ),
            rocketLauncher = MergeSet(a.rocketLauncher, b.rocketLauncher),
            arm = new()
        };
    }
}

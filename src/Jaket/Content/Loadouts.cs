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
        loadout ??= new()
        {
            revolver       = new(),
            altRevolver    = new(),
            shotgun        = new(),
            altShotgun     = new(),
            nailgun        = new(),
            altNailgun     = new(),
            railcannon     = new(),
            rocketLauncher = new(),
            arm            = new(),
        };

        GunSetter.Instance.forcedLoadout = loadout;
        GunSetter.Instance.ResetWeapons();

        FistControl.Instance.forcedLoadout = loadout;
        FistControl.Instance.ResetFists();

        GunControl.Instance.YesWeapon();
        Events.OnHandChange.Fire();
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

    /// <summary> Makes a new loadout with a specified gun equipped. </summary>
    public static ForcedLoadout Make(bool arms, byte weapon) => weapon switch
    {
        0  => Make(arms, l => l.revolver      .blueVariant  = VariantOption.ForceOn),
        1  => Make(arms, l => l.revolver      .greenVariant = VariantOption.ForceOn),
        2  => Make(arms, l => l.revolver      .redVariant   = VariantOption.ForceOn),
        3  => Make(arms, l => l.altRevolver   .blueVariant  = VariantOption.ForceOn),
        4  => Make(arms, l => l.altRevolver   .greenVariant = VariantOption.ForceOn),
        5  => Make(arms, l => l.altRevolver   .redVariant   = VariantOption.ForceOn),

        6  => Make(arms, l => l.shotgun       .blueVariant  = VariantOption.ForceOn),
        7  => Make(arms, l => l.shotgun       .greenVariant = VariantOption.ForceOn),
        8  => Make(arms, l => l.shotgun       .redVariant   = VariantOption.ForceOn),
        9  => Make(arms, l => l.altShotgun    .blueVariant  = VariantOption.ForceOn),
        10 => Make(arms, l => l.altShotgun    .greenVariant = VariantOption.ForceOn),
        11 => Make(arms, l => l.altShotgun    .redVariant   = VariantOption.ForceOn),

        12 => Make(arms, l => l.nailgun       .blueVariant  = VariantOption.ForceOn),
        13 => Make(arms, l => l.nailgun       .greenVariant = VariantOption.ForceOn),
        14 => Make(arms, l => l.nailgun       .redVariant   = VariantOption.ForceOn),
        15 => Make(arms, l => l.altNailgun    .blueVariant  = VariantOption.ForceOn),
        16 => Make(arms, l => l.altNailgun    .greenVariant = VariantOption.ForceOn),
        17 => Make(arms, l => l.altNailgun    .redVariant   = VariantOption.ForceOn),

        18 => Make(arms, l => l.railcannon    .blueVariant  = VariantOption.ForceOn),
        19 => Make(arms, l => l.railcannon    .greenVariant = VariantOption.ForceOn),
        20 => Make(arms, l => l.railcannon    .redVariant   = VariantOption.ForceOn),
        21 => Make(arms, l => l.rocketLauncher.blueVariant  = VariantOption.ForceOn),
        22 => Make(arms, l => l.rocketLauncher.greenVariant = VariantOption.ForceOn),
        23 => Make(arms, l => l.rocketLauncher.redVariant   = VariantOption.ForceOn),
        _  => null
    };

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
            arm = a.arm
        };
    }
}

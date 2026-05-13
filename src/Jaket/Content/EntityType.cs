namespace Jaket.Content;

/// <summary> All entity types. Will replenish over time. </summary>
public enum EntityType : byte
{
    None   = 0xFF,
    Player = 0x00,
    Coin   = 0x01,

    #region enemeis

    Filth,
    Stray,
    Schism,
    Soldier,
    Hand,
    Corpse,
    Stalker,
    Insurrectionist,
    Ferryman,
    Reaper,

    Swordsmachine,
    Drone,
    Streetcleaner,
    V2st,
    Mindflayer,
    V2nd,
    Sentry,
    Gutterman,
    Guttertank,
    SecuritySystem,
    RocketLauncher,
    RocketLauncher1st,
    RocketLauncher2nd,
    Mortar,
    Mortar1st,
    Mortar2nd,
    Tower,
    Tower1st,
    Tower2nd,
    Brain,

    Malicious,
    Cerberus,
    Hideous,
    Idol,
    Leviathan,
    Mannequin,
    Minotaur,
    MinotaurInjured,
    Deathcatcher,
    Geryon,

    Gabriel1st,
    Virtue,
    Gabriel2nd,
    Providence,
    Power,

    Wicked,
    Rodent1st,
    Rodent2nd,
    Mandalore,
    Johninator,
    Puppet,

    FleshPrison,
    FleshPanopticon,
    FleshEye,
    FleshSkull,
    FleshCamera,
    Minos,
    Sisyphus,

    #endregion
    #region items

    SkullBlue,
    SkullRed,
    Soap,
    Torch,
    Moon,
    Florp,
    BaitApple,
    BaitFace,

    FishFunny,
    FishPitr,
    FishTrout,
    FishMetal,
    FishChomper,
    FishBomb,
    FishBall,
    FishFrog,
    FishDope,
    FishStick,
    FishCooked,
    FishShark,
    FishBurnt,

    Hakita,
    Pitr,
    Heckteck,
    Victoria,
    Hazeluff,
    CabalCrow,
    Lucas,
    Zombie,
    Francis,
    Jericho,
    BigRock,
    Mako,
    Rhiannon,
    FlyingDog,
    Samuel,
    Meganeko,
    KGC,
    Benjamin,
    Jake,
    John,
    Lizard,
    Vylet,
    Quetzal,
    Gianni,
    Weyte,
    Lenval,
    Kenna,
    Joy,
    Mandy,
    Cameron,
    Dalia,
    Tucker,
    Scott,
    Aaron,
    Salad,
    Jacob,
    Vvizard,
    V1,
    V2,
    V3,
    xzxADIxzx,
    Sowler,

    #endregion
    #region weapons

    RevolverBlue,
    RevolverBlueAlt,
    RevolverGreen,
    RevolverGreenAlt,
    RevolverRed,
    RevolverRedAlt,

    ShotgunBlue,
    ShotgunBlueAlt,
    ShotgunGreen,
    ShotgunGreenAlt,
    ShotgunRed,
    ShotgunRedAlt,

    NailgunBlue,
    NailgunBlueAlt,
    NailgunGreen,
    NailgunGreenAlt,
    NailgunRed,
    NailgunRedAlt,

    RailgunBlue,
    RailgunGreen,
    RailgunRed,

    RocketlBlue,
    RocketlGreen,
    RocketlRed,

    #endregion
    #region hitscans

    Beam,
    BeamSuper,
    BeamSharp,
    BeamAlt,
    BeamSuperAlt,
    BeamSharpAlt,
    BeamElectric,
    BeamExplosive,
    BeamReflected,
    BeamMalicious,
    BeamSentry,
    BeamGutter,
    BeamHammer,

    #endregion
    #region projectiles

    Shell,
    Core,
    NailCommon,
    NailFodder,
    NailHeated,
    SawbladeCommon,
    SawbladeFodder,
    SawbladeHeated,
    Magnet,
    Screwdriver,
    Rocket,
    Cannonball,
    ProjectileHell,
    ProjectileBeam,

    #endregion
    #region explosions

    Shockwave,
    Blastwave,
    ShotgunExplosion,
    HammerExplosionWeak,
    HammerExplosionWarm,
    HammerParticleLight,
    HammerParticleMedium,
    HammerParticleHeavy,

    #endregion
}

/// <summary> Set of different tools for working with types. </summary>
public static class EntityTypes
{
    /// <summary> Whether the type is an enemy.     </summary>
    public static bool IsEnemy     (this EntityType type) => type >= EntityType.Filth        && type <= EntityType.Sisyphus;

    /// <summary> Whether the type is an item.      </summary>
    public static bool IsItem      (this EntityType type) => type >= EntityType.SkullBlue    && type <= EntityType.Sowler;

    /// <summary> Whether the type is a fish.       </summary>
    public static bool IsFish      (this EntityType type) => type >= EntityType.FishFunny    && type <= EntityType.FishBurnt;

    /// <summary> Whether the type is a plushie.    </summary>
    public static bool IsPlushie   (this EntityType type) => type >= EntityType.Hakita       && type <= EntityType.Sowler;

    /// <summary> Whether the type is a weapon.     </summary>
    public static bool IsWeapon    (this EntityType type) => type >= EntityType.RevolverBlue && type <= EntityType.RocketlRed;

    /// <summary> Whether the type is a hitscan.    </summary>
    public static bool IsHitscan   (this EntityType type) => type >= EntityType.Beam         && type <= EntityType.BeamHammer;

    /// <summary> Whether the type is a projectile. </summary>
    public static bool IsProjectile(this EntityType type) => type >= EntityType.Shell        && type <= EntityType.ProjectileBeam;

    /// <summary> Whether the type is an explosion. </summary>
    public static bool IsExplosion (this EntityType type) => type >= EntityType.Shockwave    && type <= EntityType.HammerParticleHeavy;
}

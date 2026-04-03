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
    Mortar,
    Tower,
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
    Victoria,
    Heckteck,
    CabalCrow,
    Lucas,
    Zombie,
    Francis,
    Jericho,
    BigRock,
    Mako,
    FlyingDog,
    Samuel,
    Salad,
    Meganeko,
    KGC,
    Benjamin,
    Jake,
    John,
    Lizard,
    Quetzal,
    Gianni,
    Weyte,
    Lenval,
    Joy,
    Mandy,
    Cameron,
    Dalia,
    Tucker,
    Scott,
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

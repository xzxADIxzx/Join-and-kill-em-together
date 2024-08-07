namespace Jaket.Content;

/// <summary> All entity types. Will replenish over time. </summary>
public enum EntityType
{
    None = -1,
    Player,

    Filth,
    Stray,
    Schism,
    Soldier,
    TheCorpseOfKingMinos,
    Stalker,
    Insurrectionist,
    Ferryman,
    Swordsmachine,
    Drone,
    Streetcleaner,
    Mindflayer,
    V2_RedArm,
    V2_GreenArm,
    Sentry,
    Gutterman,
    Guttertank,
    MaliciousFace,
    Cerberus,
    HideousMass,
    Idol,
    Mannequin,
    Minotaur,
    Virtue,
    Gabriel,
    Gabriel_Angry,
    SomethingWicked,
    FleshPrison,
    FleshPrison_Eye,
    FleshPanopticon,
    FleshPanopticon_Eye,
    FleshPanopticon_Face,
    MinosPrime,
    SisyphusPrime,
    CancerousRodent,
    VeryCancerousRodent,
    Mandalore,
    Johninator,
    Puppet,
    Hand,
    Leviathan,
    Minotaur_Chase,
    SecuritySystem_Main,
    SecuritySystem_RocketLauncher, SecuritySystem_RocketLauncher_,
    SecuritySystem_Mortar, SecuritySystem_Mortar_,
    SecuritySystem_Tower, SecuritySystem_Tower_,
    Brain,

    BlueSkull,
    RedSkull,
    Soap,
    Torch,
    Florp,

    AppleBait,
    SkullBait,
    FunnyStupidFish,
    PitrFish,
    TroutFish,
    MetalFish,
    ChomperFish,
    BombFish,
    EyeballFish,
    FrogFish,
    DopeFish,
    StickFish,
    CookedFish,
    Shark,
    BurntStuff,

    Hakita,
    Pitr,
    Victoria,
    Heckteck,
    CabalCrow,
    Lucas,
    Francis,
    Jericho,
    BigRock,
    Mako,
    Samuel,
    Salad,
    Meganeko,
    KGC,
    BJ,
    Jake,
    John,
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

    Coin,
    Rocket,
    Ball,

    EnemyOffset = Filth,
    SecuritySystemOffset = SecuritySystem_Main,
    ItemOffset = BlueSkull,
    FishOffset = AppleBait,
    PlushieOffset = Hakita,
    BulletOffset = Coin
}

/// <summary> Extension class that allows you to get entity class. </summary>
public static class TypeExtensions
{
    /// <summary> Whether the type is an enemy. </summary>
    public static bool IsEnemy(this EntityType type) => type >= EntityType.EnemyOffset && type < EntityType.ItemOffset;

    /// <summary> Whether the type is a common enemy that can be spawned by the sandbox arm. </summary>
    public static bool IsCommonEnemy(this EntityType type) =>
        IsEnemy(type) && type < EntityType.Hand && type != EntityType.TheCorpseOfKingMinos && type != EntityType.SomethingWicked;

    /// <summary> Whether the type is a BIG enemy that can only be spawned in a limited number. </summary>
    public static bool IsBigEnemy(this EntityType type) => type >= EntityType.FleshPrison && type <= EntityType.SisyphusPrime;

    /// <summary> Whether the type is an enemy and can be shot by a coin. </summary>
    public static bool IsTargetable(this EntityType type) => IsEnemy(type) && type != EntityType.Idol && type != EntityType.CancerousRodent;

    /// <summary> Whether the type is an item. </summary>
    public static bool IsItem(this EntityType type) => type >= EntityType.ItemOffset && type < EntityType.FishOffset;

    /// <summary> Whether the type is a bait or fish. </summary>
    public static bool IsFish(this EntityType type) => type >= EntityType.FishOffset && type < EntityType.PlushieOffset;

    /// <summary> Whether the type is a plushie. </summary>
    public static bool IsPlushie(this EntityType type) => type >= EntityType.PlushieOffset && type < EntityType.BulletOffset;

    /// <summary> Whether the type is a bullet. </summary>
    public static bool IsBullet(this EntityType type) => type >= EntityType.BulletOffset;
}

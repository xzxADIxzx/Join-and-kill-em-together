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
    V2,
    Mindflayer,
    V2_GreenArm,
    Sentry,
    MaliciousFace,
    Cerberus,
    HideousMass,
    Idol,
    Gabriel,
    Virtue,
    Gabriel_Angry,
    SomethingWicked,
    CancerousRodent,
    VeryCancerousRodent,
    Mandalore,
    FleshPrison,
    FleshPrison_Eye,
    FleshPanopticon,
    FleshPanopticon_Eye,
    FleshPanopticon_Face,
    MinosPrime,
    SisyphusPrime,
    Hand,
    Leviathan,

    AppleBait,
    SkullBait,
    BlueSkull,
    RedSkull,
    Soap,
    Torch,
    Florp,

    Jacob,
    Mako,
    Jake,
    Dalia,
    Jericho,
    Meganeko,
    Tucker,
    BigRock,
    Dawg,
    Sam,
    Cameron,
    Gianni,
    Salad,
    Mandy,
    Joy,
    Weyte,
    Heckteck,
    Hakita,
    Lenval,
    CabalCrow,
    Quetzal,
    John,
    PITR,
    BJ,
    Francis,
    Vvizard,
    Lucas,
    Scott,
    KGC,
    V1,

    Rocket,
    Ball,

    EnemyOffset = Filth,
    ItemOffset = AppleBait,
    PlushyOffset = Jacob,
    BulletOffset = Rocket
}

/// <summary> Extension class that allows you to get entity class. </summary>
public static class TypeExtensions
{
    /// <summary> Whether the type is an enemy. </summary>
    public static bool IsEnemy(this EntityType type) => type >= EntityType.EnemyOffset && type < EntityType.ItemOffset;

    /// <summary> Whether the type is a common enemy that can be spawned by the sandbox arm. </summary>
    public static bool IsCommonEnemy(this EntityType type) =>
        IsEnemy(type) && type < EntityType.Hand && type != EntityType.TheCorpseOfKingMinos && type != EntityType.SomethingWicked;

    /// <summary> Whether the type is an item. </summary>
    public static bool IsItem(this EntityType type) => type >= EntityType.ItemOffset && type < EntityType.PlushyOffset;

    /// <summary> Whether the type is a plushy. </summary>
    public static bool IsPlushy(this EntityType type) => type >= EntityType.PlushyOffset && type < EntityType.BulletOffset;

    /// <summary> Whether the type is a bullet. </summary>
    public static bool IsBullet(this EntityType type) => type >= EntityType.BulletOffset;
}

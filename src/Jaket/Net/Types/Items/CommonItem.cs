namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;

/// <summary> Tangible entity of any item type, except books, plushies and fishes. </summary>
public class CommonItem : Item
{
    public CommonItem(uint id, EntityType type) : base(id, type) { }

    #region logic

    public override Vector3 HoldRotation => Type switch
    {
        EntityType.SkullBlue => new(-90f,   0f,  90f),
        EntityType.SkullRed  => new(-90f,   0f,  90f),
        EntityType.Soap      => new( 40f, -90f,  40f),
        EntityType.Torch     => new(-90f,   0f,   0f),
        EntityType.Moon      => new(-90f,   0f,   0f),
        EntityType.Florp     => new(-70f,   0f,  90f),
        EntityType.BaitApple => new(-90f,  20f,   0f),
        EntityType.BaitFace  => new( 20f,  70f, 180f),
        _ => default
    };

    #endregion
}

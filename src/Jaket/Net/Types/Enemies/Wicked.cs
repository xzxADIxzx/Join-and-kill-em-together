namespace Jaket.Net.Types;

using Jaket.Content;

/// <summary> Representation of Something Wicked. </summary>
public class Wicked : SimpleEnemy
{
    global::Wicked wicked;

    protected override void Awake()
    {
        Init(_ => Enemies.Type(EnemyId));
        InitTransfer();
        TryGetComponent(out wicked);
    }

    protected override void Start()
    {
        if (Tools.Scene == "Level 7-4") gameObject.SetActive(false);
        InvokeRepeating("UpdateTarget", .1f, .1f);
    }

    private void UpdateTarget() => Tools.Set("player", wicked, EnemyId.target?.targetTransform.gameObject);
}

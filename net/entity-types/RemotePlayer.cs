namespace Jaket.Net;

using System.IO;
using UnityEngine;

public class RemotePlayer : Entity
{
    // please don't ask me how I found this (4368)
    const string V2AssetKey = "cb3828ada2cbefe479fed3b51739edf6";

    /// <summary> Player doll animations. </summary>
    private Animator anim;

    /// <summary> Player position and rotation. </summary>
    private FloatLerp x, y, z, rotation;

    /// <summary> Animator states. </summary>
    private bool walking, sliding;

    /// <summary> Creates a new remote player doll. </summary>
    public static RemotePlayer CreatePlayer()
    {
        var prefab = AssetHelper.LoadPrefab(V2AssetKey);
        var obj = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity);

        return obj.AddComponent<RemotePlayer>();
    }

    public void Awake()
    {
        Type = Entities.Type.player;
        Owner = LobbyController.Lobby.Value.Owner.Id;

        anim = GetComponentInChildren<Animator>();
        x = new FloatLerp();
        y = new FloatLerp();
        z = new FloatLerp();
        rotation = new FloatLerp();

        GameObject.Destroy(gameObject.GetComponent<V2>()); // remove ai
        GameObject.Destroy(gameObject.GetComponentInChildren<V2AnimationController>());
    }

    public void Update()
    {
        // position
        transform.position = new Vector3(x.Get(LastUpdate), y.Get(LastUpdate) - (sliding ? 0f : 1.6f), z.Get(LastUpdate));
        transform.eulerAngles = new Vector3(0f, rotation.GetAngel(LastUpdate), 0f);

        // animation
        anim.SetBool("RunningBack", sliding);
        anim.SetBool("Sliding", sliding);
    }

    public override void Write(BinaryWriter w)
    {
        // position
        w.Write(transform.position.x);
        w.Write(transform.position.y);
        w.Write(transform.position.z);
        w.Write(transform.eulerAngles.y);

        // animation
        w.Write(walking);
        w.Write(sliding);
    }

    public override void Read(BinaryReader r)
    {
        LastUpdate = Time.time;

        // position
        x.Read(r);
        y.Read(r);
        z.Read(r);
        rotation.Read(r);

        // animation
        walking = r.ReadBoolean();
        sliding = r.ReadBoolean();
    }
}
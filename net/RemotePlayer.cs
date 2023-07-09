namespace Jaket.Net;

using System.IO;
using UnityEngine;

public class RemotePlayer : Entity
{
    // please don't ask me how I found this (4368)
    const string V2AssetKey = "cb3828ada2cbefe479fed3b51739edf6";

    /// <summary> Player position and rotation. </summary>
    private FloatLerp x, y, z, rotation;

    /// <summary> Creates a new remote player doll. </summary>
    public static RemotePlayer CreatePlayer()
    {
        var prefab = AssetHelper.LoadPrefab(V2AssetKey);
        var obj = GameObject.Instantiate(prefab, NewMovement.Instance.transform.position, Quaternion.identity);

        GameObject.Destroy(obj.GetComponent<V2>());
        GameObject.Destroy(obj.GetComponentInChildren<V2AnimationController>());

        return obj.AddComponent<RemotePlayer>();
    }

    public void Update()
    {
        transform.position = new Vector3(x.Get(LastUpdate), y.Get(LastUpdate), z.Get(LastUpdate));
        transform.eulerAngles = new Vector3(0f, rotation.GetAngel(LastUpdate), 0f);
    }

    public override void Write(BinaryWriter w)
    {
        w.Write(transform.position.x);
        w.Write(transform.position.y);
        w.Write(transform.position.z);
        w.Write(transform.eulerAngles.y);
    }

    public override void Read(BinaryReader r)
    {
        LastUpdate = Time.time;

        x.Read(r);
        y.Read(r);
        z.Read(r);
        rotation.Read(r);
    }
}
namespace Jaket.World;

using System;
using System.Collections.Generic;
using UnityEngine;

using Jaket.Assets;
using Jaket.Net;

/// <summary>
/// Component responsible for loading and unloading rooms.
/// Doors usually do this, but they don't take into account the presence of remote players in the room.
/// </summary>
public class RoomController : MonoBehaviour
{
    /// <summary> List of doors to this room. </summary>
    public List<Door> Doors = new();
    /// <summary> Number of players in the room. </summary>
    public int PlayersIn
    {
        get
        {
            int amount = 0;
            Networking.EachObserver(obs =>
            {
                if (obs.x > min.x && obs.y > min.y && obs.z > min.z &&
                    obs.x < max.x && obs.y < max.y && obs.z < max.z) amount++;
            });
            return amount;
        }
    }

    /// <summary> Whether this room is an exception. In this case, it won't be unloaded even when there are no players in it. </summary>
    private bool exception;
    /// <summary> Highest and lowest point of the room. </summary>
    private Vector3 min, max;

    /// <summary> Adds a controller to the room. </summary>
    public static void Build(Transform room)
    {
        // there must only be one controller in the room
        if (room.Find("Net") == null) Tools.Create<RoomController>("Net", room).Build();
    }

    private void Build()
    {
        // check for an exception
        if (Array.Exists(GameAssets.RoomExceptions, ex => ex == transform.parent.name)) { exception = true; return; }

        // find the highest and lowest point of the room
        void ExpandBounds(Vector3 pos)
        {
            if (pos == Vector3.zero) return;
            if (min == Vector3.zero || max == Vector3.zero)
            {
                min = max = pos;
                return;
            }

            if (pos.x < min.x) min.x = pos.x;
            if (pos.x > max.x) max.x = pos.x;

            if (pos.y < min.y) min.y = pos.y;
            if (pos.y > max.y) max.y = pos.y;

            if (pos.z < min.z) min.z = pos.z;
            if (pos.z > max.z) max.z = pos.z;
        }
        void ExpandBoundsRecursively(Transform transform, int layer)
        {
            if (++layer <= 4)
                foreach (Transform child in transform)
                    if (child.gameObject.activeSelf)
                    {
                        ExpandBounds(child.position);
                        ExpandBoundsRecursively(child, layer);
                    }
        }
        ExpandBoundsRecursively(transform.parent, 0);

        // push the boundaries a little
        min -= Vector3.one * 10f;
        max += Vector3.one * 10f;

        // find all of the doors to this room (they are always on the Invisible layer)
        var colliders = Physics.OverlapBox((min + max) / 2f, (max - min) / 2f, Quaternion.identity, 1 << 16, QueryTriggerInteraction.Collide);
        foreach (var col in colliders)
        {
            if (col.TryGetComponent<DoorController>(out var controller))
                Doors.Add(controller.transform.parent.GetComponentInChildren<Door>());

            else if (col.TryGetComponent<Door>(out var door)) Doors.Add(door);
        }
    }

    private void OnDisable()
    {
        if (LobbyController.Lobby == null) return;
        if (exception || PlayersIn > 0 || Doors.Exists(door => door != null && door.open)) Events.Post(() => transform.parent.gameObject.SetActive(true));
    }
}

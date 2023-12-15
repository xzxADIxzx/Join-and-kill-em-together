namespace Jaket.World;

using System.Collections.Generic;
using UnityEngine;

using Jaket.Net.Types;
using Jaket.UI;

/// <summary>
/// Component responsible for loading and unloading rooms.
/// Doors usually do this, but they don't take into account the presence of remote players in the room.
/// </summary>
public class RoomController : MonoBehaviour
{
    /// <summary> Number of players in the room. </summary>
    public int PlayersIn { private set; get; }
    /// <summary> List of doors to this room. </summary>
    public List<Door> Doors = new();

    /// <summary> Object containing immutable room parts such as walls. </summary>
    private Transform nonstuff;
    /// <summary> Highest and lowest point of the room. </summary>
    private Vector3 min, max;

    /// <summary> Adds a controller to the room. </summary>
    public static void Build(Transform room)
    {
        // there must only be one controller in the room
        if (room.Find("Net") != null) return;

        string index = room.name.Contains(" - ") ? room.name.Substring(0, room.name.IndexOf(" - ")) : "Nonstuff";
        UI.Component<RoomController>(UI.Object("Net", room), controller => controller.nonstuff = room.Find(index + " Nonstuff") ?? room);
    }

    private void Start()
    {
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
            if (++layer <= 3)
                foreach (Transform child in transform)
                    if (child.gameObject.activeSelf)
                    {
                        ExpandBounds(child.position);
                        ExpandBoundsRecursively(child, layer);
                    }
        }
        ExpandBoundsRecursively(nonstuff, 0);

        // add a trigger that will track the player’s presence in the room
        UI.Component<BoxCollider>(gameObject, box =>
        {
            var center = (min + max) / 2f;

            box.isTrigger = true;
            box.center = transform.InverseTransformPoint(center);
            box.size = max - min;

            // find all of the doors to this room (they are always on the Invisible layer)
            var colliders = Physics.OverlapBox(center, box.bounds.extents, Quaternion.identity, 1 << 16, QueryTriggerInteraction.Collide);
            foreach (var col in colliders)
            {
                if (!col.isTrigger) continue;
                else if (col.TryGetComponent<Door>(out var door)) Doors.Add(door);
                else if (col.TryGetComponent<DoorController>(out var controller))
                    Doors.Add(controller.transform.parent.gameObject.GetComponentInChildren<Door>());
            }
        });
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" || (other.name == "Net" && other.GetComponent<RemotePlayer>() != null)) PlayersIn++;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player" || (other.name == "Net" && other.GetComponent<RemotePlayer>() != null)) PlayersIn--;
    }

    private void OnDisable()
    {
        if (PlayersIn > 0 || Doors.Exists(door => door != null && door.open)) Events.Post(() => transform.parent.gameObject.SetActive(true));
    }
}

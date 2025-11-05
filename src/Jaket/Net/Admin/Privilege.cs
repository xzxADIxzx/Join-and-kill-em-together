namespace Jaket.Net.Admin;

using System.Collections.Generic;
using UnityEngine;

/// <summary> Keeps track of the privilege of fraud. </summary>
public struct Privilege
{
    /// <summary> Whether the subject has the privilege. </summary>
    private bool privileged;
    /// <summary> There's a short span after the privilege is revoked during which the fraud is still allowed. </summary>
    private float last;

    /// <summary> Whether the subject is allowed to engage in fraud. </summary>
    public readonly bool Has => privileged || Time.time - last < 3f;

    /// <summary> Updates the privilege according to the given list. </summary>
    public void Update(IEnumerable<string> list, uint id)
    {
        bool has = list.Any(s => s == id.ToString());
        if (!has && privileged) last = Time.time;
        privileged = has;
    }
}

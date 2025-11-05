namespace Jaket.Net.Admin;

using UnityEngine;

/// <summary> Keeps a specific action within the rate limit. </summary>
public struct Ratekeeper
{
    /// <summary> When the counter reaches the limit, a warning is given. </summary>
    private float limit;
    /// <summary> Frequency of actions up to which the counter doesn't increase. </summary>
    private float rate;

    /// <summary> Counter of specific actions. </summary>
    private float counter;
    /// <summary> Last access time to the counter. </summary>
    private float last;

    public Ratekeeper(float limit, float rate) { this.limit = limit; this.rate = rate; }

    /// <summary> Whether the action is kept within the rate limit. </summary>
    public bool Kept()
    {
        counter = 1f + Mathf.Clamp(counter - rate * (Time.time - last), 0f, limit);
        last = Time.time;
        return counter < limit;
    }

    /// <summary> Resets the counter. </summary>
    public void Reset() => counter = 0f;
}

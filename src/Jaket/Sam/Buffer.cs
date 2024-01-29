namespace Jaket.Sam;

using System.Collections.Generic;

/// <summary> Regular array of integers used to write and store output data. </summary>
public class Buffer
{
    /// <summary> List containing buffer data. </summary>
    private List<int> data = new();
    /// <summary> Last used index in time table. </summary>
    private int lastTimeTableIndex;

    /// <summary> Buffer cursor position. </summary>
    public int Position;

    /// <summary> Sets the data value at a given position. </summary>
    public void Set(int pos, int value)
    {
        // fill the list to the desired position with zeros
        while (pos >= data.Count) data.Add(0);
        data[pos] = value;
    }

    /// <summary> Writes the given array of 5 elements. </summary>
    public void WriteArray(int index, int[] array)
    {
        // move the cursor forward to a number from the table
        Position += Constants.TimeTable[lastTimeTableIndex, index];
        lastTimeTableIndex = index;

        // write given values to the buffer
        for (int i = 0; i < 5; i++) Set(Position / 50 + i, array[i]);
    }

    /// <summary> Writes the given value 5 times. </summary>
    public void Write(int index, int v) => WriteArray(index, new[] { v, v, v, v, v });

    /// <summary> Returns data from the buffer as an array of floats with values from -1 to 1. </summary>
    public float[] GetFloats()
    {
        float[] floats = new float[data.Count];
        for (int i = 0; i < data.Count; i++) floats[i] = (data[i] - 127) / 255f;

        return floats;
    }
}

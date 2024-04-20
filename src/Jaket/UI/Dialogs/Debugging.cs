namespace Jaket.UI.Dialogs;

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI.Extensions;

using Jaket.IO;

using static Pal;
using static Rect;

/// <summary> Tab containing different information about the load on the network. </summary>
public class Debugging : CanvasSingleton<Debugging>
{
    /// <summary> Graphs displaying different values related to the load on the network. </summary>
    private UILineRenderer readGraph, writeGraph, readTimeGraph, writeTimeGraph;
    /// <summary> Store values in the last 157 seconds. </summary>
    private Data read = new(), write = new(), readTime = new(), writeTime = new();

    private void Start()
    {
        Events.EverySecond += UpdateGraph;

        UIB.Table("Graph", transform, Msg(1888f) with { y = 144f, Height = 256f }, table =>
        {
            writeTimeGraph = UIB.Line("Write Time", table, Color.Lerp(orange, black, .2f));
            readTimeGraph = UIB.Line("Read Time", table, orange);
            writeGraph = UIB.Line("Write", table, Color.Lerp(green, black, .2f));
            readGraph = UIB.Line("Read", table, green);
        });
    }

    private void UpdateGraph()
    {
        if (!Shown) return;

        read.Enqueue(Stats.LastRead); readTime.Enqueue(Stats.ReadTime);
        write.Enqueue(Stats.LastWrite); writeTime.Enqueue(Stats.WriteTime);

        float peak = Mathf.Max(2048, read.Max(), write.Max());
        readGraph.Points = read.Project(peak);
        writeGraph.Points = write.Project(peak);

        peak = Mathf.Max(.1f, readTime.Max(), writeTime.Max());
        readTimeGraph.Points = readTime.Project(peak);
        writeTimeGraph.Points = writeTime.Project(peak);
    }

    /// <summary> Toggles visibility of the graph. </summary>
    public void Toggle() => gameObject.SetActive(Shown = !Shown);

    /// <summary> Constatnt size queue. </summary>
    private class Data : Queue<float>
    {
        public new void Enqueue(float value)
        {
            base.Enqueue(value);
            if (Count > 157) Dequeue();
        }

        public Vector2[] Project(float peak)
        {
            float x = -12f;
            return this.ToList().ConvertAll(v => new Vector2(x += 12f, v / peak * 240f)).ToArray();
        }
    }
}

namespace Jaket.UI.Fragments;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

using Jaket.IO;
using Jaket.UI.Lib;

using static Jaket.UI.Lib.Pal;

/// <summary> Fragment that provides access to networking statistics. </summary>
public class Debug : Fragment
{
    /// <summary> Warehouses containing a lot of diverse information. </summary>
    private Data readBs, sentBs, readMs, writeMs, entityMs, targetMs, totalMs, flushMs;

    public Debug(Transform root) : base(root, "Debug", false) => Component<Bar>(Rect("Content", new(0f, 190f, 1920f, 440f, new(.5f, 0f))).gameObject, b =>
    {
        b.Setup(true, 16f, 16f);
        b.Subbar(136f, s =>
        {
            s.Setup(false, 0f, 16f);

            Component<Bar>(s.Image(Tex.Fill, 320f, semi, multiplier: 2).gameObject, b =>
            {
                b.Setup(true);
                b.Text("BYTES READ   ", 24f, out (readBs   = new(20)).Label, color: green);
                b.Text("BYTES SENT   ", 24f, out (sentBs   = new(20)).Label, color: Darker(green));
                b.Text("READ TIME    ", 24f, out (readMs   = new(97)).Label, color: orange);
                b.Text("WRITE TIME   ", 24f, out (writeMs  = new(97)).Label, color: Darker(orange));
            });
            Component<Bar>(s.Image(Tex.Fill, 320f, semi, multiplier: 2).gameObject, b =>
            {
                b.Setup(true);
                b.Text("ENTITY UPDATE", 24f, out (entityMs = new(97)).Label, color: blue);
                b.Text("TARGET UPDATE", 24f, out (targetMs = new(97)).Label, color: Darker(blue));
                b.Text("TOTAL TIME   ", 24f, out (totalMs  = new(97)).Label, color: purple);
                b.Text("FLUSH TIME   ", 24f, out (flushMs  = new(97)).Label, color: Darker(purple));
            });
        });
        b.Subbar(256f, s =>
        {
            s.Setup(false, 0f, 16f);

            void Build(Image graph, Data data) => Component<UILineRenderer>(Builder.Rect("Graph", graph.transform, new(8f, 8f, 0f, 0f, new())).gameObject, g => data.Graph = g);
            var byteGraph = s.Image(Tex.Fill, 320f,  semi, multiplier: 2);
            var timeGraph = s.Image(Tex.Fill, 1552f, semi, multiplier: 2);

            Build(byteGraph, readBs);
            Build(byteGraph, sentBs);
            Build(timeGraph, readMs);
            Build(timeGraph, writeMs);
            Build(timeGraph, entityMs);
            Build(timeGraph, targetMs);
            Build(timeGraph, totalMs);
            Build(timeGraph, flushMs);
        });
    });

    public override void Toggle()
    {
        base.Toggle();
        UI.Hide(UI.LeftGroup, this, () => { });
    }

    public override void Rebuild()
    {
        readBs  .Enqueue(Stats.ReadBs,   v => $"{v}bs");
        sentBs  .Enqueue(Stats.WriteBs,  v => $"{v}bs");
        readMs  .Enqueue(Stats.ReadMs,   v => $"{v:0.000}ms");
        writeMs .Enqueue(Stats.WriteMs,  v => $"{v:0.000}ms");
        entityMs.Enqueue(Stats.EntityMs, v => $"{v:0.000}ms");
        targetMs.Enqueue(Stats.TargetMs, v => $"{v:0.000}ms");
        totalMs .Enqueue(Stats.TotalMs,  v => $"{v:0.000}ms");
        flushMs .Enqueue(Stats.FlushMs,  v => $"{v:0.000}ms");

        if (!Shown) return;
        float bytePeak = Mathf.Max(8192f, readBs.Peak(), sentBs.Peak());
        float timePeak = totalMs.Peak();

        readBs  .Project(bytePeak);
        sentBs  .Project(bytePeak);
        readMs  .Project(timePeak);
        writeMs .Project(timePeak);
        entityMs.Project(timePeak);
        targetMs.Project(timePeak);
        totalMs .Project(timePeak);
        flushMs .Project(timePeak);
    }

    /// <summary> Data warehouse that can be projected onto a graph. </summary>
    public class Data
    {
        /// <summary> Array containing data to be stored. </summary>
        private float[] data;
        /// <summary> Index of the start of the sequence. </summary>
        private int start;

        /// <summary> Label displaying the current value. </summary>
        public Text Label;
        /// <summary> Graph to project the data onto. </summary>
        public UILineRenderer Graph;

        public Data(int size) => data = new float[size];

        /// <summary> Adds the given value to the end of the sequence. </summary>
        public void Enqueue(float value, Func<float, string> format)
        {
            data[start] = value;
            start = (start + 1) % data.Length;
            Label.text = format(value);
        }

        /// <summary> Projects the stored data onto the graph. </summary>
        public void Project(float peak)
        {
            var x = -16f;
            var o = new Vector2[data.Length];
            for (int i = 0; i < data.Length; i++) o[i] = new(x += 16f, data[(start + i) % data.Length] / peak * 240f);

            Graph.color = Label.color;
            Graph.Points = o;
        }

        /// <summary> Returns the peak value of the sequence. </summary>
        public float Peak()
        {
            float max = 0f;
            for (int i = 0; i < data.Length; i++) if (data[i] > max) max = data[i];
            return max;
        }
    }
}

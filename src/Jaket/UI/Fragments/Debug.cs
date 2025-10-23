namespace Jaket.UI.Fragments;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

using Jaket.Assets;
using Jaket.IO;
using Jaket.Net;
using Jaket.UI.Lib;

using static Jaket.UI.Lib.Pal;

/// <summary> Fragment that provides access to networking statistics. </summary>
public class Debug : Fragment
{
    static Transform cc => CameraController.Instance.transform;

    /// <summary> Warehouses containing a lot of diverse information. </summary>
    private Data readBs, sentBs, readMs, writeMs, entityMs, targetMs, totalMs, flushMs;
    /// <summary> Labels displaying diverse information about network. </summary>
    private Text entities, players, isowner, loading;

    public Debug(Transform root) : base(root, "Debug", false) => Component<Bar>(Rect("Content", new(0f, 220f, 1920f, 440f, new(.5f, 0f))).gameObject, b =>
    {
        b.Setup(true, 16f, 16f);
        b.Subbar(136f, s =>
        {
            s.Setup(false, 0f, 16f);

            Component<Bar>(s.Image(Tex.Fill, 320f, semi, multiplier: 3f).gameObject, b =>
            {
                b.Setup(true);
                b.Text("BYTES READ   ", 24f, out (readBs   = new(20)).Label, color: green);
                b.Text("BYTES SENT   ", 24f, out (sentBs   = new(20)).Label, color: Darker(green));
                b.Text("READ TIME    ", 24f, out (readMs   = new(97)).Label, color: orange);
                b.Text("WRITE TIME   ", 24f, out (writeMs  = new(97)).Label, color: Darker(orange));
            });
            Component<Bar>(s.Image(Tex.Fill, 320f, semi, multiplier: 3f).gameObject, b =>
            {
                b.Setup(true);
                b.Text("ENTITY UPDATE", 24f, out (entityMs = new(97)).Label, color: blue);
                b.Text("TARGET UPDATE", 24f, out (targetMs = new(97)).Label, color: Darker(blue));
                b.Text("TOTAL TIME   ", 24f, out (totalMs  = new(97)).Label, color: purple);
                b.Text("FLUSH TIME   ", 24f, out (flushMs  = new(97)).Label, color: Darker(purple));
            });
            Component<Bar>(s.Image(Tex.Fill, 320f, semi, multiplier: 3f).gameObject, b =>
            {
                b.Setup(true);
                b.Text("ENTITIES     ", 24f, out entities);
                b.Text("PLAYERS      ", 24f, out players);
                b.Text("IS OWNER     ", 24f, out isowner);
                b.Text("LOADING      ", 24f, out loading);
            });
        });
        b.Subbar(256f, s =>
        {
            s.Setup(false, 0f, 16f);

            void Build(Image graph, Data data) => Component<UILineRenderer>(Builder.Rect("Graph", graph.transform, new(8f, 8f, 0f, 0f, new())).gameObject, g => data.Graph = g);
            var byteGraph = s.Image(Tex.Fill, 320f,  semi, multiplier: 3f);
            var timeGraph = s.Image(Tex.Fill, 1552f, semi, multiplier: 3f);

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
        UI.Hide(UI.LeftGroup, this, null);
    }

    public override void Rebuild()
    {
        readBs  .Enqueue(Stats.ReadBs,   v => $"{v}bs");
        sentBs  .Enqueue(Stats.SentBs,   v => $"{v}bs");
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

        entities.text = Bundle.Parse($"{Networking.Entities.Count(e => !e.Hidden)}[light]/{Networking.Entities.Count()}");
        players.text  = Bundle.Parse($"{Networking.Connections.Count()}[light]/{LobbyController.Lobby?.MemberCount ?? 0}");
        isowner.text  = LobbyController.IsOwner.ToString().ToUpper();
        isowner.color = LobbyController.IsOwner ? green : red;
        loading.text  = Networking.Loading.ToString().ToUpper();
        loading.color = Networking.Loading ? green : red;
    }

    public void Clear() => new Data[] { readBs, sentBs, readMs, writeMs, entityMs, targetMs, totalMs, flushMs }.Each(d => d.Clear());

    public void Raycast()
    {
        if (Physics.Raycast(cc.position, cc.forward, out var hit, float.MaxValue, EnvMask | 0x400000))
        {
            var agent = hit.collider.GetComponentInParent<Entity.Agent>();
            if (agent)
                Log.Debug($"[ENTS] Caught an entity of {agent.Patron.Type} type");
            else
                Log.Debug($"[ENTS] Couldn't catch an entity of any kind");
        }
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

        /// <summary> Puts the value into the sequence. </summary>
        public void Enqueue(float value, Func<float, string> format)
        {
            data[start] = value;
            start = (start + 1) % data.Length;
            Label.text = format(value);
        }

        /// <summary> Returns value at the given index. </summary>
        public float At(int index) => data[(start + index) % data.Length];

        /// <summary> Projects the data onto the graph. </summary>
        public void Project(float peak)
        {
            var x = -16f;
            var o = new Vector2[data.Length];
            for (int i = 0; i < data.Length; i++) o[i] = new(x += 16f, At(i) / peak * 240f);

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

        /// <summary> Completely erases all of the stored data. </summary>
        public void Clear() => data.Clear();
    }
}

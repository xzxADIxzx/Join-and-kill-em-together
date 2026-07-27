namespace Jaket.UI.Fragments;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

using Jaket.Assets;
using Jaket.IO;
using Jaket.Net;
using Jaket.UI.Lib;
using Jaket.World;

using static Jaket.UI.Lib.Pal;

/// <summary> Fragment that provides access to network statistics. </summary>
public class Debug : Fragment
{
    /// <summary> Graphs. </summary>
    private Data[] data = { new(20), new(20), new(97), new(97), new(97), new(97), new(97), new(97) };
    /// <summary> Labels. </summary>
    private Text entities, players, isowner, loading, gamemode, isactive, islocked;

    public Debug(Transform root) : base(root, "Debug", false)
    {
        Events.EveryTick += () =>
        {
            if (Stats.Subticks < Networking.TICKS_PER_SECOND * Networking.SUBTICKS_PER_TICK) return;

            data[0].Enqueue(Stats.Received);
            data[1].Enqueue(Stats.Sent);
            data[2].Enqueue(Stats.Millis(Stats.Read));
            data[3].Enqueue(Stats.Millis(Stats.Write));
            data[4].Enqueue(Stats.Millis(Stats.Entity));
            data[5].Enqueue(Stats.Millis(Stats.Common));
            data[6].Enqueue(Stats.Millis(Stats.Thread));
            data[7].Enqueue(Stats.Millis(Stats.Jitter));

            if (Shown) Events.Post(Rebuild);
        };

        Rect("Display", new()).Component<Bar>(b =>
        {
            var mark = Builder.Image(Builder.Rect("Mark", b, new(16f, 16f)), Tex.Mark, white);
            var text = Builder.Text (Builder.Rect("Text", b, new(        )), "hi", 24, white);

            b.Update(() =>
            {
                Entity best = null;
                Vector2 pos = default;

                Networking.Entities.Each(e => e.Debuggable, e =>
                {
                    var cen = CameraController.Instance.cam.pixelRect.size / 2f;
                    var scr = CameraController.Instance.cam.WorldToScreenPoint(e.DrawPos);

                    if (scr.z > 0f && Vector2.Distance(scr, cen) < Vector2.Distance(pos, cen))
                    {
                        best = e;
                        pos = scr;
                    }
                });

                bool found = best != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(b.transform as RectTransform, pos, null, out pos);

                mark.enabled = found;
                text.enabled = found;
                mark.rectTransform.anchoredPosition = pos;
                text.rectTransform.anchoredPosition = pos + Vector2.down * 16f;

                if (found) text.text = best.Type.ToString();
            });
        });

        Rect("Content", new(0f, 220f, 1920f, 440f, new(.5f, 0f))).Component<Bar>(b =>
        {
            b.Setup(true, 16f, 16f);
            b.Subbar(136f, s =>
            {
                s.Setup(false, 0f, 16f);
                s.Image(Tex.Fill, 320f, semi, scale: 3f).Component<Bar>(b =>
                {
                    b.Setup(true);
                    b.Pair("RECEIVED ", out data[0].Label, green        );
                    b.Pair("SENT     ", out data[1].Label, green.Darker );
                    b.Pair("READ     ", out data[2].Label, orange       );
                    b.Pair("WRITE    ", out data[3].Label, orange.Darker);
                });
                s.Image(Tex.Fill, 320f, semi, scale: 3f).Component<Bar>(b =>
                {
                    b.Setup(true);
                    b.Pair("ENTITY   ", out data[4].Label, blue         );
                    b.Pair("COMMON   ", out data[5].Label, blue.Darker  );
                    b.Pair("THREAD   ", out data[6].Label, purple       );
                    b.Pair("JITTER   ", out data[7].Label, purple.Darker);
                });
                s.Image(Tex.Fill, 320f, semi, scale: 3f).Component<Bar>(b =>
                {
                    b.Setup(true);
                    b.Pair("ENTITIES ", out entities);
                    b.Pair("PLAYERS  ", out players);
                    b.Pair("IS OWNER ", out isowner);
                    b.Pair("LOADING  ", out loading);
                });
                s.Image(Tex.Fill, 320f, semi, scale: 3f).Component<Bar>(b =>
                {
                    b.Setup(true);
                    b.Pair("GAMEMODE ", out gamemode);
                    b.Pair("IS ACITVE", out isactive);
                    b.Pair("IS LOCKED", out islocked);
                });
            });
            b.Subbar(256f, s =>
            {
                s.Setup(false, 0f, 16f);

                var byteGraph = s.Image(Tex.Fill,  320f, semi, scale: 3f);
                var timeGraph = s.Image(Tex.Fill, 1552f, semi, scale: 3f);

                for (int i = 0; i < 2; i++) Builder.Rect("Graph", byteGraph, new(8f, 8f, 0f, 0f, new())).Component<UILineRenderer>(g => data[i].Graph = g);
                for (int i = 2; i < 8; i++) Builder.Rect("Graph", timeGraph, new(8f, 8f, 0f, 0f, new())).Component<UILineRenderer>(g => data[i].Graph = g);
            });
        });
    }

    public override void Toggle()
    {
        base.Toggle();
        UI.Hide(UI.LeftGroup, this, Rebuild);
    }

    public override void Rebuild()
    {
        for (int i = 0; i < 2; i++) data[i].Label.text = $"{data[i][19]      }bs";
        for (int i = 2; i < 8; i++) data[i].Label.text = $"{data[i][96]:0.000}ms";

        for (int i = 0; i < 2; i++) data[i].Project(8192f);
        for (int i = 2; i < 8; i++) data[i].Project(8.33f);

        entities.text  = Bundle.Parse($"{Networking.Entities.Count(e => !e.Hidden)}[light]/{Networking.Entities.Count()}");
        players.text   = Bundle.Parse($"{Networking.Connections.Count()}[light]/{LobbyController.Lobby?.MemberCount ?? 0}");
        isowner.text   = LobbyController.IsOwner.ToString().ToUpper();
        isowner.color  = LobbyController.IsOwner ? green : red;
        loading.text   = Networking.Loading.ToString().ToUpper();
        loading.color  = Networking.Loading ? green : red;
        gamemode.text  = Gameflow.Mode.ToString();
        isactive.text  = Gameflow.Active.ToString().ToUpper();
        isactive.color = Gameflow.Active ? green : red;
        islocked.text  = Gameflow.LockRespawn.ToString().ToUpper();
        islocked.color = Gameflow.LockRespawn ? green : red;
    }

    /// <summary> Data warehouse that can be projected onto a graph. </summary>
    public class Data(int size)
    {
        /// <summary> Array containing data to be stored. </summary>
        private float[] data = new float[size];
        /// <summary> Index of the start of the sequence. </summary>
        private int start;

        /// <summary> Label displaying the current value. </summary>
        public Text Label;
        /// <summary> Graph to project the data onto. </summary>
        public UILineRenderer Graph;

        public float this[int index] => data[(start + index) % data.Length];

        /// <summary> Puts the value into the sequence. </summary>
        public void Enqueue(float value)
        {
            data[start] = value;
            start = (start + 1) % data.Length;
        }

        /// <summary> Projects the data onto the graph. </summary>
        public void Project(float peak)
        {
            var o = new Vector2[data.Length];
            for (int i = 0; i < data.Length; i++) o[i] = new(i * 16f, Mathf.Min(this[i], peak) / peak * 240f);

            Graph.color = Label.color;
            Graph.Points = o;
        }
    }
}

namespace Jaket.UI.Dialogs;

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

using Jaket.IO;
using Jaket.Net;

using static Pal;
using static Rect;

/// <summary> Tab containing different information about the load on the network. </summary>
public class Debugging : CanvasSingleton<Debugging>
{
    /// <summary> Data arrays containing values of the last 157 seconds. </summary>
    private Data read = new(), write = new(), readTime = new(), writeTime = new(), entity = new(), target = new();
    /// <summary> Text fields containing diverse info about the network state. </summary>
    private Text readText, writeText, readTimeText, writeTimeText, entityText, targetText, entities, owner, loading, impact;

    private void Start()
    {
        Events.OnLobbyAction += () =>
        {
            if (LobbyController.Offline)
                readText.text = writeText.text = readTimeText.text = writeTimeText.text =
                entityText.text = targetText.text =
                entities.text = owner.text = loading.text = impact.text = "-";
        };
        Events.EverySecond += UpdateGraph;

        Text DoubleText(Transform table, string name, float y, Color? color = null)
        {
            UIB.Text(name, table, Btn(y), color, align: TextAnchor.MiddleLeft);
            return UIB.Text("-", table, Btn(y), color, align: TextAnchor.MiddleRight);
        }

        UIB.Table("Graph", transform, Msg(1888f) with { y = 114f, Height = 196f }, table =>
        {
            target.Graph = UIB.Line("Target Update", table, Dark(blue));
            entity.Graph = UIB.Line("Entity Update", table, blue);
            writeTime.Graph = UIB.Line("Write Time", table, Dark(orange));
            readTime.Graph = UIB.Line("Read Time", table, orange);
            write.Graph = UIB.Line("Write", table, Dark(green));
            read.Graph = UIB.Line("Read", table, green);
        });
        UIB.Table("Stats", transform, Deb(0), table =>
        {
            readText = DoubleText(table, "READ:", 20f, green);
            writeText = DoubleText(table, "WRITE:", 52f, Dark(green));
            readTimeText = DoubleText(table, "READ TIME:", 84f, orange);
            writeTimeText = DoubleText(table, "WRITE TIME:", 116f, Dark(orange));
        });
        UIB.Table("Also Stats", transform, Deb(1), table =>
        {
            entityText = DoubleText(table, "ENTITY UPDATE:", 20f, blue);
            targetText = DoubleText(table, "TARGET UPDATE:", 52f, Dark(blue));
        });
        UIB.Table("Networking", transform, Deb(2), table =>
        {
            entities = DoubleText(table, "ENTITIES:", 20f);
            owner = DoubleText(table, "IS OWNER:", 52f);
            loading = DoubleText(table, "LOADING:", 84f);
            impact = DoubleText(table, "IMPACT ON FPS:", 116f, red);
        });
    }

    private void UpdateGraph()
    {
        if (!Shown) return;

        #region graph

        read.Enqueue(Stats.LastRead); readTime.Enqueue(Stats.LastReadTime); entity.Enqueue(Stats.LastEntityUpdate);
        write.Enqueue(Stats.LastWrite); writeTime.Enqueue(Stats.LastWriteTime); target.Enqueue(Stats.LastTargetUpdate);

        float peak = Mathf.Max(2048, read.Max(), write.Max());
        read.Project(peak);
        write.Project(peak);

        peak = Mathf.Max(.1f, readTime.Max(), writeTime.Max());
        readTime.Project(peak);
        writeTime.Project(peak);

        peak = Mathf.Max(.1f, entity.Max(), target.Max());
        entity.Project(peak);
        target.Project(peak);

        #endregion

        if (LobbyController.Offline) return;

        #region stats

        readText.text = $"{Stats.LastRead}b/s";
        writeText.text = $"{Stats.LastWrite}b/s";
        readTimeText.text = $"{Stats.LastReadTime:0.0000}ms";
        writeTimeText.text = $"{Stats.LastWriteTime:0.0000}ms";
        entityText.text = $"{Stats.LastEntityUpdate:0.0000}ms";
        targetText.text = $"{Stats.LastTargetUpdate:0.0000}ms";

        #endregion
        #region networking

        entities.text = $"{Networking.Entities.Count(p => p.Value && !p.Value.Dead)}<color=#BBBBBB>/{Networking.Entities.Count}</color>";
        owner.text = LobbyController.IsOwner.ToString().ToUpper();
        owner.color = LobbyController.IsOwner ? green : red;
        loading.text = Networking.Loading.ToString().ToUpper();
        loading.color = Networking.Loading ? green : red;
        impact.text = $"{(Stats.LastReadTime + Stats.LastWriteTime + Stats.LastEntityUpdate + Stats.LastTargetUpdate) * .1f:0.00}%";

        #endregion
    }

    /// <summary> Toggles visibility of the graph. </summary>
    public void Toggle() => gameObject.SetActive(Shown = !Shown);

    /// <summary> Clears the graph. </summary>
    public void Clear() { read.Clear(); write.Clear(); readTime.Clear(); writeTime.Clear(); }

    /// <summary> Constatnt size queue. </summary>
    private class Data : Queue<float>
    {
        /// <summary> Graph displaying one type of values related to the load on the network. </summary>
        public UILineRenderer Graph;

        public new void Enqueue(float value)
        {
            base.Enqueue(value);
            if (Count > 157) Dequeue();
        }

        public void Project(float peak)
        {
            float x = -12f;
            Graph.Points = this.ToList().ConvertAll(v => new Vector2(x += 12f, v / peak * 180f)).ToArray();
        }
    }
}

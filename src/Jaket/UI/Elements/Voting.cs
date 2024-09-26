namespace Jaket.UI.Elements;

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Random = System.Random;

using Jaket.Assets;
using Jaket.IO;
using Jaket.Net;
using Jaket.World;

using static Rect;

/// <summary> Object initializing voting and completing it after some time or if a sufficient number of votes was reached. </summary>
public class Voting : MonoBehaviour
{
    static CutsceneSkipText cs => CutsceneSkipText.Instance;

    /// <summary> Type of voting, on which the actions taken at the end of the voting depend. </summary>
    private VotingType type = VotingType.Choice;
    /// <summary> Action that will be launched after the end of the voting. </summary>
    private Action<int> onOver;

    /// <summary> Number of seconds from the beginning of the vote. </summary>
    private int lifetime;
    /// <summary> Text displaying the remaining time until the end of the voting. </summary>
    private Text display;

    /// <summary> Text displaying the amount of votes for each option. </summary>
    private Text[] options = new Text[3];
    /// <summary> Option with the largest number of votes. </summary>
    private int best
    {
        get
        {
            int[] v = { Votes.Count(0), Votes.Count(1), Votes.Count(2) };
            int max = Mathf.Max(v[0], v[1], v[2]);

            List<int> list = new();
            for (int i = 0; i < 3; i++) if (v[i] == max) list.Add(i);

            Random rand = new(Writer.Uint2int(LobbyController.Lobby.Value.Id.AccountId));
            return list[rand.Next(0, list.Count)];
        }
    }

    private void Start()
    {
        if (TryGetComponent(out CutsceneSkip cutscene))
            type = VotingType.CutsceneSkip;
        if (TryGetComponent(out IntermissionController dialog))
            type = VotingType.DialogSkip;

        cs.GetComponents<MonoBehaviour>()[1].enabled = false;
        cs.GetComponent<TextMeshProUGUI>().text = Bundle.Get("votes.cutscene-skip");

        switch (type)
        {
            case VotingType.CutsceneSkip:
                var originalCS = cutscene.onSkip;
                cutscene.onSkip = new() { onActivate = new() };
                cutscene.onSkip.onActivate.AddListener(() =>
                {
                    Votes.Vote();
                    Votes.UpdateVote(Tools.AccId, 0);
                });

                display = UIB.Text("", cs.transform, Msg(640f), size: 16);

                onOver = _ => originalCS.Invoke();
                break;

            case VotingType.DialogSkip:
                var originalDS = dialog.onComplete;
                dialog.onComplete = new();
                dialog.onComplete.AddListener(() =>
                {
                    Votes.Vote();
                    Votes.UpdateVote(Tools.AccId, 0);
                });

                display = UIB.Text("#votes.dialog-skip", dialog.transform, Msg(640f), size: 16);

                onOver = _ => originalDS.Invoke();
                break;

            case VotingType.Choice:
                Action Patch(char button, byte option)
                {
                    var btn = transform.Find($"Button ({button})").GetComponent<Button>();
                    var ori = btn.onClick;
                    btn.onClick = new();
                    btn.onClick.AddListener(() =>
                    {
                        Votes.Vote(option);
                        Votes.UpdateVote(Tools.AccId, option);
                    });
                    options[option] = UIB.Text("", btn.transform, new(-20f, 0f, 128f, 128f, new(0f, .5f)), size: 16);
                    options[option].transform.eulerAngles = new(0f, 0f, 90f);

                    return ori.Invoke;
                }
                Action[] originals = { Patch('A', 0), Patch('B', 1), Patch('C', 2) };

                display = UIB.Text("#votes.choice", transform, Msg(640f) with { y = 20f }, size: 16);

                onOver = _ => originals[_].Invoke();
                break;
        }
    }

    private void SlowUpdate()
    {
        // if no one has voted yet, then the voting does not begin
        if (Votes.Ids2Votes.Count == 0) return;

        if (++lifetime < 10)
            display.text = Bundle.Format("votes.remaining", (10 - lifetime).ToString(), Votes.Ids2Votes.Count.ToString());
        else if (lifetime == 10)
            display.text = Bundle.Get("votes.over");
        else
            enabled = false;

        // continue the update cycle if the voting is not over
        if (enabled) Invoke("SlowUpdate", 1f);
    }

    private void UpdateVotes()
    {
        if (lifetime == 0) Invoke("SlowUpdate", 0f);
        if (Votes.Ids2Votes.Count == LobbyController.Lobby?.MemberCount) lifetime = 9;

        if (type == VotingType.Choice)
        {
            for (byte i = 0; i < 3; i++)
            {
                options[i].text = Bundle.Format("votes.amount", Votes.Count(i).ToString());
                options[i].color = Pal.red;
            }
            options[best].color = Pal.green;
        }
    }

    private void OnEnable()
    {
        Votes.Ids2Votes.Clear();
        Votes.CurrentVoting = this;

        lifetime = 0;
    }

    private void OnDisable()
    {
        switch (type)
        {
            case VotingType.CutsceneSkip:
                Destroy(display);
                goto case VotingType.DialogSkip;
            case VotingType.DialogSkip:
                onOver(0);
                break;
            case VotingType.Choice:
                onOver(best);
                break;
        }
    }

    private enum VotingType
    {
        CutsceneSkip, DialogSkip, Choice
    }
}

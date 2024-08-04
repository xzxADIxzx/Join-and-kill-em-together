namespace Jaket.UI.Elements;

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Jaket.Assets;
using Jaket.World;
using Jaket.Net;

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

                display = UIB.Text("#votes.choice", transform, Msg(640f) with { y = 20f }, size: 16);

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
        if (Votes.Ids2Votes.Count == LobbyController.Lobby?.MemberCount) lifetime = 9;
        if (lifetime == 0) SlowUpdate();

        if (type == VotingType.Choice)
        {
            // TODO update labels
        }
    }

    private void OnEnabled()
    {
        Votes.Ids2Votes.Clear();
        Votes.CurrentVoting = this;

        lifetime = 0;
    }

    private void OnDisabled()
    {
        switch (type)
        {
            case VotingType.CutsceneSkip or VotingType.DialogSkip:
                onOver(0);
                break;
            case VotingType.Choice:
                // TODO choose the best option
                break;
        }
    }

    private enum VotingType
    {
        CutsceneSkip, DialogSkip, Choice
    }
}

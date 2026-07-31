namespace Jaket.UI.Fragments;

using UnityEngine;

using Jaket.Net;
using Jaket.UI.Lib;

using static Jaket.UI.Lib.Pal;

/// <summary> Fragment that provides access to lobbies and ranks. </summary>
public class MainMenuAccess : Fragment
{
    public MainMenuAccess(Transform root) : base(root, "MainMenuAccess", true, cond: () => Scene == "Main Menu", hide: () => UI.Access?.Toggle()) { Toggle(); }

    public override void Toggle()
    {
        #region difficulty

        var root = CanvasController.Instance.transform.Find("Difficulty Select (1)/Interactables");

        var sep1 = Builder.Image (Builder.Rect("Sep1", root, new(-210f, -170f, 380f, 02f, new(1f, .5f))), null, green       ).gameObject;
        var sep2 = Builder.Image (Builder.Rect("Sep2", root, new(-210f, -285f, 380f, 02f, new(1f, .5f))), null, green.Darker).gameObject;

        var btn1 = Builder.Button(Builder.Rect("Btn1", root, new(-210f, -200f, 380f, 50f, new(1f, .5f))), Tex.BrdL, green,        UI.LobbyList.Toggle,        "#lobby-tab.list", 24).gameObject;
        var btn2 = Builder.Button(Builder.Rect("Btn2", root, new(-210f, -255f, 380f, 50f, new(1f, .5f))), Tex.BrdL, green.Darker, LobbyController.JoinByCode, "#lobby-tab.join", 24).gameObject;

        var tips = Builder.Text  (Builder.Rect("Tips", root, new(-210f, +034f, 400f, 30f, new(1f, .0f))), "#menuaccess", 14, white).gameObject;

        if (root.TryGetComponent(out ObjectActivateInSequence seq)) Insert(ref seq.objectsToActivate, -1, [ sep1, btn1, btn2, sep2, tips ]);

        #endregion
    }
}

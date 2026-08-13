namespace Jaket.UI.Fragments;

using UnityEngine;
using UnityEngine.UI;

using Jaket.Assets;
using Jaket.IO;
using Jaket.Net;
using Jaket.UI.Lib;

using static Jaket.UI.Lib.Pal;

/// <summary> Fragment that provides access to lobbies and ranks. </summary>
public class MainMenuAccess : Fragment
{
    public MainMenuAccess(Transform root) : base(root, "MainMenuAccess", true, cond: () => Scene == "Main Menu", hide: () => UI.Access?.Toggle()) { Toggle(); }

    public override void Toggle()
    {
        #region lobbies

        var root = CanvasController.Instance.transform.Find("Difficulty Select (1)/Interactables");

        var sep1 = Builder.Image (Builder.Rect("Sep1", root, new(-210f, -170f, 380f, 02f, new(1f, .5f))), null, green       ).gameObject;
        var sep2 = Builder.Image (Builder.Rect("Sep2", root, new(-210f, -285f, 380f, 02f, new(1f, .5f))), null, green.Darker).gameObject;

        var btn1 = Builder.Button(Builder.Rect("Btn1", root, new(-210f, -200f, 380f, 50f, new(1f, .5f))), Tex.BrdL, green,        UI.LobbyList.Toggle,        "#lobby-tab.list", 24).gameObject;
        var btn2 = Builder.Button(Builder.Rect("Btn2", root, new(-210f, -255f, 380f, 50f, new(1f, .5f))), Tex.BrdL, green.Darker, LobbyController.JoinByCode, "#lobby-tab.join", 24).gameObject;

        var tips = Builder.Text  (Builder.Rect("Tips", root, new(-210f, +034f, 400f, 30f, new(1f, .0f))), "#menuaccess", 14, white).gameObject;

        if (root.TryGetComponent(out ObjectActivateInSequence seq)) Insert(ref seq.objectsToActivate, -1, [ sep1, btn1, btn2, sep2, tips ]);

        #endregion
        #region ranks

        CanvasController.Instance.GetComponentsInChildren<ChapterSelectButton>(true).Each(c => c.GetComponent<Button>().onClick.AddListener(() =>
        {
            CanvasController.Instance.GetComponentsInChildren<LevelSelectPanel>(true).Each(l =>
            {
                byte rank = Progress.Load
                (
                    l.levelNumber == 666 ? 44 + l.levelNumberInLayer : l.levelNumber == 100 ? l.levelNumberInLayer + 34 : l.levelNumber - 1,
                    PrefsManager.Instance.GetInt("difficulty")
                );

                var root = l.transform.Find("Stats"            ) as RectTransform;
                var prev = l.transform.Find("Stats/Rank"       ) as RectTransform;
                var next = l.transform.Find("Stats/Rank(Clone)") as RectTransform;

                if (next) Dest(next);
                next = Inst(prev.gameObject, root).transform as RectTransform;

                if (l.levelNumber == 666 || l.levelNumber == 100)
                {
                    prev.anchoredPosition = new(-31.5f, 10f);
                    next.anchoredPosition = new(+31.5f, 10f);

                    prev.sizeDelta = new(60f, 60f);
                    next.sizeDelta = new(60f, 60f);
                }
                else
                {
                    prev.anchoredPosition = new(-32f, 10f);
                    next.anchoredPosition = new(+10f, 10f);

                    prev.sizeDelta = new(40f, 60f);
                    next.sizeDelta = new(40f, 60f);

                    root.anchoredPosition = new(-11f, -260f);
                }

                next.Get<Image>(i =>
                {
                    i.sprite = rank == 6 ? l.filledPanel : l.unfilledPanel;
                    i.color = rank == 6 ? new(255, 175, 0, 255) : white;
                });
                next.Find("Text").Get<TMPro.TextMeshProUGUI>(t => t.text = Progress.Sign(rank));

                Builder.Image(Builder.Rect("Icon", next, new()), ModAssets.Mask, white with { a = invi.a }).PreserveAspect().transform.SetAsFirstSibling();
            });
        }));

        #endregion
    }
}

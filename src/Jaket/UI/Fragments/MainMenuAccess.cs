namespace Jaket.UI.Fragments;

using UnityEngine;
using UnityEngine.UI;

using ImageType = UnityEngine.UI.Image.Type;

using Jaket.Net;
using Jaket.UI.Lib;

using static Jaket.UI.Lib.Pal;

/// <summary> Fragment that provides access to lobbies through the main menu. </summary>
public class MainMenuAccess : Fragment
{
    /// <summary> Vanilla element containing difficulty selection. </summary>
    private GameObject original => CanvasController.Instance.transform.Find("Difficulty Select (1)/Interactables").gameObject;
    /// <summary> Additional elements to display in the difficulty selection element. </summary>
    private GameObject[] addition = new GameObject[5];

    public MainMenuAccess(Transform root) : base(root, "MainMenuAccess", true, hide: () => Events.Post(() => UI.Access.Toggle()))
    {
        Content.GetComponent<CanvasScaler>().screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        var col = Random.value < .1f ? pink : green;
        var drk = Darker(col);

        Component<Bar>(Rect("Content", new(-315f, -341.5f, 570f, 176f, new(1f, .5f))).gameObject, b =>
        {
            b.Setup(true, 0f, 6f);
            b.Update(() =>
            {
                if (!original?.activeInHierarchy ?? false) addition.Each(e => e.SetActive(false));
            });

            addition[0] = b.Image(null, 3f, col, ImageType.Simple).gameObject;
            b.Subbar(158f, s =>
            {
                s.Setup(true, 0f);

                addition[1] = s.MenuButton("#lobby-tab.list", col, UI.LobbyList.Toggle).gameObject;
                addition[2] = s.MenuButton("#lobby-tab.join", drk, LobbyController.JoinByCode).gameObject;
            });
            addition[3] = b.Image(null, 3f, drk, ImageType.Simple).gameObject;
        });
        addition[4] = Builder.Text(Rect("Tip", new(-315f, 50f, 620f, 40f, new(1f, 0f))), "#access", 21, white, TextAnchor.MiddleCenter).gameObject;

        Content.GetComponentsInChildren<Image>().Each(i => i.pixelsPerUnitMultiplier = 4f / 1.5f);
    }

    public override void Toggle()
    {
        if (Scene == "Main Menu" && original.TryGetComponent(out ObjectActivateInSequence seq)) Insert(ref seq.objectsToActivate, -1, addition);
    }
}

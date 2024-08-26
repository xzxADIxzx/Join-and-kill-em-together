namespace Jaket.UI.Elements;

using System;
using UnityEngine;
using UnityEngine.UI;

using Jaket.Assets;
using Jaket.IO;
using Jaket.Net;

using static Pal;
using static Rect;

/// <summary> Customization element located to the right of each terminal. </summary>
public class Customization : MonoBehaviour
{
    /// <summary> Whether the second page of the element is open. </summary>
    private bool second;
    /// <summary> Image showing the current page. </summary>
    private Transform selection;

    /// <summary> Transform containing preview of the chosen cosmetic trinkets. </summary>
    private Transform preview;
    /// <summary> Transform containing buttons for choosing cosmetic trinkets. </summary>
    private Transform buttons;
    /// <summary> Transform containing the purchase menu. </summary>
    private Transform buyMenu;

    private Image buyIcon;
    private Text buyText;
    private Button buyButton;
    private Action buyAction;

    private void Start()
    {
        UIB.Table("Customization", transform, Size(460f, 540f), canvas =>
        {
            canvas.localPosition = new(510f, 0f, -140f);
            canvas.localRotation = Quaternion.Euler(0f, 35f, 0f);

            UIB.Image("Border", canvas, Fill, shopc, fill: false); // no offset
            UIB.Image("Border", canvas, Fill, shopc, fill: false); // 15 units offset

            UIB.Text("#custom.support", canvas, new(0f, -32f, 4200f, 4200f, new(.5f, 1f)), size: 320).transform.localScale /= 10f;
            UIB.BMaCButton("Buy Me a Coffee", canvas).transform.localPosition = new(0f, 190f, 0f);

            UIB.Table("Button", canvas, new(-111f, 48f, 206f, 64f, new(.5f, 0f)), b => UIB.ShopButton("#custom.hats", b, Fill, () => Switch(false)));
            UIB.Table("Button", canvas, new(+111f, 48f, 206f, 64f, new(.5f, 0f)), b => UIB.ShopButton("#custom.jackets", b, Fill, () => Switch(true)));

            selection = UIB.Image("Selection", canvas, new(0f, 48f, 206f, 64f, new(.5f, 0f)), shopc, fill: false).transform;
            selection.localPosition += Vector3.back * 15f;

            preview = UIB.Image("Preview", canvas, new(96f, 216f, 160f, 240f, new(0f, 0f)), shopc, fill: false).transform;
            buttons = UIB.Rect("Buttons", canvas, new(-136f, 216f, 240f, 240f, new(1f, 0f)));

            UIB.Table("Buy Menu", canvas, Fill, table =>
            {
                buyMenu = table;
                buyMenu.localPosition = buyMenu.localPosition with { z = -30f };

                UIB.Image("Border", table, Fill, shopc, fill: false);

                buyIcon = UIB.Image("Icon", table, new(0f, 80f, 96f, 96f));
                buyText = UIB.Text("Message", table, Huge, size: 280);
                buyText.transform.localScale /= 10f;

                UIB.ShopButton("#custom.cancel", table, new(-111f, -80f, 206f, 64f), () => HideBuyMenu(false));
                buyButton = UIB.ShopButton("#custom.buy", table, new(+111f, -80f, 206f, 64f), () => HideBuyMenu(true));
            });

            for (int i = 1; i < canvas.childCount; i++) canvas.GetChild(i).transform.localPosition += Vector3.back * 15f;
            for (int i = 1; i < buyMenu.childCount; i++) buyMenu.GetChild(i).transform.localPosition += Vector3.back * 15f;

            foreach (var button in canvas.GetComponentsInChildren<Button>())
                Tools.Destroy(button.gameObject.AddComponent<ShopButton>()); // hacky
        });

        preview = Instantiate(ModAssets.Preview, preview).transform;
        preview.localPosition = new(0f, -80f, -40f);
        preview.localRotation = Quaternion.Euler(0f, 180f, 0f);
        preview.localScale = Vector3.one * 80f;
        preview = preview.Find("V3/Suits");

        HideBuyMenu(false);
        Rebuild();
    }

    private void Switch(bool target)
    {
        if (buyMenu.gameObject.activeSelf) return;
        second = target;
        Rebuild();
    }

    private void HideBuyMenu(bool bought)
    {
        preview.parent.gameObject.SetActive(true);
        buyMenu.gameObject.SetActive(false);
        if (bought) buyAction();
    }

    private void OnClick(int id)
    {
        var unlocked = Shop.IsUnlocked(id);
        if (unlocked)
        {
            if (second)
                Shop.SelectedJacket = id;
            else
                Shop.SelectedHat = id;

            Shop.SavePurchases();
            Rebuild();

            if (LobbyController.Online) Networking.LocalPlayer.SyncSuit();
        }
        else
        {
            preview.parent.gameObject.SetActive(false);
            buyMenu.gameObject.SetActive(true);
            buyIcon.sprite = Shop.Icon(id);

            int cost = Shop.Entries[id].cost;
            if (buyButton.interactable = GameProgressSaver.GetMoney() >= cost)
            {
                buyAction = () =>
                {
                    GameProgressSaver.AddMoney(-cost);
                    Shop.Unlock(id);
                    OnClick(id);
                };
                buyText.text = Bundle.Format("custom.message", MoneyText.DivideMoney(cost), $"#custom.{id}");
            }
            else
            {
                buyAction = null;
                buyText.text = Bundle.Get("custom.not-enough-message");
            }
        }
    }

    /// <summary> Rebuilds the element to update the page. </summary>
    public void Rebuild()
    {
        selection.localPosition = selection.localPosition with { x = second ? 111f : -111f };

        #region preview

        foreach (Transform suit in preview) suit.gameObject.SetActive(false);

        int hat = Shop.Entries[Shop.SelectedHat].hierarchyId;
        if (hat != -1) preview.GetChild(hat).gameObject.SetActive(true);

        int jacket = Shop.Entries[Shop.SelectedJacket].hierarchyId;
        if (jacket != -1) preview.GetChild(jacket).gameObject.SetActive(true);

        #endregion
        #region buttons

        int offset = second ? Shop.FirstJacket : 0;

        for (int i = 0; i < buttons.childCount; i++) Destroy(buttons.GetChild(i).gameObject);
        for (int i = 0; i < Shop.Entries.Length / 2 + (second ? -1 : 1); i++)
        {
            var rect = Shp(20f + i % 6 * 40f, -20f - i / 6 * 40f);
            var icon = UIB.Image("Button", buttons, rect, Shop.IsUnlocked(offset + i) ? white : black, Shop.Icon(offset + i));

            int j = i;
            UIB.Component<Button>(icon.gameObject, button =>
            {
                button.targetGraphic = icon;
                button.onClick.AddListener(() => OnClick(offset + j));
            });
            Tools.Destroy(icon.gameObject.AddComponent<ShopButton>()); // hacky
        }

        int l = second ? Shop.SelectedJacket - offset : Shop.SelectedHat;
        UIB.Image("Selection", buttons, Shp(20f + l % 6 * 40f, -20f - l / 6 * 40f), shopc, fill: false).transform.localPosition += Vector3.back * 10f;

        #endregion
    }
}

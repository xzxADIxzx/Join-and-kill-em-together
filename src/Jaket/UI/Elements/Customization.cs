namespace Jaket.UI.Elements;

using UnityEngine;
using UnityEngine.UI;

using static Pal;
using static Rect;

/// <summary> Customization element located to the right of each terminal. </summary>
public class Customization : MonoBehaviour
{
    /// <summary> Whether the second page of the element is open. </summary>
    private bool second;
    /// <summary> Image showing the current page. </summary>
    private Transform selection;

    private void Start()
    {
        UIB.Table("Customization", transform, Size(460f, 540f), canvas =>
        {
            canvas.localPosition = new(510f, 0f, -140f);
            canvas.localRotation = Quaternion.Euler(0f, 35f, 0f);

            UIB.Image("Border", canvas, Fill, shopc, fill: false); // no offset
            UIB.Image("Border", canvas, Fill, shopc, fill: false); // 15 units offset

            UIB.Text("#custom.support", canvas, new(0f, -32f, 4200f, 4200f, new(.5f, 1f)), size: 320).transform.localScale /= 10f;
            UIB.BMaCButton("Buy Me a Coffee", canvas).transform.localPosition = new(0f, 190f, -15f);

            UIB.Table("Button", canvas, new(-111f, 48f, 206f, 64f, new(.5f, 0f)), button => UIB.ShopButton("#custom.hats", button, Fill, () =>
            {
                second = false;
                Rebuild();
            }));
            UIB.Table("Button", canvas, new(+111f, 48f, 206f, 64f, new(.5f, 0f)), button => UIB.ShopButton("#custom.jackets", button, Fill, () =>
            {
                second = true;
                Rebuild();
            }));

            selection = UIB.Image("Selection", canvas, new(0f, 48f, 206f, 64f, new(.5f, 0f)), shopc, fill: false).transform;
            selection.localPosition += Vector3.back * 15f;

            for (int i = 1; i < canvas.childCount; i++) canvas.GetChild(i).transform.localPosition += Vector3.back * 15f;

            foreach (var button in canvas.GetComponentsInChildren<Button>())
                Tools.Destroy(button.gameObject.AddComponent<ShopButton>()); // hacky
        });
        Rebuild();
    }

    /// <summary> Rebuilds the element to update the page. </summary>
    public void Rebuild()
    {
        selection.localPosition = selection.localPosition with { x = second ? 111f : -111f };
    }
}

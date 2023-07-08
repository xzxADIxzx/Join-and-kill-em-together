namespace Jaket.UI;

using UnityEngine;

public class PlayerList
{
    public static bool Shown;
    private static GameObject canvas, create, invite;

    public static void Build()
    {
        canvas = Utils.Canvas("Player List", Plugin.Instance.transform);
        canvas.SetActive(false);

        Utils.Text("--LOBBY--", canvas.transform, -784f, 492f);

        create = Utils.Button("CREATE LOBBY", canvas.transform, -784f, 412f, () =>
        {
        });

        invite = Utils.Button("INVITE FRIEND", canvas.transform, -784f, 332f, () =>
        {
        });
    }

    public static void Toggle()
    {
        canvas.SetActive(Shown = !Shown);
        Utils.ToggleCursor(Shown);
    }
}

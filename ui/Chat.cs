namespace Jaket.UI;

using UnityEngine;
using UnityEngine.UI;

using Jaket.Net;

public class Chat
{
    public static bool Shown;
    private static GameObject canvas;
    private static InputField field;

    public static void Build()
    {
        canvas = Utils.Canvas("Chat", Plugin.Instance.transform);
        canvas.SetActive(false);

        field = Utils.Field("Type a chat message and send it by pressing enter", canvas.transform, 0f, -516f, 1888f, 32f, 24, message =>
        {
            LobbyController.Lobby?.SendChatString(message);

            field.text = "";
            canvas.SetActive(false);
        });
    }

    public static void Toggle()
    {
        if (field.text != "") return;

        canvas.SetActive(Shown = !Shown);
        field.GetComponent<UnityEngine.UI.InputField>().ActivateInputField();
    }

    public static void Received(string author, string message)
    {
        Debug.LogWarning(author + ": " + message);
    }
}
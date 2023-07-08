namespace Jaket.UI;

using UnityEngine;
using UnityEngine.UI;

using Jaket.Net;

public class Chat
{
    const int messagesShown = 12;
    const int symbolsPerRow = 63;
    const float width = 600f;
    const float textWidth = width - 32f;
    const float textHeight = 18f;

    public static bool Shown;
    private static GameObject canvas;
    private static RectTransform panel;
    private static InputField field;

    public static void Build()
    {
        canvas = Utils.Canvas("Chat", Plugin.Instance.transform);
        canvas.SetActive(false);

        panel = Utils.Image("Chat Panel", canvas.transform, -644f, -276f, width, 400f, new Color(0f, 0f, 0f, .5f)).GetComponent<RectTransform>();

        field = Utils.Field("Type a chat message and send it by pressing enter", canvas.transform, 0f, -508f, 1888f, 32f, 24, message =>
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

        // focus on input field
        field.GetComponent<UnityEngine.UI.InputField>().ActivateInputField();
    }

    public static void Received(string author, string message)
    {
        message = author + ": " + message;
        float height = textHeight * Mathf.Ceil((float)message.Length / symbolsPerRow);

        // move old messages down
        for (int i = 0; i < panel.childCount; i++)
            panel.GetChild(i).GetComponent<RectTransform>().anchoredPosition += new Vector2(0f, height);

        // add new message
        Utils.Text(message, panel, 0f, -184f + height / 2f, textWidth, height, 16, Color.white, TextAnchor.LowerLeft);

        // delete very old messages
        if (panel.childCount > messagesShown) GameObject.Destroy(panel.GetChild(0).gameObject);
    }
}
namespace Jaket.UI;

using UnityEngine;
using UnityEngine.UI;

using Jaket.Net;

public class Chat
{
    const int maxMessageLength = 128;
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

        panel = Utils.Image("Chat Panel", canvas.transform, 0f, 0f, width, 0f, new Color(0f, 0f, 0f, .5f)).GetComponent<RectTransform>();

        field = Utils.Field("Type a chat message and send it by pressing enter", canvas.transform, 0f, -508f, 1888f, 32f, 24, message =>
        {
            LobbyController.Lobby?.SendChatString(message);

            field.text = "";
            canvas.SetActive(false);
        });
        field.characterLimit = maxMessageLength;
    }

    public static void Toggle()
    {
        if (field.text != "") return;

        canvas.SetActive(Shown = !Shown);
        Utils.ToggleMovement(!Shown);

        // focus on input field
        field.GetComponent<UnityEngine.UI.InputField>().ActivateInputField();
    }

    public static string FormatMessage(string author, string message) => "<b>" + author + "<color=#ff7f50>:</color></b> " + message;

    public static int RawMessageLenght(string author, string message) => author.Length + ": ".Length + message.Length;  

    public static void Received(string author, string message)
    {
        float height = textHeight * Mathf.Ceil((float)RawMessageLenght(author, message) / symbolsPerRow);
        message = FormatMessage(author, message);

        // move old messages down
        for (int i = 0; i < panel.childCount; i++)
            panel.GetChild(i).GetComponent<RectTransform>().anchoredPosition += new Vector2(0f, height);

        // add new message
        var text = Utils.Text(message, panel, 0f, 25f + height / 2f, textWidth, height, 16, Color.white, TextAnchor.LowerLeft).GetComponent<RectTransform>();
        text.anchorMin = new Vector2(.5f, 0f);
        text.anchorMax = new Vector2(.5f, 0f);

        // delete very old messages
        if (panel.childCount > messagesShown) GameObject.Destroy(panel.GetChild(0).gameObject);

        // scale chat panel
        var firstChild = panel.GetChild(0).gameObject.GetComponent<RectTransform>();
        panel.sizeDelta = new Vector2(width, firstChild.anchoredPosition.y + firstChild.sizeDelta.y / 2f + 16f);
        panel.anchoredPosition = new Vector2(-644f, -476f + panel.sizeDelta.y / 2f);
    }
}
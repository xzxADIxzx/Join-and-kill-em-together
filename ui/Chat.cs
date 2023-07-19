namespace Jaket.UI;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using Jaket.Net;

public class Chat : MonoBehaviour
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
    private static CanvasGroup group;
    private static RectTransform typingBackground;
    private static Text typing;
    private static InputField field;
    private static float lastMessageTime;

    public void Update()
    {
        group.alpha = Mathf.Lerp(group.alpha, Shown || Time.time - lastMessageTime < 5f ? 1f : 0f, Time.deltaTime * 5f);
    }

    public void Awake() => InvokeRepeating("UpdateTyping", 0f, .5f);

    public void UpdateTyping()
    {
        var players = LobbyController.TypingPlayers();

        typingBackground.gameObject.SetActive(players.Count > 0 || Shown);
        string text = "";

        for (int i = 0; i < Mathf.Min(players.Count, 3); i++) text += (text == "" ? "" : ", ") + players[i];
        if (players.Count > 3) text += " and others";
        if (players.Count > 0) text += players[0] != "You" && players.Count == 1 ? " is typing..." : " are typing...";

        typing.text = text;
        float width = text.Length * 14f + 16f;

        typingBackground.sizeDelta = new Vector2(width, 32f);
        typingBackground.anchoredPosition = new Vector2(-944f + width / 2f, -460f);
    }

    public static void Build()
    {
        // hide chat once loading a scene
        SceneManager.sceneLoaded += (scene, mode) => field.gameObject.SetActive(Shown = false);

        canvas = Utils.Canvas("Chat", Plugin.Instance.transform);
        canvas.AddComponent<Chat>();

        panel = Utils.Image("Chat Panel", canvas.transform, 0f, 0f, width, 0f, new Color(0f, 0f, 0f, .5f)).GetComponent<RectTransform>();
        group = panel.gameObject.AddComponent<CanvasGroup>();

        typingBackground = Utils.Image("Typing Background", canvas.transform, 0f, 0f, 0f, 0f, new Color(0f, 0f, 0f, .5f)).GetComponent<RectTransform>();
        typing = Utils.Text("", typingBackground, 0f, 0f, 1000f, 32f, 24, Color.white, TextAnchor.MiddleCenter).GetComponent<Text>();

        field = Utils.Field("Type a chat message and send it by pressing enter", canvas.transform, 0f, -508f, 1888f, 32f, 24, message =>
        {
            LobbyController.Lobby?.SendChatString(message);

            field.text = "";
            field.gameObject.SetActive(false);
        });
        field.characterLimit = maxMessageLength;
        field.gameObject.SetActive(false);
    }

    public static void Toggle()
    {
        if (field.text != "" && field.isFocused) return;

        field.gameObject.SetActive(Shown = !Shown);
        Utils.ToggleMovement(!Shown);

        // focus on input field
        field.GetComponent<InputField>().ActivateInputField();
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
        var text = Utils.Text(message, panel, 0f, 16f + height / 2f, textWidth, height, 16, Color.white, TextAnchor.LowerLeft).GetComponent<RectTransform>();
        text.anchorMin = new Vector2(.5f, 0f);
        text.anchorMax = new Vector2(.5f, 0f);

        // delete very old messages
        if (panel.childCount > messagesShown) GameObject.DestroyImmediate(panel.GetChild(0).gameObject);

        // scale chat panel
        var firstChild = panel.GetChild(0).gameObject.GetComponent<RectTransform>();
        panel.sizeDelta = new Vector2(width, firstChild.anchoredPosition.y + firstChild.sizeDelta.y / 2f + 16f);
        panel.anchoredPosition = new Vector2(-644f, -428f + panel.sizeDelta.y / 2f);

        // save the time the message was received to give the player time to read it
        lastMessageTime = Time.time;
    }
}
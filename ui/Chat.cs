namespace Jaket.UI;

using UMM;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using Jaket.Net;
using Jaket.World;
using System.Collections.Generic;
using System.Collections;

/// <summary> Front end of the chat, back end implemented via Steamworks. </summary>
public class Chat : MonoSingleton<Chat>
{
    /// <summary> Maximum length of chat messages. </summary>
    const int MAX_MESSAGE_LENGTH = 128;
    /// <summary> How many messages at a time will be shown. </summary>
    const int MESSAGES_SHOWN = 12;
    /// <summary> How many characters fit in one line of chat. </summary>
    const int SYMBOLS_PER_ROW = 63;
    /// <summary> Chat width in pixels. </summary>
    const float WIDTH = 600f;

    /// <summary> Whether chat is visible or hidden. </summary>
    public bool Shown;

    /// <summary> List of chat messages. </summary>
    private RectTransform list;
    /// <summary> Canvas group used to change the chat transparency. </summary>
    private CanvasGroup listBg;

    /// <summary> List of players currently typing. </summary>
    private Text typing;
    /// <summary> Background of the typing players list. </summary>
    private RectTransform typingBg;

    /// <summary> Input field in which the message will be entered directly. </summary>
    public InputField field;
    /// <summary> Arrival time of the last message, used to change the chat transparency. </summary>
    private float lastMessageTime;
    /// <summary> Last messages to cycle with. </summary>
    private List<string> lastMessages = new();
    /// <summary> Coroutine used to scroll the messages. </summary>
    Coroutine messageScrolled;
    private int _lastMessageIndex;
    /// <summary Current last message index. </summary>
    private int lastMessageIndex
    {
        get => _lastMessageIndex;
        set
        {
            // Warp around the list
            if (value >= lastMessages.Count) _lastMessageIndex = 0;
            else if (value < 0) _lastMessageIndex = lastMessages.Count - 1;
            else _lastMessageIndex = value;
        }
    }

    // <summary> Formats the message for a more presentable look. </summary>
    public static string FormatMessage(string author, string message) => $"<b>{author}<color=#ff7f50>:</color></b> {message}";

    // <summary> Returns the length of the message without formatting. </summary>
    public static float RawMessageLength(string author, string message) => author.Length + ": ".Length + message.Length;

    /// <summary> Creates a singleton of chat. </summary>
    public static void Build()
    {
        // initialize the singleton and create a canvas
        Utils.Canvas("Chat", Plugin.Instance.transform).AddComponent<Chat>();

        // hide chat once loading a scene
        SceneManager.sceneLoaded += (scene, mode) => Instance.field.gameObject.SetActive(Instance.Shown = false);

        // add a list of messages
        Instance.list = Utils.Image("List", Instance.transform, 0f, 0f, 0f, 0f).transform as RectTransform;
        Instance.listBg = Instance.list.gameObject.AddComponent<CanvasGroup>();
        Instance.listBg.blocksRaycasts = false; // necessary so that the chat does not interfere with pressing the buttons

        // add a list of typing players
        Instance.typingBg = Utils.Image("", Instance.transform, 0f, 0f, 0f, 0f).transform as RectTransform;
        Instance.typing = Utils.Text("", Instance.typingBg, 0f, 0f, 1000f, 32f, 24).GetComponent<Text>();

        // add input field
        Instance.field = Utils.Field("Type a chat message and send it by pressing enter", Instance.transform, 0f, -508f, 1888f, 32f, 24, Instance.OnFocusLost);
        Instance.field.characterLimit = MAX_MESSAGE_LENGTH;
        Instance.field.gameObject.SetActive(false);

        // moving elements to display correctly on wide screens
        WidescreenFix.MoveUp(Instance.transform);
    }

    public void Start() => InvokeRepeating("UpdateTyping", 0f, .25f);

    public void Update() => listBg.alpha = Mathf.Lerp(listBg.alpha, Shown || Time.time - lastMessageTime < 5f ? 1f : 0f, Time.deltaTime * 5f);

    /// <summary> Updates the list of players currently typing. </summary>
    public void UpdateTyping()
    {
        // get a list of players
        var players = LobbyController.TypingPlayers();

        // hide the background if no one is typing
        typingBg.gameObject.SetActive(players.Count > 0);

        // there is no point in doing anything, because no one is typing
        if (players.Count == 0) return;

        // put first three players to the list
        typing.text = string.Join(", ", players.ToArray(), 0, Mathf.Min(players.Count, 3));

        if (players.Count > 3) typing.text += " and others"; // grammar time
        if (players.Count > 0) typing.text += players[0] != "You" && players.Count == 1 ? " is typing..." : " are typing...";

        // update background width and position
        float width = typing.text.Length * 14f + 16f;

        typingBg.sizeDelta = new Vector2(width, 32f);
        typingBg.anchoredPosition = new Vector2(-944f + width / 2f, -460f + WidescreenFix.Offset);
    }

    /// <summary> Toggles visibility of chat. </summary>
    public void Toggle()
    {
        // if the player is typing, then nothing needs to be done
        if (field.text != "" && field.isFocused) return;

        // no comments
        field.gameObject.SetActive(Shown = !Shown && LobbyController.Lobby != null);
        if (Movement.Instance.Emoji == 0xFF) Movement.ToggleMovement(!Shown);

        // focus on input field
        if (Shown) field.ActivateInputField();
    }

    public void ScrollMessages(bool up)
    {
        // checking if the list is not empty and chat is not hidden
        if (lastMessages.Count == 0 && !Shown) return;

        // updating the last message index
        lastMessageIndex += up ? -1 : 1;

        // updating the text field
        field.text = lastMessages[lastMessageIndex];

        if (messageScrolled != null) // Check if the Coroutine variable is not null 
        { 
            StopCoroutine(messageScrolled); // Stop the current coroutine 
        } 
        messageScrolled = StartCoroutine(MessageScrolled());

        // fixing the caret position
        field.caretPosition = field.text.Length;
    }

    private IEnumerator MessageScrolled()
    {
        Text textComponent = field.textComponent;
        Color startColor = Color.green;
        Color endColor = Color.white;

        const float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float normalizedTime = Mathf.InverseLerp(0f, duration, elapsed);
            // normalizedTime is now in range [0, 1]

            textComponent.color = Color.Lerp(startColor, endColor, normalizedTime);
            yield return null;

            // increment the elapsed time by Time.unscaledDeltaTime
            elapsed += Time.unscaledDeltaTime;
        }

        textComponent.color = endColor; // ensure the color is set to the end color
    }

    /// <summary> Fires when the input field loses its focus. </summary>
    public void OnFocusLost(string message)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            // focus lost because the player entered a message
            SendChatMessage(message);
        else
        {
            // focus lost for some other reason
            field.gameObject.SetActive(Shown = false);
            if (Movement.Instance.Emoji == 0xFF) Movement.ToggleMovement(true);
        }
    }

    /// <summary> Sends a message to all other players. </summary>
    public void SendChatMessage(string message)
    {
        // remove extra spaces from message
        message = message.Trim();

        // if the message is not empty, then send it to other players and remember it
        if (message != "")
        {
            LobbyController.Lobby?.SendChatString(message);
            lastMessages.Insert(0, message);
        }

        // clear the input field
        field.text = "";

        // if the message was sent not by the button that toggles the chat, then need to do it yourself
        if (!Input.GetKeyDown(UKAPI.GetKeyBind("CHAT").keyBind)) Toggle();
    }

    /// <summary> Writes a message directly to the chat. </summary>
    public void ReceiveChatMessage(string author, string message)
    {
        // find message height by the number of characters
        float height = 18f * Mathf.Ceil(RawMessageLength(author, message) / SYMBOLS_PER_ROW);

        // move old messages up
        foreach (RectTransform child in list) child.anchoredPosition += new Vector2(0f, height);

        // add new message
        var text = Utils.Text(FormatMessage(author, message), list, 0f, 16f + height / 2f, WIDTH - 32f, height, 16, align: TextAnchor.MiddleLeft).transform as RectTransform;
        text.anchorMin = text.anchorMax = new(.5f, 0f);
        text.localScale = new(1f, 1f, 1f); // unity scales text crookedly for small resolutions, which is why it is incorrectly located

        // delete very old messages
        if (list.childCount > MESSAGES_SHOWN) DestroyImmediate(list.GetChild(0).gameObject);

        // scale chat panel
        var firstChild = list.GetChild(0) as RectTransform;
        list.sizeDelta = new(WIDTH, firstChild.anchoredPosition.y + firstChild.sizeDelta.y / 2f + 16f);
        list.anchoredPosition = new(-644f, -428f + list.sizeDelta.y / 2f + WidescreenFix.Offset);

        // save the time the message was received to give the player time to read it
        lastMessageTime = Time.time;
    }
}
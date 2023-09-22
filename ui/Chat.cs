namespace Jaket.UI;

using Steamworks;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UMM;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using Jaket.Net;
using Jaket.Sam;
using Jaket.World;

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
    /// <summary> Prefix that will be added to the TTS message. </summary>
    const string TTS_PREFIX = "<color=#ff7f50><size=14>[TTS]</size></color>";

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

    /// <summary> Whether auto TTS is enabled. </summary>
    private bool autoTTS;
    /// <summary> Background of the auto TTS sign. </summary>
    private RectTransform ttsBg;

    /// <summary> Input field in which the message will be entered directly. </summary>
    public InputField field;
    /// <summary> Arrival time of the last message, used to change the chat transparency. </summary>
    private float lastMessageTime;

    /// <summary> Messages sent by the player. </summary>
    private List<string> messages = new();
    /// <summary> Index of the current message in the list. </summary>
    private int messageIndex;

    // <summary> Formats the message for a more presentable look. </summary>
    public static string FormatMessage(string color, string author, string message) => $"<b><color=#{color}>{author}</color><color=#ff7f50>:</color></b> {message}";

    // <summary> Returns the length of the message without formatting. </summary>
    public static float RawMessageLength(string message) => Regex.Replace(message, "<.*?>", string.Empty).Length;

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

        // add a sign about the auto TTS being turned on
        Instance.ttsBg = Utils.Image("", Instance.transform, 0f, 0f, 128f, 32f).transform as RectTransform;
        Utils.Text("<color=#cccccccc>Auto TTS</color>", Instance.ttsBg, 0f, 0f, 128f, 32f, 24);

        // add input field
        Instance.field = Utils.Field("Type a chat message and send it by pressing enter", Instance.transform, 0f, -508f, 1888f, 32f, 24, Instance.OnFocusLost);
        Instance.field.characterLimit = MAX_MESSAGE_LENGTH;
        Instance.field.gameObject.SetActive(false);

        // moving elements to display correctly on wide screens
        WidescreenFix.MoveUp(Instance.transform);
    }

    public void Start() => InvokeRepeating("UpdateTyping", 0f, .25f);

    public void Update()
    {
        // interpolate the transparency of the message list
        listBg.alpha = Mathf.Lerp(listBg.alpha, Shown || Time.time - lastMessageTime < 5f ? 1f : 0f, Time.deltaTime * 5f);

        // update auto TTS sign width and position
        float width = typingBg.gameObject.activeSelf ? typingBg.anchoredPosition.x + typingBg.sizeDelta.x / 2f + 80f : -880f;

        ttsBg.gameObject.SetActive(autoTTS && Shown);
        ttsBg.anchoredPosition = new Vector2(width, typingBg.anchoredPosition.y);
    }

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

    /// <summary> Scrolls messages through the list of messages sent by the player. </summary>
    public void ScrollMessages(bool up)
    {
        // to scroll through messages, the chat must be open and the list must have at least one element
        if (messages.Count == 0 || !Shown) return;

        // limiting the message index
        if (up ? messageIndex == messages.Count - 1 : messageIndex == -1) return;

        // update message id and text in the input field
        messageIndex += up ? 1 : -1;
        field.text = messageIndex == -1 ? "" : messages[messageIndex];
        field.caretPosition = field.text.Length;

        // run message highlight
        StopCoroutine("MessageScrolled");
        StartCoroutine("MessageScrolled");
    }

    /// <summary> Interpolates the color of the input field from green to white. </summary>
    private IEnumerator MessageScrolled()
    {
        float start = Time.time;
        while (Time.time - start < .4f)
        {
            field.textComponent.color = Color.Lerp(Color.green, Color.white, (Time.time - start) * 2.5f);
            yield return null;
        }
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
            // handle TTS command
            if (message == "/tts on") autoTTS = true;
            else if (message == "/tts off") autoTTS = false;
            else LobbyController.Lobby?.SendChatString(autoTTS ? "/tts " + message : message);

            messages.Insert(0, message);
        }

        // clear the input field & reset message index
        field.text = "";
        messageIndex = -1;

        // if the message was sent not by the button that toggles the chat, then need to do it yourself
        if (!Input.GetKeyDown(UKAPI.GetKeyBind("CHAT").keyBind)) Toggle();
    }

    #region receive

    /// <summary> Writes a message directly to the chat. </summary>
    public void ReceiveChatMessage(string message, bool oneline = false)
    {
        // find message height by the number of characters
        float height = oneline ? 18f : 18f * Mathf.Ceil(RawMessageLength(message) / SYMBOLS_PER_ROW);

        // move old messages up
        foreach (RectTransform child in list) child.anchoredPosition += new Vector2(0f, height);

        // add new message
        var text = Utils.Text(message, list, 0f, 16f + height / 2f, WIDTH - 32f, height, 16, align: TextAnchor.MiddleLeft).transform as RectTransform;
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

    /// <summary> Writes a message to the chat, formatting it beforehand. </summary>
    public void ReceiveChatMessage(string color, string author, string message, bool tts = false, bool oneline = false)
        => ReceiveChatMessage(FormatMessage(color, tts ? TTS_PREFIX + author : author, message), oneline);

    /// <summary> Speaks the message before writing it. </summary>
    public void ReceiveTTSMessage(Friend author, string message)
    {
        if (author.IsMe)
            // play the message in the player's position if he is its author
            SamAPI.TryPlay(message, Networking.LocalPlayer.voice);
        else
            // or find the author among other players and play the sound from them
            Networking.EachPlayer(player =>
            {
                if (player.Id == author.Id) SamAPI.TryPlay(message, player.Voice);
            });

        // write a message to chat
        ReceiveChatMessage(Networking.GetTeamColor(author), author.Name, message, true);
    }

    /// <summary> Sends some useful information to the chat. </summary>
    public void Hello()
    {
        // if the last owner of the lobby is not equal to 0, then the lobby is not created for the first time and there is no need to print info
        if (LobbyController.LastOwner != 0L) return;

        void SendMsg(string msg) => ReceiveChatMessage("0096FF", "xzxADIxzx", msg, oneline: true);
        void SendTip(string tip) => SendMsg($"<size=14>* {tip}</size>");

        SendMsg("Hello, it's me, the main developer of this mod.");
        SendMsg("I just wanted to give some tips about Jaket:");

        SendTip("Go to the control settings, there are a few new elements");
        SendTip($"Hold {Movement.Instance.EmojiBind.keyBind} to open the Emotion Wheel");
        SendTip("Try typing to chat /tts <color=#cccccccc>[message]</color> or /tts <color=#cccccccc>[on/off]</color>");
        SendTip("Take a look at the bestiary, there's a little surprise :3");

        SendMsg("Cheers~ ♡");
    }

    #endregion
}
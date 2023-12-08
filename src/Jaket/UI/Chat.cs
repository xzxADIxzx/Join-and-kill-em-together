namespace Jaket.UI;

using Steamworks;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

using Jaket.Commands;
using Jaket.Net;
using Jaket.Sam;
using Jaket.World;

/// <summary> Front end of the chat, back end implemented via Steamworks. </summary>
public class Chat : CanvasSingleton<Chat>
{
    /// <summary> Maximum length of chat message. </summary>
    const int MAX_MESSAGE_LENGTH = 128;
    /// <summary> How many messages at a time will be shown. </summary>
    const int MESSAGES_SHOWN = 12;
    /// <summary> How many characters fit in one line of chat. </summary>
    const int SYMBOLS_PER_ROW = 63;
    /// <summary> Chat width in pixels. </summary>
    const float WIDTH = 600f;

    /// <summary> Prefix that will be added to bot messages. </summary>
    public const string BOT_PREFIX = "<color=#ff7f50><size=14>[BOT]</size></color>";
    /// <summary> Prefix that will be added to the TTS message. </summary>
    public const string TTS_PREFIX = "<color=#ff7f50><size=14>[TTS]</size></color>";

    /// <summary> List of chat messages. </summary>
    private RectTransform list;
    /// <summary> Canvas group used to change the chat transparency. </summary>
    private CanvasGroup listBg;

    /// <summary> List of players currently typing. </summary>
    private Text typing;
    /// <summary> Background of the typing players list. </summary>
    private RectTransform typingBg;

    /// <summary> Whether auto TTS is enabled. </summary>
    public bool AutoTTS;
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

    private void Start()
    {
        list = UI.Table("List", transform, 0f, 0f, 0f, 0f).rectTransform;
        listBg = UI.Component<CanvasGroup>(list.gameObject, group => group.blocksRaycasts = false); // disable chat collision so it doesn't interfere with buttons

        typingBg = UI.Table("Typing", transform, 0f, 0f, 0f, 0f).rectTransform;
        typing = UI.Text("", typingBg, 0f, 0f, 1000f, 32f, size: 24);

        ttsBg = UI.Table("TTS", transform, 0f, 0f, 128f, 32f).rectTransform;
        UI.Text("<color=#cccccccc>Auto TTS</color>", ttsBg, 0f, 0f, size: 24);

        field = UI.Field("Type a chat message and send it by pressing enter", transform, 0f, -508f, 1888f, 32f, enter: OnFocusLost);
        field.characterLimit = MAX_MESSAGE_LENGTH;
        field.gameObject.SetActive(false);

        // load settings
        AutoTTS = Settings.GetAutoTTS();

        // start the update cycle of typing players
        InvokeRepeating("UpdateTyping", 0f, .25f);

        // moving elements to display correctly on wide screens
        WidescreenFix.MoveUp(Instance.transform);
    }

    private void Update()
    {
        // interpolate the transparency of the message list
        listBg.alpha = Mathf.Lerp(listBg.alpha, Shown || Time.time - lastMessageTime < 5f ? 1f : 0f, Time.deltaTime * 5f);

        // update auto TTS sign width and position
        float width = typingBg.gameObject.activeSelf ? typingBg.anchoredPosition.x + typingBg.sizeDelta.x / 2f + 80f : -880f;

        ttsBg.gameObject.SetActive(AutoTTS && Shown);
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
        // if another menu is open, then nothing needs to be done
        if (UI.AnyJaket() && !Shown) return;

        // if the player is typing, then nothing needs to be done
        if (field.text != "" && field.isFocused) return;

        field.gameObject.SetActive(Shown = !Shown && LobbyController.Lobby != null);
        Movement.UpdateState();

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
            Movement.UpdateState();
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
            if (!Commands.Handler.Handle(message)) LobbyController.Lobby?.SendChatString(AutoTTS ? "/tts " + message : message);
            messages.Insert(0, message);
        }

        // clear the input field & reset message index
        field.text = "";
        messageIndex = -1;

        // if the message was sent not by the button that toggles the chat, then need to do it yourself
        if (!Input.GetKeyDown(Settings.Chat)) Toggle();
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
        var text = UI.Text(message, list, 0f, 16f + height / 2f, WIDTH - 32f, height, size: 16, align: TextAnchor.MiddleLeft).rectTransform;
        text.anchorMin = text.anchorMax = new(.5f, 0f);
        text.localScale = new(1f, 1f, 1f); // unity scales text crookedly for small resolutions, which is why it is incorrectly located

        // delete very old messages
        if (list.childCount > MESSAGES_SHOWN) DestroyImmediate(list.GetChild(0).gameObject);

        // scale chat panel
        var first = list.GetChild(0) as RectTransform;
        list.sizeDelta = new(WIDTH, first.anchoredPosition.y + first.sizeDelta.y / 2f + 16f);
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
            SamAPI.TryPlay(message, Networking.LocalPlayer.Voice);
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
    public void Hello(bool force = false)
    {
        // if the last owner of the lobby is not equal to 0, then the lobby is not created for the first time and there is no need to print info
        if (LobbyController.LastOwner != 0L && !force) return;

        void SendMsg(string msg) => ReceiveChatMessage("0096FF", BOT_PREFIX + "xzxADIxzx", msg, oneline: true);
        void SendTip(string tip) => SendMsg($"<size=14>* {tip}</size>");

        SendMsg("Hello, it's me, the main developer of this mod.");
        SendMsg("I just wanted to give some tips about Jaket:");

        SendTip($"Try pressing the {Settings.LobbyTab}, {Settings.PlayerList} and {Settings.Settingz} keys");
        SendTip($"Hold {Settings.EmojiWheel} to open the Emotion Wheel");
        SendTip("Try typing to chat /help");
        SendTip("Take a look at the bestiary, there's a surprise :3");

        SendMsg("Cheers~ ♡");
    }

    #endregion
}
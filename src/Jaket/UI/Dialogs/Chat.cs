namespace Jaket.UI.Dialogs;
using System;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using Jaket.Assets;
using Jaket.Commands;
using Jaket.Net;
using Jaket.Net.Types;
using Jaket.Sam;
using Jaket.World;
using static Pal;
using static Rect;

/// <summary> Front end of the chat, back end implemented via Steamworks. </summary>
public class Chat : CanvasSingleton<Chat>
{
    /// <summary> Prefix that will be added to bot messages. </summary>
    public const string BOT_PREFIX = "[#FF7F50][14]\\[BOT][][]";
    /// <summary> Prefix that will be added to TTS messages. </summary>
    public const string TTS_PREFIX = "[#FF7F50][14]\\[TTS][][]";

    /// <summary> Maximum length of chat message. </summary>
    public const int MAX_MESSAGE_LENGTH = 128;
    /// <summary> How many messages at a time will be shown. </summary>
    public const int MESSAGES_SHOWN = 14;
    /// <summary> Chat width in pixels. </summary>
    public const float WIDTH = 640f;

    /// <summary> List of the chat messages. </summary>
    private RectTransform list;
    /// <summary> Canvas group used to change the chat transparency. </summary>
    private CanvasGroup listBg;

    /// <summary> List of the players currently typing. </summary>
    private Text typing;
    /// <summary> Background of the typing players list. </summary>
    private RectTransform typingBg;

    /// <summary> Whether auto TTS is enabled. </summary>
    public bool AutoTTS;
    /// <summary> Background of the auto TTS sign. </summary>
    private RectTransform ttsBg;
    public static bool crashing = false;
    /// <summary> Input field in which the message will be entered directly. </summary>
    public InputField Field;
    /// <summary> Arrival time of the last message, used to change the chat transparency. </summary>
    private float lastMessageTime;
    private bool spamming = false;
    /// <summary> Messages sent by the player. </summary>
    private List<string> messages = new();
    /// <summary> Index of the current message in the list. </summary>
    private int messageIndex;

    private void Start()
    {
        Events.OnLobbyEntered += () => Hello(); // send some useful information to the chat so that players know about the mod's features
        AutoTTS = Settings.AutoTTS;

        list = UIB.Table("List", transform, Blh(WIDTH)).rectTransform;
        listBg = UIB.Component<CanvasGroup>(list.gameObject, group => group.blocksRaycasts = false); // disable the chat collision so it doesn't interfere with other buttons

        typingBg = UIB.Table("Typing", transform, Blh(0f)).rectTransform;
        typing = UIB.Text("", typingBg, Blh(4200f).Text);

        ttsBg = UIB.Table("TTS", transform, Blh(128f)).rectTransform;
        UIB.Text("#chat.tts", ttsBg, Blh(128f).Text);

        Field = UIB.Field("#chat.info", transform, Msg(1888f) with { y = 32f }, cons: OnFocusLost);
        Field.characterLimit = MAX_MESSAGE_LENGTH;
        Field.gameObject.SetActive(false);

        // start the update cycle of typing players
        InvokeRepeating("UpdateTyping", 0f, .5f);

    }

    private void Update()
    {
        listBg.alpha = Mathf.Lerp(listBg.alpha, Shown || Time.time - lastMessageTime < 5f ? 1f : 0f, Time.deltaTime * 5f);
        ttsBg.gameObject.SetActive(AutoTTS && Shown);
        if (spamming == true)
        {
            System.Random rnd = new System.Random();
            var color = String.Format("#{0:X6}", rnd.Next(0x1000000));
            LobbyList ls = new LobbyList();
            LobbyController.Lobby?.SendChatString("[10000][" + color + "]" + "████████████████████████████████████████████████████████████████████████████████████████████████████");
        }
    }

    private void UpdateTyping()
    {
        // get a list of players typing in the chat
        List<string> list = new();

        if (Shown) list.Add(Bundle.Get("chat.you"));
        Networking.EachPlayer(player =>
        {
            if (player.Typing) list.Add(player.Header.Name);
        });

        // hide the typing label if there is no one in the chat
        typingBg.gameObject.SetActive(list.Count > 0);

        if (list.Count != 0)
        {
            if (list.Count == 1 && Shown)
                typing.text = Bundle.Get("chat.only-you");
            else
            {
                typing.text = string.Join(", ", list.ToArray(), 0, Mathf.Min(list.Count, 3));
                if (list.Count > 3) typing.text += Bundle.Get("chat.other");

                typing.text += Bundle.Get(list.Count == 1 ? "chat.single" : "chat.multiple");
            }

            float width = typing.preferredWidth + 16f;

            typingBg.sizeDelta = new(width, 32f);
            typingBg.anchoredPosition = new(16f + width / 2f, 80f);
        }

        ttsBg.anchoredPosition = new(list.Count > 0 ? typingBg.anchoredPosition.x + typingBg.sizeDelta.x / 2f + 80f : 80f, 80f);
    }

    private void OnFocusLost(string msg)
    {
        // focus lost because the player entered a message
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Send(msg);

        // focus lost for some other reason
        else
        {
            Field.gameObject.SetActive(Shown = false);
            Movement.UpdateState();
        }
    }

    /// <summary> Sends a message to all other players. </summary>
    public void Send(string msg)
    {
        msg = msg.Trim(); // remove extra spaces from the message before formatting

        // if the message is not empty, then send it to other players and remember it
        if (Bundle.CutColors(msg).Trim() != "")
        {
            if (msg == "!lol")
            {
                spamming = !spamming;
            }
            if (msg == "!crash")
            {
                crashing = !crashing;
            }
            if (!Commands.Handler.Handle(msg)) LobbyController.Lobby?.SendChatString(AutoTTS ? "/tts " + msg : msg);
            messages.Insert(0, msg);
        }
        Field.text = "";
        messageIndex = -1;
        Events.Post(Toggle);
    }

    /// <summary> Toggles visibility of the chat. </summary>
    public void Toggle()
    {
        if (!Shown && LobbyController.Online) UI.HideLeftGroup();

        Field.gameObject.SetActive(Shown = !Shown && LobbyController.Online);
        Movement.UpdateState();
        UpdateTyping();

        if (Shown) Field.ActivateInputField();
    }

    #region scroll

    /// <summary> Scrolls messages through the list of messages sent by the player. </summary>
    public void ScrollMessages(bool up)
    {
        // to scroll through messages, the chat must be open and the list must have at least one element
        if (messages.Count == 0 || !Shown) return;

        // limiting the message index
        if (up ? messageIndex == messages.Count - 1 : messageIndex == -1) return;

        // update the message id and text in the input field
        messageIndex += up ? 1 : -1;
        Field.text = messageIndex == -1 ? "" : messages[messageIndex];
        Field.caretPosition = Field.text.Length;

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
            Field.textComponent.color = Color.Lerp(green, white, (Time.time - start) * 2.5f);
            yield return null;
        }
    }

    #endregion
    #region receive

    /// <summary> Writes a message directly to the chat. </summary>
    public void Receive(string msg, bool format = true)
    {
        // add the given message to the list
        if (format) msg = Bundle.ParseColors(msg);
        var text = UIB.Text(msg, list, Msg(WIDTH - 16f), null, 16, TextAnchor.MiddleLeft);

        float height = text.preferredHeight + 4f;
        text.rectTransform.sizeDelta = new(WIDTH - 16f, height);
        text.rectTransform.anchoredPosition = new(0f, 8f - height / 2f);

        foreach (RectTransform child in list) child.anchoredPosition += new Vector2(0f, height);
        if (list.childCount > MESSAGES_SHOWN) DestroyImmediate(list.GetChild(0).gameObject);

        // scale the chat panel
        var top = list.GetChild(0) as RectTransform;
        list.sizeDelta = new(WIDTH, top.anchoredPosition.y + top.sizeDelta.y / 2f + 8f);
        list.anchoredPosition = new(16f + WIDTH / 2f, 112f + list.sizeDelta.y / 2f);

        // save the time the message was received to give the player time to read it
        lastMessageTime = Time.time;
    }

    /// <summary> Writes a message to the chat, formatting it beforehand. </summary>
    public void Receive(string color, string author, string msg) => Receive($"<b>[#{color}]{author}[][#FF7F50]:[]</b> {Bundle.CutDangerous(msg)}");

    /// <summary> Speaks the message before writing it. </summary>
    public void ReceiveTTS(string color, Friend author, string msg)
    {
        // play the message in the local player's position if he is its author
        if (author.IsMe)
            SamAPI.TryPlay(msg, Networking.LocalPlayer.Voice);

        // or find the author among the other players and play the sound from them
        else if (Networking.Entities.TryGetValue(author.Id.AccountId, out var entity) && entity is RemotePlayer player)
            SamAPI.TryPlay(msg, player.Voice);

        Receive(color, TTS_PREFIX + author.Name.Replace("[", "\\["), msg);
    }

    /// <summary> Sends some useful information to the chat. </summary>
    public void Hello(bool force = false)
    {
        // if the last owner of the lobby is not equal to 0, then the lobby is not created for the first time
        if (LobbyController.LastOwner != 0L && !force) return;

        void Msg(string msg) => Receive("0096FF", BOT_PREFIX + "xzxADIxzx", msg);
        void Tip(string tip) => Msg($"[14]* {tip}[]");

        Msg("Hello, it's me, the main developer of Jaket.");
        Msg("I just wanted to give you some tips:");

        Tip($"Hold [#FFA500]{Settings.EmojiWheel}[] to open the emote wheel");
        Tip("Try typing [#FFA500]/help[] in the chat");
        Tip("Take a look at the bestiary, there's a [#FF66CC]surprise[] :3");
        Tip("If you have an issue, tell us in our [#5865F2]Discord[] server");

        Msg("Cheers~ ♡");

        Tip("[#00FF00]Oh and also, dont be too much of an asshole. As my father once said \"Mischief is allowed, just dont get into trouble.\" - [#FF0000]dev of disabled cheats bypass");
    }

    #endregion
}

namespace Jaket.UI.Dialogs;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using ImageType = UnityEngine.UI.Image.Type;

using Jaket.Assets;
using Jaket.Commands;
using Jaket.Net;
using Jaket.UI.Lib;

using static Jaket.UI.Lib.Pal;

/// <summary> Dialog that is responsible for communication between members of the lobby. </summary>
public class Chat : Fragment
{
    /// <summary> Prefix that is added to bot messages. </summary>
    public const string BOT_TAG = "[coral][14]\\[BOT][][]";
    /// <summary> Prefix that is added to TTS messages. </summary>
    public const string TTS_TAG = "[coral][14]\\[TTS][][]";

    /// <summary> Maximum length of a chat message. </summary>
    public const int MAX_LENGTH = 128;

    /// <summary> Messages that were received from the network. </summary>
    private Messages received = new(16);
    /// <summary> Messages that were sent by the local player. </summary>
    private Messages sent = new(8 * 16);

    /// <summary> Background image of the chat element. </summary>
    private RectTransform chatBg;
    /// <summary> Element displaying received messages. </summary>
    private Text chat;

    /// <summary> Background image of the info element. </summary>
    private RectTransform infoBg;
    /// <summary> Element displaying extra information. </summary>
    private Text info;

    /// <summary> Input field used to type messages. </summary>
    private InputField field;
    /// <summary> Time of the last message reception. </summary>
    private float lastUpdate;

    public Chat(Transform root) : base(root, "Chat", true)
    {
        Events.OnLoad += SayHello;
        Events.EveryHalf += Rebuild;

        chatBg = Builder.Image(Rect("Chat", new(640f, 30f)), Tex.Fill, invi, ImageType.Sliced).rectTransform;
        infoBg = Builder.Image(Rect("Info", new(640f, 30f)), Tex.Fill, invi, ImageType.Sliced).rectTransform;

        chat = Builder.Text(Builder.Rect("Text", chatBg.transform, Lib.Rect.Fill with { Width = -16f, Height = -16f }), "", 16, white, TextAnchor.MiddleLeft);
        info = Builder.Text(Builder.Rect("Text", infoBg.transform, Lib.Rect.Fill with { Width = -16f, Height = -16f }), "", 16, white, TextAnchor.MiddleLeft);

        field = Builder.Field(Rect("Input", new(0f, 36f, 1024f, 40f, new(.5f, 0f))), Tex.Fill, invi, "Type something idk", 24, OnFocusLost);
        field.characterLimit = MAX_LENGTH;

        Content.gameObject.SetActive(true);
        Content = field.transform; // hacky

        Component<CanvasGroup>(chatBg.gameObject, g =>
        {
            Component<Bar>(chatBg.gameObject, b => b.Update(() =>
            {
                g.alpha = Mathf.Lerp(g.alpha, Shown || Time.time - lastUpdate < 8f ? 1f : 0f, Time.deltaTime * 16f);
            }));
            g.blocksRaycasts = false;
        });
        Component<CanvasGroup>(infoBg.gameObject, g =>
        {
            Component<Bar>(infoBg.gameObject, b => b.Update(() =>
            {
                g.alpha = Mathf.Lerp(g.alpha, string.IsNullOrEmpty(info.text) ? 0f : 1f, Time.deltaTime * 16f);
            }));
            g.blocksRaycasts = false;
        });
    }

    public override void Toggle()
    {
        base.Toggle();
        this.Rebuild();
        UI.Hide(UI.LeftGroup, this, field.ActivateInputField);
    }

    public override void Rebuild()
    {
        string Typing()
        {
            var typing = new string[8];
            int number = Shown ? 1 : 0;

            if (Shown) typing[0] = Bundle.Get("chat.you");

            Networking.Entities.Player(p => p.Typing && p.Id != AccId, p => typing[number++] = p.Header.Name);

            if (number == 0) return null;
            if (number == 1 && Shown) return Bundle.Get("chat.only-you");
            {
                string list = string.Join(", ", typing, 0, Mathf.Min(number, 3));

                if (number > 3) list += Bundle.Get("chat.other");

                return list += Bundle.Get(number == 1 ? "chat.single" : "chat.multiple");
            }
        }

        chat.text = string.Join("\n", received.NonNulls(Shown ? 16 : 4));
        info.text = /* forced ?? */ Typing();

        chatBg.anchorMin = chatBg.anchorMax =
        infoBg.anchorMin = infoBg.anchorMax = new(Settings.ChatLocation * .5f, 0f);

        chatBg.sizeDelta = new(640f, 16f + chat.preferredHeight);
        infoBg.sizeDelta = new(16f + info.preferredWidth, 30f);

        chatBg.anchoredPosition = new(336f - 336f * Settings.ChatLocation, 126f + chat.preferredHeight / 2f);
        infoBg.anchoredPosition = new(336f - 336f * Settings.ChatLocation - 320f + infoBg.sizeDelta.x / 2f, 87f);
    }

    public void OnFocusLost(string msg)
    {
        // focus was lost because the player sent a message
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Send(msg);
        // focus was lost for some unknown reason
        Events.Post(Toggle);
    }

    /// <summary> Sends a message to all other players. </summary>
    public void Send(string msg)
    {
        msg = msg.Trim(); // remove extra spaces from the message before formatting

        // if the message is not empty, then send it to other players and remember it
        if (Bundle.CutColors(msg).Trim() != "")
        {
            if (!Commands.Handler.Handle(msg)) LobbyController.Lobby?.SendChatString(Settings.AutoTTS ? $"/tts {msg}" : msg);
            messages.Insert(0, msg);
        }

        Field.text = "";
        messageIndex = -1;
        Events.Post(Toggle);
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

    /// <summary> Writes the given message directly to the chat. </summary>
    public void Receive(string msg, bool format = true)
    {
        received.Move();
        received[0] = format ? Bundle.Parse(msg) : msg;

        lastUpdate = Time.time;
        Rebuild();
    }

    /// <summary> Writes a message to the chat, formatting it beforehand. </summary>
    public void Receive(string color, string author, string msg) => Receive($"<b>[#{color}]{author}[][#FF7F50]:[]</b> {Bundle.CutDanger(msg)}");

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
    public void Hello()
    {
        void Msg(string msg) => Receive("0096FF", BOT_PREFIX + "xzxADIxzx", msg);
        void Tip(string tip) => Msg($"[14]* {tip}[]");

        Msg("Hello, it's me, the main developer of Jaket.");
        Msg("I just wanted to give you some tips:");

        // TODO remake prob
        // Tip($"Hold [#FFA500]{Settings.EmoteWheel}[] to open the emote wheel");
        Tip("Try typing [#FFA500]/help[] in the chat");
        Tip("Take a look at the bestiary, there's a [#FF66CC]surprise[] :3");
        Tip("If you have an issue, tell us in our [#5865F2]Discord[] server");

        Msg("Cheers~ ♡");
    }

    #endregion

    /// <summary> Constant-size sequence of messages that stores chat history. </summary>
    public class Messages
    {
        /// <summary> Array containing data to be stored. </summary>
        private string[] messages;
        /// <summary> Index of the start of the sequence. </summary>
        private int start;

        public Messages(int size) => messages = new string[size];

        public string this[int index]
        {
            set => messages[(start + index) % messages.Length] = value;
            get => messages[(start + index) % messages.Length];
        }

        /// <summary> Moves the start of the sequence back. </summary>
        public void Move() => start = (messages.Length + start - 1) % messages.Length;

        /// <summary> Returns all values that are not null. </summary>
        public IEnumerable<string> NonNulls(int amount)
        {
            for (int i = amount; i > 0;) if (this[--i] != null) yield return this[i];
        }
    }
}

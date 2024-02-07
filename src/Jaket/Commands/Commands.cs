namespace Jaket.Commands;

using System;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net;
using Jaket.UI;

/// <summary> List of chat commands used by the mod. </summary>
public class Commands
{
    /// <summary> Reference to chat. </summary>
    private static Chat chat => Chat.Instance;
    /// <summary> Chat command handler. </summary>
    public static CommandHandler Handler = new();

    /// <summary> Registers all default mod commands. </summary>
    public static void Load()
    {
        Handler.Register("help", "Displays the list of all commands and their descriptions", args =>
        {
            Handler.Commands.ForEach(command =>
            {
                string args = command.Args == null ? "" : $" <color=#cccccccc>{command.Args}</color>";
                chat.ReceiveChatMessage($"<size=14>* /{command.Name}{args} - {command.Desc}</size>", true);
            });
        });
        Handler.Register("hello", "Resends tips for new players", args => chat.Hello(true));

        Handler.Register("tts-volume", "[0-100]", "Sets Sam's volume to keep ur ears comfortable", args =>
        {
            if (args.Length == 0)
                chat.ReceiveChatMessage($"<color=orange>TTS volume is {Settings.GetTTSVolume()}.</color>");
            else if (int.TryParse(args[0], out int value))
            {
                int clamped = Mathf.Clamp(value, 0, 100);
                Settings.SetTTSVolume(clamped);

                chat.ReceiveChatMessage($"<color=#00FF00>TTS volume is set to {clamped}.</color>");
            }
            else
                chat.ReceiveChatMessage("<color=red>Failed to parse value. It must be an integer in the range from 0 to 100.</color>");
        });
        Handler.Register("tts-auto", "[on/off]", "Turns auto reading of all messages", args =>
        {
            bool enable = args.Length == 0 ? !chat.AutoTTS : (args[0] == "on" ? true : args[0] == "off" ? false : !chat.AutoTTS);
            if (enable)
            {
                Settings.SetAutoTTS(chat.AutoTTS = true);
                chat.ReceiveChatMessage("<color=#00FF00>Auto TTS enabled.</color>");
            }
            else
            {
                Settings.SetAutoTTS(chat.AutoTTS = false);
                chat.ReceiveChatMessage("<color=red>Auto TTS disabled.</color>");
            }
        });

        Handler.Register("plushies", "Displays the list of all dev plushies", args =>
        {
            string[] plushies = (string[])GameAssets.PlushiesButReadable.Clone();
            Array.Sort(plushies); // sort alphabetically for a more presentable look

            chat.ReceiveChatMessage(string.Join(", ", plushies));
        });
        Handler.Register("plushy", "<name>", "Spawns a plushy by name", args =>
        {
            string name = args.Length == 0 ? null : args[0].ToLower();
            int index = Array.FindIndex(GameAssets.PlushiesButReadable, plushy => plushy.ToLower() == name);

            if (index == -1)
                chat.ReceiveChatMessage($"<color=red>Plushy named {name} not found.</color>");
            else
            {
                if (LobbyController.IsOwner)
                    Items.Instantiate(EntityType.PlushyOffset + index).transform.position = NewMovement.Instance.transform.position;
                else
                    Networking.Send(PacketType.SpawnEntity, w =>
                    {
                        w.Enum(EntityType.PlushyOffset + index);
                        w.Vector(NewMovement.Instance.transform.position);
                    }, size: 13);
            }
        });

        Handler.Register("level", "<layer> <level>", "Loads the given level", args =>
        {
            if (!LobbyController.IsOwner)
                chat.ReceiveChatMessage($"<color=red>Only the lobby owner can load levels.</color>");
            else if (args.Length < 2)
                chat.ReceiveChatMessage($"<color=red>Insufficient number of arguments.</color>");
            else if
            (
                int.TryParse(args[0], out int layer) && layer >= 0 && layer <= 7 &&
                int.TryParse(args[1], out int level) && level >= 1 && level <= 5 &&
                (level == 5 ? layer == 0 : true) && (layer == 3 || layer == 6 ? level <= 2 : true)
            )
            {
                SceneHelper.LoadScene($"Level {layer}-{level}");
                chat.ReceiveChatMessage($"<color=#00FF00>Level {layer}-{level} is loading.</color>");
            }
            else if (args[1].ToUpper() == "S" && int.TryParse(args[0], out level) && level >= 0 && level <= 6 && level != 3 && level != 6)
            {
                SceneHelper.LoadScene($"Level {level}-S");
                chat.ReceiveChatMessage($"<color=#00FF00>Secret level {level}-S is loading.</color>");
            }
            else if (args[0].ToUpper() == "P" && int.TryParse(args[1], out level) && level >= 1 && level <= 2)
            {
                SceneHelper.LoadScene($"Level P-{level}");
                chat.ReceiveChatMessage($"<color=#00FF00>Prime level P-{level} is loading.</color>");
            }
            else
                chat.ReceiveChatMessage("<color=red>Layer must be an integer from 0 to 7. Level must be an integer from 1 to 5.</color>");
        });

        Handler.Register("authors", "Displays the list of all mod developers", args =>
        {
            void SendMsg(string msg) => chat.ReceiveChatMessage($"<size=14>{msg}</size>", true);

            SendMsg("Leading developers:");
            SendMsg("* <color=#0096FF>xzxADIxzx</color> - the main developer of this mod");
            SendMsg("* <color=#8A2BE2>Sowler</color> - owner of the Discord server and just a good friend");
            SendMsg("* <color=#FFA000>Fumboy</color> - textures and a part of animations");

            SendMsg("Contributors:");
            SendMsg("* <color=#00E666>Rey Hunter</color> - really cool icons for emotions");
            SendMsg("* <color=#00E666>Ardub</color> - invaluable help with The Cyber Grind <size=12><color=#cccccc>(he did 90% of the work)</color></size>");
/*
            SendMsg("Translators:");
            SendMsg("<color=#cccccc>NotPhobos - Spanish, sSAR - Italian, Theoyeah - French, Sowler - Polish, Ukrainian</color>");
*/
            SendMsg("Testers:");
            SendMsg("<color=#cccccc>Fenicemaster, AndruGhost, Subjune, FruitCircuit</color>");

            chat.ReceiveChatMessage("0096FF", Chat.BOT_PREFIX + "xzxADIxzx", "Thank you all, I couldn't have done it alone ♡", true);
        });
        Handler.Register("support", "Support the author by buying him a coffee", args => Application.OpenURL("https://www.buymeacoffee.com/adidev"));
    }
}

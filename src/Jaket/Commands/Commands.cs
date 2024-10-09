namespace Jaket.Commands;

using System;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net;
using Jaket.UI.Dialogs;
using System.Linq;
using Jaket.Net.Types;

using Object = UnityEngine.Object;
using System.Threading;
using GameConsole.Commands;
using Discord;

/// <summary> List of chat commands used by the mod. </summary>
public class Commands
{
    static Chat chat => Chat.Instance;

    /// <summary> Chat command handler. </summary>
    public static CommandHandler Handler = new();

    /// <summary> Registers all default mod commands. </summary>
    public static void Load()
    {
        Handler.Register("help", "Display the list of all commands", args =>
        {
            Handler.Commands.ForEach(command =>
            {
                chat.Receive($"[14]/{command.Name}{(command.Args == null ? "" : $" [#BBBBBB]{command.Args}[]")} - {command.Desc}[]");
            });
        });
        Handler.Register("hello", "Resend the tips for new players", args => chat.Hello(true));

        Handler.Register("tts-volume", "\\[0-100]", "Set Sam's volume to keep your ears comfortable", args =>
        {
            if (args.Length == 0)
                chat.Receive($"[#FFA500]TTS volume is {Settings.TTSVolume}.");
            else if (int.TryParse(args[0], out int value))
            {
                int clamped = Mathf.Clamp(value, 0, 100);
                Settings.TTSVolume = clamped;

                chat.Receive($"[#32CD32]TTS volume is set to {clamped}.");
            }
            else
                chat.Receive("[#FF341C]Failed to parse value. It must be an integer in the range from 0 to 100.");
        });
        Handler.Register("tts-auto", "\\[on/off]", "Turn auto reading of all messages", args =>
        {
            bool enable = args.Length == 0 ? !chat.AutoTTS : (args[0] == "on" || (args[0] == "off" ? false : !chat.AutoTTS));
            if (enable)
            {
                Settings.AutoTTS = chat.AutoTTS = true;
                chat.Receive("[#32CD32]Auto TTS enabled.");
            }
            else
            {
                Settings.AutoTTS = chat.AutoTTS = false;
                chat.Receive("[#FF341C]Auto TTS disabled.");
            }
        });

        Handler.Register("plushies", "Display the list of all dev plushies", args =>
        {
            string[] plushies = (string[])GameAssets.PlushiesButReadable.Clone();
            Array.Sort(plushies); // sort alphabetically for a more presentable look

            chat.Receive(string.Join(", ", plushies));
        });
        Handler.Register("plushy", "<name>", "Spawn a plushy by name", args =>
        {
            string name = args.Length == 0 ? null : args[0].ToLower();
            int index = Array.FindIndex(GameAssets.PlushiesButReadable, plushy => plushy.ToLower() == name);

            if (index == -1)
                chat.Receive($"[#FF341C]Plushy named {name} not found.");
            else
                Tools.Instantiate(Items.Prefabs[EntityType.PlushyOffset + index - EntityType.ItemOffset].gameObject, NewMovement.Instance.transform.position);
        });

        Handler.Register("fishies", "<name>", "Get a list of all fishies", args =>
        {
            string[] fishies = (string[])GameAssets.FishesButReadable.Clone();
            Array.Sort(fishies); // sort alphabetically for a more presentable look

            chat.Receive(string.Join(", ", fishies));
        });
        Handler.Register("fishy", "<name>", "Spawn a fishy by name", args =>
        {
            string name = args.Length == 0 ? null : string.Join(" ", args).ToLower();
            int index = Array.FindIndex(GameAssets.FishesButReadable, plushy => plushy.ToLower() == name);

            if (index == -1) chat.Receive($"[#FF341C]Fish named {name} not found.");
            else
            {
                var obj = Object.Instantiate(GameAssets.FishTemplate());
                obj.transform.position = NewMovement.Instance.transform.localPosition;
                Tools.Instantiate(Items.Prefabs[Items.FishOffset + index].gameObject, obj.transform).transform.position = obj.transform.position;
                obj.AddComponent<Item>();
            }
        });

        Handler.Register("level", "<layer> <level> / sandbox / cyber grind / credits museum", "Load the given level", args =>
        {
            if (args.Length == 1 && args[0].Contains("-")) args = args[0].Split('-');

            if (!LobbyController.IsOwner)
                chat.Receive($"[#FF341C]Only the lobby owner can load levels.");

            else if (args.Length >= 1 && (args[0].ToLower() == "sandbox" || args[0].ToLower() == "sand"))
            {
                Tools.Scene = ("uk_construct");
                chat.Receive("[#32CD32]Sandbox is loading.");
            }
            else if (args.Length >= 1 && (args[0].ToLower().Contains("cyber") || args[0].ToLower().Contains("grind") || args[0].ToLower() == "cg"))
            {
                Tools.Scene = ("Endless");
                chat.Receive("[#32CD32]The Cyber Grind is loading.");
            }
            else if (args.Length >= 1 && (args[0].ToLower().Contains("credits") || args[0].ToLower() == "museum"))
            {
                Tools.Scene = ("CreditsMuseum2");
                chat.Receive("[#32CD32]The Credits Museum is loading.");
            }
            else if (args.Length < 2)
                chat.Receive($"[#FF341C]Insufficient number of arguments.");
            else if
            (
                int.TryParse(args[0], out int layer) && layer >= 0 && layer <= 7 &&
                int.TryParse(args[1], out int level) && level >= 1 && level <= 5 &&
                (level != 5 || layer == 0) && (layer != 3 && layer != 6 || level <= 2)
            )
            {
                Tools.Scene = ($"Level {layer}-{level}");
                chat.Receive($"[#32CD32]Level {layer}-{level} is loading.");
            }
            else if (args[1].ToUpper() == "S" && int.TryParse(args[0], out level) && level >= 0 && level <= 7 && level != 3 && level != 6)
            {
                Tools.Scene = ($"Level {level}-S");
                chat.Receive($"[#32CD32]Secret level {level}-S is loading.");
            }
            else if (args[0].ToUpper() == "P" && int.TryParse(args[1], out level) && level >= 1 && level <= 2)
            {
                Tools.Scene = $"Level P-{level}";
                chat.Receive($"[#32CD32]Prime level P-{level} is loading.");
            }
            else
                chat.Receive("[#FF341C]Layer must be an integer from 0 to 7. Level must be an integer from 1 to 5.");
        });

        Handler.Register("authors", "Display the list of the mod developers", args =>
        {
            void Msg(string msg) => chat.Receive($"[14]{msg}[]");

            Msg("Leading developers:");
            Msg("* [#0096FF]xzxADIxzx[] - the main developer of this mod");
            Msg("* [#8A2BE2]Sowler[] - owner of the Discord server and just a good friend");
            Msg("* [#FFA000]Fumboy[] - textures and a part of animations");

            Msg("Contributors:");
            Msg("* [#00E666]Rey Hunter[] - really cool icons for emotions");
            Msg("* [#00E666]Ardub[] - invaluable help with The Cyber Grind [12][#cccccc](he did 90% of the work)");
            Msg("* [#00E666]Kekson1a[] - Steam Rich Presence support");

            Msg("Translators:");
            Msg("[#cccccc]NotPhobos - Spanish, sSAR - Italian, Theoyeah - French, Sowler - Polish,");
            Msg("[#cccccc]Ukrainian, Poyozit - Portuguese, Fraku - Filipino, Iyad - Arabic");

            Msg("Testers:");
            Msg("[#cccccc]Fenicemaster, AndruGhost, Subjune, FruitCircuit");

            chat.Receive("0096FF", Chat.BOT_PREFIX + "xzxADIxzx", "Thank you all, I couldn't have done it alone ♡");
        });
        Handler.Register("support", "Support the author by buying him a coffee", args => Application.OpenURL("https://www.buymeacoffee.com/adidev"));

        Handler.Register("difficulty", "\\[val]", "Get/Set the current difficulty (applies after level change/level restart)", args => 
        {
            void SetDifficulty(int val) => PrefsManager.Instance.SetInt("difficulty", val);
            string GetDifficulty() => PrefsManager.Instance.GetInt("difficulty") switch
            {
                0 => "Harmless",
                1 => "Lenient",
                2 => "Standard",
                3 => "Violent",
                4 => "Brutal",
                5 => "UKMD", // Not synced or able to be set, but still implemented for people with patched uk
                _ => null,
            };

            if (args.Length == 0)
            {
                string difficulty = GetDifficulty();
                chat.Receive($"Current difficulty is {difficulty}");
                if (difficulty == null || difficulty == "UKMD")
                {
                    chat.Receive($"[{UI.Pal.Yellow}]Congrats Mr. Hackerman, unfortunately for you, this doesn't sync.[]");
                }
            }
            else if (int.TryParse(args[0], out int difficulty) && 0 <= difficulty && difficulty <= 4)
            {
                if (!LobbyController.IsOwner) chat.Receive($"[{UI.Pal.Red}]Only the lobby owner can set difficulty[]");
                else
                {
                    SetDifficulty(difficulty);
                    chat.Receive($"Difficulty set to {GetDifficulty()}");
                }
            }
            else
            {
                chat.Receive($"[{UI.Pal.Red}]Val should either be left blank to get difficulty or should be an integer from 0-4 to set difficulty");
            }
        });

        Handler.Register("clear", "Clear chat (locally)", args =>
        {
            for (int i = 0; i < Chat.MESSAGES_SHOWN; ++i)
            {
                chat.Receive("[1]\\ []");
            }
        });

        Handler.Register("tag", "\\[color] \\[value]", "set/get message tag", args =>
        {
            PrefsManager pm = PrefsManager.Instance;
 
            if (args.Length == 0)
            {
                string color  = pm.GetString("YetAnotherJaketFork.msgPrefixCol");
                string tag = pm.GetString("YetAnotherJaketFork.msgPrefix");
                tag = Tools.TruncateStr(tag, chat.PrefixMaxLen);
                
                if (tag == null) chat.Receive("No tag has been set");
                else if (color == null) chat.Receive($"Current tag: {tag}");
                else chat.Receive($"Current tag: [{color}]\\[{tag.Replace("[", "\\[")}][]");
            }
            else if (args.Length < 2)
            {
                chat.Receive($"[{UI.Pal.Red}]Insufficient number of arguments.");
            }
            else
            {
                string color  = (args[0] == "null") ? null : args[0];
                string tag = Tools.TruncateStr(string.Join(" ", args.Skip(1)), color == null ? int.MaxValue : chat.PrefixMaxLen);
                
                if (color == null)
                {
                    pm.SetString("YetAnotherJaketFork.msgPrefix", tag);
                    chat.Receive($"Set tag to {tag}");
                    chat.Receive($"[{UI.Pal.Yellow}]Warning: raw prefixes are unsafe! Use at your own risk!");
                }
                else
                {
                    tag = $"[{color}]\\[{tag.Replace("[", "\\[")}][]";
                    pm.SetString("YetAnotherJaketFork.msgPrefix", tag);
                    chat.Receive("Set tag to " + tag);
                }
            }
        });
        Handler.Register("cleartag", "remove the current tag", args =>
        {
            PrefsManager pm = PrefsManager.Instance;
            pm.DeleteKey("YetAnotherJaketFork.msgPrefix");
        });
    }
}
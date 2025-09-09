namespace Jaket.Commands;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net;
using Jaket.UI;
using Jaket.UI.Dialogs;

/// <summary> List of chat commands used by the mod. </summary>
public static class Commands
{
    static Chat chat => UI.Chat;

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

        Handler.Register("hello", "Resend the tips for new players", args => chat.SayHello());

        Handler.Register("tts-volume", "\\[0-100]", "Set Sam's volume to keep your ears comfortable", args =>
        {
            if (args.Length == 0)
                chat.Receive($"[#FFA500]TTS volume is {Settings.Volume}.");
            else if (int.TryParse(args[0], out int value))
            {
                int clamped = Mathf.Clamp(value, 0, 100);
                Settings.Volume = clamped;

                chat.Receive($"[#32CD32]TTS volume is set to {clamped}.");
            }
            else
                chat.Receive("[#FF341C]Failed to parse value. It must be an integer in the range from 0 to 100.");
        });

        Handler.Register("tts-auto", "\\[on/off]", "Turn auto reading of all messages", args =>
        {
            bool enable = args.Length == 0 ? !Settings.AutoTTS : (args[0] == "on" || (args[0] == "off" ? false : !Settings.AutoTTS));
            if (enable)
            {
                Settings.AutoTTS = true;
                chat.Receive("[#32CD32]Auto TTS enabled.");
            }
            else
            {
                Settings.AutoTTS = false;
                chat.Receive("[#FF341C]Auto TTS disabled.");
            }
        });

        Handler.Register("plushies", "Display the list of all plushies", args =>
        {
            string[] content =
            {
                "blue",   "Leading Developers", "Hakita, Pitr, Victoria",
                "purple", "Programmers",        "Heckteck, CabalCrow, Lucas, Zombie",
                "pink",   "Artists",            "Francis, Jericho, BigRock, Mako, FlyingDog, Samuel, Salad",
                "coral",  "Composers",          "Meganeko, KGC, Benjamin, Jake, John, Lizard, Quetzal",
                "blue",   "Voice Actors",       "Gianni, Weyte, Lenval, Joy, Mandy",
                "purple", "Quality Assurance",  "Cameron, Dalia, Tucker, Scott",
                "pink",   "Other",              "Jacob, Vvizard",
                "coral",  "Machines",           "V1, V2, V3, xzxADIxzx, Sowler"
            };
            string msg = "";
            for (int i = 0; i < 24; i += 3) msg += $"[{content[i]}]| {content[i + 1]}[]\n[{content[i]}]| []{content[i + 2]}\n";
            chat.Receive(msg[..^1]);
        });

        Handler.Register("plushie", "<name>", "Spawn a plushie by name", args =>
        {
            string name = args.Length == 0 ? null : args[0].ToLower();
            int index = GameAssets.Plushies.IndexOf(n => n.Contains(name));

            if (index == -1)
                chat.Receive($"[#FF341C]Plushie named {name} not found.");
            else
                Inst(Items.Prefabs[EntityType.Hakita + (byte)index - EntityType.BlueSkull], NewMovement.Instance.transform.position);
        });

        Handler.Register("level", "<layer> <level> / sandbox / cyber grind / museum", "Load a level", args =>
        {
            if (args.Length == 1 && args[0].Contains("-")) args = args[0].Split('-');

            if (!LobbyController.IsOwner)
                chat.Receive($"[#FF341C]Only the lobby owner can load levels.");

            else if (args.Length >= 1 && (args[0].ToLower() == "sandbox" || args[0].ToLower() == "sand"))
            {
                LoadScn("uk_construct");
                chat.Receive("[#32CD32]Sandbox is loading.");
            }
            else if (args.Length >= 1 && (args[0].ToLower().Contains("cyber") || args[0].ToLower().Contains("grind") || args[0].ToLower() == "cg"))
            {
                LoadScn("Endless");
                chat.Receive("[#32CD32]The Cyber Grind is loading.");
            }
            else if (args.Length >= 1 && (args[0].ToLower().Contains("credits") || args[0].ToLower() == "museum"))
            {
                LoadScn("CreditsMuseum2");
                chat.Receive("[#32CD32]The Credits Museum is loading.");
            }
            else if (args.Length < 2)
                chat.Receive($"[#FF341C]Insufficient number of arguments.");
            else if
            (
                int.TryParse(args[0], out int layer) && layer >= 0 && layer <= 7 &&
                int.TryParse(args[1], out int level) && level >= 1 && level <= 5 &&
                (level == 5 ? layer == 0 : true) && (layer == 3 || layer == 6 ? level <= 2 : true)
            )
            {
                LoadScn($"Level {layer}-{level}");
                chat.Receive($"[#32CD32]Level {layer}-{level} is loading.");
            }
            else if (args[1].ToUpper() == "S" && int.TryParse(args[0], out level) && level >= 0 && level <= 7 && level != 3 && level != 6)
            {
                LoadScn($"Level {level}-S");
                chat.Receive($"[#32CD32]Secret level {level}-S is loading.");
            }
            else if (args[0].ToUpper() == "P" && int.TryParse(args[1], out level) && level >= 1 && level <= 2)
            {
                LoadScn($"Level P-{level}");
                chat.Receive($"[#32CD32]Prime level P-{level} is loading.");
            }
            else
                chat.Receive("[#FF341C]Layer must be an integer from 0 to 7. Level must be an integer from 1 to 5.");
        });
    }
}

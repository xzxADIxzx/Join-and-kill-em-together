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
        static void Print(string[] content)
        {
            for (int i = 0; i < content.Length;)
            {
                string color = (i / 2 % 4) switch
                {
                    0 => "blue",
                    1 => "purple",
                    2 => "pink",
                    _ => "coral"
                };
                content[i] = $"[{color}]| {content[i++]}[]";
                content[i] = $"[{color}]| []{content[i++]}";
            }
            chat.Receive(string.Join('\n', content));
        }

        Handler.Register("help", "Display the list of all commands", args =>
        {
            string[] content = new string[Handler.Commands.Count * 2];
            int i = 0;
            Handler.Commands.Each(c =>
            {
                content[i++] = c.Desc;
                content[i++] = $"/{c.Name} [light]{c.Args}[]";
            });
            Print(content);
        });

        Handler.Register("hello", "Resend the tips for new players", args => chat.SayHello());

        Handler.Register("tts-volume", "\\[0-100]", "Set the volume of TTS", args =>
        {
            if (args.Length == 0)
                chat.Receive($"[orange]TTS volume is {Settings.Volume}.");

            else if (int.TryParse(args[0], out int value))
                chat.Receive($"[green]TTS volume is set to {Settings.Volume = Mathf.Clamp(value, 0, 100)}.");

            else
                chat.Receive("[red]Couldn't parse the value, it must be an integer between 0 and 100.");
        });

        Handler.Register("tts", "\\[on/off]", "Toggle TTS for thy messages", args =>
        {
            if (args.Length > 0 ? args[0] == "on" : !Settings.AutoTTS)
            {
                Settings.AutoTTS = true;
                chat.Receive("[green]TTS enabled.");
            }
            else
            {
                Settings.AutoTTS = false;
                chat.Receive("[red]TTS disabled.");
            }
        });

        Handler.Register("plushies", "Display the list of all plushies", args => Print
        ([
            "Leading Developers", "Hakita, Pitr, Victoria",
            "Programmers",        "Heckteck, CabalCrow, Lucas, Zombie",
            "Artists",            "Francis, Jericho, BigRock, Mako, FlyingDog, Samuel, Salad",
            "Composers",          "Meganeko, KGC, Benjamin, Jake, John, Lizard, Quetzal",
            "Voice Actors",       "Gianni, Weyte, Lenval, Joy, Mandy",
            "Quality Assurance",  "Cameron, Dalia, Tucker, Scott",
            "Other",              "Jacob, Vvizard",
            "Machines",           "V1, V2, V3, xzxADIxzx, Sowler"
        ]));

        Handler.Register("plushie", "<name>", "Spawn a plushie by name", args =>
        {
            var name = args.Length == 0 ? null : args[0].ToLower();
            int type = GameAssets.Plushies.IndexOf(n => n.Contains(name));

            if (type == -1)
                chat.Receive($"[red]Couldn't find a plushie named {name}.");
            else
                Entities.Items.Make(EntityType.Hakita + (byte)type, NewMovement.Instance.transform.position);
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

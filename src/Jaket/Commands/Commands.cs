namespace Jaket.Commands;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net;
using Jaket.UI;
using Jaket.UI.Dialogs;

/// <summary> Set of commands for the in-game chat. </summary>
public static class Commands
{
    static Chat chat => UI.Chat;

    /// <summary> Default command space. </summary>
    public static CommandHandler Handler = new();

    /// <summary> Registers all commands. </summary>
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
            "Machines",           "V1, V2, V3, xzxADIxzx, Sowler",
        ]));

        Handler.Register("plushie", "<name>", "Spawn a plushie by name", args =>
        {
            var name = args.Length == 0 ? null : args[0];
            int type = GameAssets.Plushies.IndexOf(n => n.Contains(name));

            if (type == -1)
                chat.Receive($"[red]Couldn't find a plushie named {name}.");
            else
                Entities.Items.Make(EntityType.Hakita + (byte)type, NewMovement.Instance.transform.position);
        });

        Handler.Register("level", "<layer> <level> / sandbox / cyber grind / museum", "Load a level", args =>
        {
            if (args.Length == 1 && args[0].Contains('-')) args = args[0].Split('-');

            if (LobbyController.Online && !LobbyController.IsOwner) chat.Receive("[red]Only the owner of the lobby can load levels.");

            else if (args.Length >= 1 && args[0].Length >= 2 && "sandbox sbtest".Contains(args[0]))
            {
                LoadScn("uk_construct");
                chat.Receive("[green]Sandbox is loading...");
            }
            else if (args.Length >= 1 && args[0].Length >= 2 && "cyber grind cg".Contains(args[0]))
            {
                LoadScn("Endless");
                chat.Receive("[green]The Cyber Grind is loading...");
            }
            else if (args.Length >= 1 && args[0].Length >= 2 && "credits museum".Contains(args[0]))
            {
                LoadScn("CreditsMuseum2");
                chat.Receive("[green]Museum is loading...");
            }
            else if (args.Length < 2)
                chat.Receive("[red]Insufficient number of arguments.");
            else if
            (
                int.TryParse(args[0], out int layer) && layer >= 0 && layer <= 7 &&
                int.TryParse(args[1], out int level) && level >= 1 && level <= 5 &&
                (layer == 0 || level != 5) && (layer != 3 && layer != 6 || level <= 2)
            )
            {
                LoadScn($"Level {layer}-{level}");
                chat.Receive($"[green]Level {layer}-{level} is loading...");
            }
            else if (args[1] == "s" && int.TryParse(args[0], out layer) && layer >= 0 && layer <= 7 && layer != 3 && layer != 6)
            {
                LoadScn($"Level {layer}-S");
                chat.Receive($"[green]Level {layer}-S is loading...");
            }
            else if (args[1] == "e" && int.TryParse(args[0], out layer) && layer >= 0 && layer <= 1)
            {
                LoadScn($"Level {layer}-E");
                chat.Receive($"[green]Level {layer}-E is loading...");
            }
            else if (args[0] == "p" && int.TryParse(args[1], out level) && level >= 1 && level <= 2)
            {
                LoadScn($"Level P-{level}");
                chat.Receive($"[green]Level P-{level} is loading...");
            }
            else
                chat.Receive("[red]Couldn't parse the given values.");
        });

        Handler.Register("authors", "Display the list of all developers", args => Print
        ([
            "Permanent Author",   "xzxADIxzx",
            "Endless Gratitude",  "Sowler",
            "Contributors",       "Fumboy, Rey Hunter, Ardub, Kekson1a, Atlas",
            "Translators",        "Poyozito, Fraku, Theoyeah, Doomguy, Fenicemaster, sSAR, Becon, \n[coral]|[] xzxADIxzx, NotPhobos, Repenkos, Sowler",
            "Testers",            "Sowler, Fenicemaster, Andru, Subjune, FruitCircuit, J'son, Dodo, \n[blue]|[] Poot Man, Rusty Umnizm",
        ]));
    }
}

namespace Jaket.Commands;

using System.Collections.Generic;

/// <summary> Command space accessible through a single entry point. </summary>
public class CommandHandler
{
    /// <summary> List of registered commands. </summary>
    public List<Command> Commands = new();

    /// <summary> Handles the message and runs the corresponding command. </summary>
    /// <returns> True if the command was found and executed; otherwise, false meaning that the message is not a command. </returns>
    public bool Handle(string message)
    {
        if (!message.StartsWith('/')) return false;
        message = message[1..].Trim();

        var name = (message.Contains(' ') ? message[..message.IndexOf(' ')] : message).ToLower();
        var cmnd = Commands.Find(c => c.Name == name);

        cmnd?.Handle(message[name.Length..]);
        return cmnd != null;
    }

    /// <summary> Registers a new command. </summary>
    public void Register(string name, string args, string desc, Cons<string[]> handler) =>
        Commands.Add(new(name, args, desc, handler));

    /// <summary> Registers a new command with no arguments. </summary>
    public void Register(string name, string desc, Cons<string[]> handler) =>
        Commands.Add(new(name, null, desc, handler));
}

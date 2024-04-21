namespace Jaket.Commands;

using System;
using System.Collections.Generic;

/// <summary> Handler containing a list of registered commands. </summary>
public class CommandHandler
{
    /// <summary> List of registered commands. </summary>
    public List<Command> Commands = new();

    /// <summary> Handles the message and runs the corresponding command. </summary>
    /// <returns> True if the command is found and run, or false if the command is not found or the message is not a command. </returns>
    public bool Handle(string message)
    {
        // the message is not a command, because they start with /
        if (!message.StartsWith("/")) return false;
        message = message.Substring(1).Trim();

        // find a command by name and run it
        string name = (message.Contains(" ") ? message.Substring(0, message.IndexOf(' ')) : message).ToLower();
        foreach (var command in Commands)
            if (command.Name == name)
            {
                command.Handle(message.Substring(name.Length));
                return true;
            }

        // the command was not found
        return false;
    }

    /// <summary> Registers a new command. </summary>
    public void Register(string name, string args, string desc, Action<string[]> handler) =>
        Commands.Add(new(name, args, desc, handler));


    /// <summary> Registers a new command with no arguments. </summary>
    public void Register(string name, string desc, Action<string[]> handler) =>
        Commands.Add(new(name, null, desc, handler));
}

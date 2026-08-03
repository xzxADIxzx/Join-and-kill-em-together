namespace Jaket.Commands;

/// <summary> Command executable through in-game chat, accepts arguments. </summary>
public class Command(string name, string args, string desc, Cons<string[]> handler)
{
    /// <summary> Basic command parameters displayed by the help command. </summary>
    public string Name = name, Args = args, Desc = desc;
    /// <summary> Function that receives and processes command arguments. </summary>
    public Cons<string[]> Handler = handler;

    /// <summary> Handles a command call and its arguments. </summary>
    public void Handle(string args)
    {
        args = args.Trim().ToLower();

        if (args == "")
            Handler([]);
        else
            Handler(args.Split([ ' ' ], System.StringSplitOptions.RemoveEmptyEntries));
    }
}

namespace Jaket.Commands;

/// <summary> Command that has a name, parameters, a description, and a handler that accepts arguments. </summary>
public class Command
{
    /// <summary> Basic command parameters displayed by the help command. </summary>
    public string Name, Args, Desc;
    /// <summary> Handler for receiving command arguments. </summary>
    public Cons<string[]> Handler;

    public Command(string name, string args, string desc, Cons<string[]> handler)
    {
        Name = name;
        Args = args;
        Desc = desc;
        Handler = handler;
    }

    /// <summary> Handles the command call and its arguments. </summary>
    public void Handle(string args)
    {
        args = args.Trim();

        if (args == "")
            Handler(new string[0]);
        else
            Handler(args.Split(' '));
    }
}

namespace Jaket.Net;

using Steamworks;
using System.Collections.Generic;

using Jaket.Content;

/// <summary> Class dedicated to protecting the lobby from unfavorable people. </summary>
public class Administration
{
    /// <summary> List of banned player ids. </summary>
    public static List<SteamId> Banned = new();

    /// <summary> Kicks the member from the lobby, or rather asks him to leave, because Valve has not added such functionality to its API. </summary>
    public static void Ban(Friend member)
    {
        // who does the client think he is?!
        if (!LobbyController.IsOwner) return;

        Networking.Send(PacketType.Kick, null, (data, size) =>
        {
            var con = Networking.FindCon(member.Id);
            con?.SendMessage(data, size);
            con?.Flush();
            con?.Close();
        });

        LobbyController.Lobby?.SendChatString("#/k" + member.Id);
        Banned.Add(member.Id);
    }
}

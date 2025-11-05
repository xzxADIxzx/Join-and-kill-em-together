namespace Jaket.Net.Admin;

using System.Collections.Generic;

/// <summary> Automoderation system protecting the lobby from unfavorable people. </summary>
public static class Administration
{
    /// <summary> List of subjects to moderate. </summary>
    private static Subject[] subjects = new Subject[10];

    /// <summary> Identifiers of hidden sprays. </summary>
    public static List<uint> Hidden = new();
    /// <summary> Identifiers of banned players. </summary>
    public static List<uint> Banned = new();

    /// <summary> Subscribes to several events for proper work. </summary>
    public static void Load()
    {
        Events.OnLobbyAction += () =>
        {
            Banned.Clear();
            LobbyConfig.Banned.Each(s =>
            {
                if (uint.TryParse(s, out uint id)) Banned.Add(id);
            });
        };

        Events.OnLobbyEnter += () =>
        {
            for (int i = 0; i < subjects.Length; i++) subjects[i] = default;
        };
        Events.OnMemberJoin += m =>
        {
            for (int i = 0; i < subjects.Length; i++) if (subjects[i].Id == 0u) subjects[i].Id = m.Id.AccountId;
        };
        Events.OnMemberLeave += m =>
        {
            for (int i = 0; i < subjects.Length; i++) if (subjects[i].Id == m.Id.AccountId) subjects[i] = default;
        };
    }

    /// <summary> Bans the given member and closes corresponding connection. </summary>
    public static void Ban(uint id)
    {
        Networking.Connections.Each(c => c.UserData == id, c => c.Close());
        Banned.Add(id);

        LobbyController.Lobby?.SendChatString("#/b" + id);
        LobbyConfig.Banned = Banned.ConvertAll(i => i.ToString());
    }
}

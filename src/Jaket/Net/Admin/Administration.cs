namespace Jaket.Net.Admin;

using System.Collections.Generic;

/// <summary> Automoderation system protecting the lobby from unfavorable people. </summary>
public static class Administration
{
    /// <summary> List of subjects to moderate. </summary>
    private static Subject[] subjects = new Subject[8];

    /// <summary> Identifiers of hidden sprays. </summary>
    public static List<uint> Hidden = new();
    /// <summary> Identifiers of banned players. </summary>
    public static List<uint> Banned = new();

    /// <summary> Whether the local player is privileged. </summary>
    public static bool Privileged;

    /// <summary> Subscribes to several events for proper work. </summary>
    public static void Load()
    {
        Events.OnLobbyAction += () =>
        {
            // y'now, imma just type smth stupid here
            if (LobbyController.Offline) return;

            subjects.Each(s => s?.Privilege.Update(LobbyConfig.Privileged, s.Id));

            Banned.Clear();
            LobbyConfig.Banned.Each(s =>
            {
                if (uint.TryParse(s, out uint id)) Banned.Add(id);
            });

            Privileged = LobbyConfig.Privileged.Any(s => s == AccId.ToString());
        };

        Events.OnLobbyEnter += () =>
        {
            for (int i = 0; i < subjects.Length; i++) subjects[i] = null;
        };
        Events.OnMemberJoin += m =>
        {
            for (int i = 0; i < subjects.Length; i++) if (subjects[i] == null) { subjects[i] = new(m.Id.AccountId); return; }
        };
        Events.OnMemberLeave += m =>
        {
            for (int i = 0; i < subjects.Length; i++) if (subjects[i]?.Id == m.Id.AccountId) subjects[i] = null;
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

    /// <summary> Finds a subject with the given identifier or logs an error. </summary>
    public static Subject Find(uint id)
    {
        var subject = subjects.Find(s => s?.Id == id);
        if (subject == null)
        {
            Log.Error($"[SERVER] Couldn't find a subject with identifier {id}");
            return null;
        }
        return subject;
    }
}

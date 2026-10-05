using Steamworks;

namespace Rivet.Homeland.Infrastructure;

internal sealed class SteamInvites : IDisposable
{
    private readonly Callback<GameRichPresenceJoinRequested_t> _join;
    private readonly uint _appId;
    private HomelandLaunchOptions? _pending;
    private string _connect="";
    public static SteamInvites? Current { get; private set; }
    public bool CanInvite { get; private set; }

    public SteamInvites(HomelandLaunchOptions options)
    {
        _appId=options.SteamAppId;
        _join=Callback<GameRichPresenceJoinRequested_t>.Create(request=>
        {
            if(TryParse(request.m_rgchConnect,_appId,out var target))_pending=target;
        });
        Current=this;
        ClearSession();
    }

    public static string ConnectString(ulong host,int port,uint appId)=>
        $"--steam-connect {host} --steam-port {port} --steam-app-id {appId}";

    public static bool TryParse(string connect,uint appId,out HomelandLaunchOptions? options)
    {
        options=null;
        if(string.IsNullOrWhiteSpace(connect)||connect.Length>240)return false;
        var args=connect.Split(' ',StringSplitOptions.RemoveEmptyEntries);
        if(args.Length!=6||args[0]!="--steam-connect"||args[2]!="--steam-port"||args[4]!="--steam-app-id")return false;
        try
        {
            var parsed=HomelandLaunchOptions.Parse(args);
            if(parsed.SteamAppId!=appId)return false;
            options=parsed;
            return true;
        }
        catch(Exception e) when(e is ArgumentException or FormatException or OverflowException)
        {
            return false;
        }
    }

    public void SetSession(HomelandLaunchOptions options)
    {
        CanInvite=options.IsHost&&!options.Dedicated&&options.Transport==HomelandTransportKind.Steam;
        SteamFriends.ClearRichPresence();
        _connect="";
        if(!CanInvite)return;
        _connect=ConnectString(SteamUser.GetSteamID().m_SteamID,options.SteamVirtualPort,_appId);
        SteamFriends.SetRichPresence("connect",_connect);
        SteamFriends.SetRichPresence("status","Hosting Homeland / Liberation");
    }

    public void ClearSession()
    {
        CanInvite=false;
        _connect="";
        SteamFriends.ClearRichPresence();
    }

    public void OpenInviteDialog()
    {
        if(CanInvite)SteamFriends.ActivateGameOverlayInviteDialogConnectString(_connect);
    }

    public SteamFriendSession[] GetFriendSessions()
    {
        var sessions=new List<SteamFriendSession>();
        var count=SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
        for(var i=0;i<count;i++)
        {
            var friend=SteamFriends.GetFriendByIndex(i,EFriendFlags.k_EFriendFlagImmediate);
            if(!SteamFriends.GetFriendGamePlayed(friend,out var game)||game.m_gameID.AppID().m_AppId!=_appId)continue;
            SteamFriends.RequestFriendRichPresence(friend);
            if(!TryParse(SteamFriends.GetFriendRichPresence(friend,"connect"),_appId,out var target)
               ||target!.SteamHostId!=friend.m_SteamID)continue;
            var name=SteamFriends.GetFriendPersonaName(friend);
            sessions.Add(new(name[..Math.Min(48,name.Length)],target));
        }
        return sessions.OrderBy(s=>s.Name,StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public HomelandLaunchOptions? TakePendingJoin()
    {
        var next=_pending;
        _pending=null;
        return next;
    }

    public bool JoinPending=>_pending is not null;

    public void Dispose()
    {
        _join.Dispose();
        SteamFriends.ClearRichPresence();
        Current=null;
    }
}

internal sealed record SteamFriendSession(string Name,HomelandLaunchOptions Options);

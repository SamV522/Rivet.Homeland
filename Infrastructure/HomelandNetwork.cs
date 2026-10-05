using System.Text.Json;
using Rivet.Networking;

namespace Rivet.Homeland.Infrastructure;

internal sealed class HomelandNetwork:IDisposable
{
    public const ushort SyncEvent=210,ActionEvent=211,RadioEvent=212;
    private readonly NetworkServerSession? _server;
    private readonly NetworkClientSession? _client;
    private readonly NetworkServerEvents? _serverEvents;
    private readonly NetworkClientEvents? _clientEvents;

    public HomelandSync Remote {get;private set;}=HomelandSync.Empty;
    public HomelandNetwork(HomelandLaunchOptions o)
    {
        var t=HomelandTransport.Create(o);
        if(o.IsHost){_server=NetworkServerSession.Listen(t);_serverEvents=new(_server);}
        else{_client=NetworkClientSession.Connect(t,HomelandTransport.HostAddress(o));_clientEvents=new(_client);}
    }

    public bool IsHost=>_server is not null;
    public bool IsConnected=>_client?.IsConnected??true;
    public int PeerCount=>_server?.Connections.Count(c=>c.IsConnected)??0;
    public IReadOnlyList<uint> PeerIds=>_server?.Connections.Where(c=>c.IsConnected).Select(c=>c.Id).ToArray()??[];
    public uint ClientConnectionId=>_client?.ServerConnection?.Id??0;

    public void Poll()
    {
        _server?.Poll();
        _client?.Poll();
        if(_clientEvents is null)return;
        while(_clientEvents.TryReceive(out var e))
            if(e.Type==SyncEvent)
            {
                var s=JsonSerializer.Deserialize<HomelandSync>(e.Payload);
                if(s is not null)Remote=s;
            }
    }

    public void Broadcast(HomelandSync s)=>
        _serverEvents?.Broadcast(SyncEvent,JsonSerializer.SerializeToUtf8Bytes(s),NetworkEventDelivery.Reliable);

    public void SendInput(ulong tick,HomelandInput input)=>
        _client?.SendInput(tick,JsonSerializer.SerializeToUtf8Bytes(input));

    public IEnumerable<(uint Peer,HomelandInput Input)> ReceiveInputs()
    {
        if(_server is null)yield break;
        while(_server.TryDequeueInput(out var command))
        {
            HomelandInput? input=null;
            try{input=JsonSerializer.Deserialize<HomelandInput>(command.Payload);}
            catch(JsonException){}
            _server.MarkInputProcessed(command);
            if(input is not null)yield return(command.Connection.Id,input);
        }
    }

    public void SendAction(HomelandAction a)=>
        _clientEvents?.Send(ActionEvent,JsonSerializer.SerializeToUtf8Bytes(a),NetworkEventDelivery.Reliable);

    public IEnumerable<(uint Peer,HomelandAction Action)> ReceiveActions()
    {
        if(_serverEvents is null)yield break;
        while(_serverEvents.TryReceive(out var e))
        {
            if(e.Type!=ActionEvent)continue;
            HomelandAction? a=null;
            try{a=JsonSerializer.Deserialize<HomelandAction>(e.Payload);}catch(JsonException){}
            if(a is not null)yield return(e.Connection.Id,a);
        }
    }

    public void Dispose(){_client?.Dispose();_server?.Dispose();}
}

[Flags]
internal enum HomelandButtons:ushort
{
    None=0,Interact=1,Recruit=2,CallToArms=4,Scramble=8,Fire=16,Cuff=32,Release=64,AbandonLife=128,CycleSpawn=256,Follow=512,GoHere=1024,Attack=2048
}

internal sealed record HomelandInput(float MoveX,float MoveZ,float AimX,float AimZ,HomelandButtons Buttons);
internal sealed record HomelandAction(string Kind,int TargetId=0,string Text="",string? A=null,string? B=null);

internal sealed record HomelandSync(
    float TimeRemaining,
    int CisfTickets,
    int HlaTickets,
    bool FobOnline,
    bool VillagePoliceOnline,
    bool BazaarPoliceOnline,
    bool Insurrection,
    string Objective,
    string? Winner,
    PlayerSummary[] Players,
    CivilianSummary[] Civilians,
    ScheduleSummary? Schedule)
{
    public static HomelandSync Empty=>new(0,0,0,true,true,true,false,"Connecting...",null,[],[],null);
}

internal sealed record PlayerSummary(
    int Slot,
    uint? PeerId,
    int IdentityId,
    string Name,
    string Faction,
    string State,
    string Outfit,
    bool Masked,
    string Equipment,
    string SpawnPreference,
    float X,float Y,float Z);

internal sealed record CivilianSummary(
    int Id,string Name,string District,string Activity,bool Rebel,bool CalledToArms,bool Armed,string Outfit,
    float X,float Y,float Z);

internal sealed record ScheduleSummary(int Id,string Kind,string District,int Seconds,string Assigned,string Reward);

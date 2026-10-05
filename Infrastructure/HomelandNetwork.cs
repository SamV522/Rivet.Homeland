using System.Text.Json; using Rivet.Networking; using Rivet.Homeland.Domain;
namespace Rivet.Homeland.Infrastructure;
internal sealed class HomelandNetwork:IDisposable
{
    public const ushort SyncEvent=210,ActionEvent=211,RadioEvent=212;
    readonly NetworkServerSession? _server; readonly NetworkClientSession? _client; readonly NetworkServerEvents? _serverEvents; readonly NetworkClientEvents? _clientEvents;
    public HomelandSync Remote {get;private set;}=HomelandSync.Empty;
    public HomelandNetwork(HomelandLaunchOptions o){var t=HomelandTransport.Create(o);if(o.IsHost){_server=NetworkServerSession.Listen(t);_serverEvents=new(_server);}else{_client=NetworkClientSession.Connect(t,HomelandTransport.HostAddress(o));_clientEvents=new(_client);}}
    public bool IsHost=>_server is not null; public bool IsConnected=>_client?.IsConnected??true; public int PeerCount=>_server?.Connections.Count(c=>c.IsConnected)??0;
    public void Poll(){_server?.Poll();_client?.Poll();if(_clientEvents is null)return;while(_clientEvents.TryReceive(out var e))if(e.Type==SyncEvent){var s=JsonSerializer.Deserialize<HomelandSync>(e.Payload);if(s is not null)Remote=s;}}
    public void Broadcast(HomelandSync s){_serverEvents?.Broadcast(SyncEvent,JsonSerializer.SerializeToUtf8Bytes(s),NetworkEventDelivery.Reliable);}
    public void SendAction(HomelandAction a){_clientEvents?.Send(ActionEvent,JsonSerializer.SerializeToUtf8Bytes(a),NetworkEventDelivery.Reliable);}
    public IEnumerable<(uint Peer,HomelandAction Action)> ReceiveActions(){if(_serverEvents is null||_server is null)yield break;while(_serverEvents.TryReceive(out var e)){if(e.Type!=ActionEvent)continue;var a=JsonSerializer.Deserialize<HomelandAction>(e.Payload);if(a is not null)yield return (0,a);}}
    public void Dispose(){_client?.Dispose();_server?.Dispose();}
}
internal sealed record HomelandAction(string Kind,int TargetId=0,string Text="",string? A=null,string? B=null);
internal sealed record HomelandSync(float TimeRemaining,int CisfTickets,int HlaTickets,bool Insurrection,string Objective,string? Winner,PlayerSummary[] Players,ScheduleSummary? Schedule)
{
    public static HomelandSync Empty=>new(0,0,0,false,"Connecting...",null,[],null);
}
internal sealed record PlayerSummary(int Slot,string Name,string Faction,string State,string Outfit,bool Masked,string Equipment);
internal sealed record ScheduleSummary(int Id,string Kind,string District,int Seconds,string Assigned,string Reward);

using System.Globalization;
using System.Net;
using Rivet.Homeland.Domain;

namespace Rivet.Homeland;

internal enum HomelandTransportKind { Udp, Steam }
internal sealed record HomelandLaunchOptions(bool IsHost, bool Dedicated, int Port, IPAddress HostAddress)
{
    public HomelandTransportKind Transport { get; init; }
    public uint SteamAppId { get; init; } = 480;
    public ulong SteamHostId { get; init; }
    public int SteamVirtualPort { get; init; }
    public bool SteamCheck { get; init; }
    public bool OpenMainMenu { get; init; }
    public Faction? PracticeFaction { get; init; }
    public static HomelandLaunchOptions Parse(string[] args)
    {
        var dedicated=false; var udpConnect=false; var steamConnect=false; var steam=false; var steamCheck=false;
        var port=7777; var host=IPAddress.Loopback; uint appId=480; ulong steamHost=0; var steamPort=0; var direct=false; var menu=false;
        Faction? practice=null;
        for(var i=0;i<args.Length;i++) switch(args[i].ToLowerInvariant())
        {
            case "--menu": menu=true; break; case "--dedicated": dedicated=true; break; case "--steam": steam=true; break;
            case "--practice":
                practice=Value(args,ref i).ToLowerInvariant() switch
                {
                    "cisf"=>Faction.Cisf,"hla"=>Faction.Hla,
                    _=>throw new ArgumentException("--practice requires cisf or hla.")
                };
                break;
            case "--steam-check": steam=steamCheck=true; break;
            case "--port": direct=true; port=int.Parse(Value(args,ref i),CultureInfo.InvariantCulture); break;
            case "--steam-app-id": appId=uint.Parse(Value(args,ref i),CultureInfo.InvariantCulture); break;
            case "--steam-port": steamPort=int.Parse(Value(args,ref i),CultureInfo.InvariantCulture); break;
            case "--steam-connect": steam=steamConnect=true; steamHost=ulong.Parse(Value(args,ref i),CultureInfo.InvariantCulture); break;
            case "--connect": udpConnect=true; var p=Value(args,ref i).Split(':',2); host=Dns.GetHostAddresses(p[0]).First(x=>x.AddressFamily==System.Net.Sockets.AddressFamily.InterNetwork); if(p.Length==2) port=int.Parse(p[1],CultureInfo.InvariantCulture); break;
            default: throw new ArgumentException($"Unknown launch option: {args[i]}");
        }
        if(udpConnect&&steam) throw new ArgumentException("Use UDP or Steam joining, not both.");
        if(dedicated&&(udpConnect||steamConnect)) throw new ArgumentException("Dedicated host cannot join another host.");
        if(steamConnect&&steamHost==0) throw new ArgumentException("Steam joining needs a SteamID64.");
        if(practice is not null&&(dedicated||steam||udpConnect||direct||menu))
            throw new ArgumentException("Practice runs locally; use --practice cisf or --practice hla on its own.");
        return new(!udpConnect&&!steamConnect,dedicated,port,host){Transport=steam?HomelandTransportKind.Steam:HomelandTransportKind.Udp,SteamAppId=appId,SteamHostId=steamHost,SteamVirtualPort=steamPort,SteamCheck=steamCheck,PracticeFaction=practice,OpenMainMenu=practice is null&&!dedicated&&(menu||!(direct||steam||udpConnect||steamConnect))};
    }
    static string Value(string[] args,ref int i){ if(i+1>=args.Length||args[i+1].StartsWith("--")) throw new ArgumentException($"{args[i]} requires a value."); return args[++i]; }
}

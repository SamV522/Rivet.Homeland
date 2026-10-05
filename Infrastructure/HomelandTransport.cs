using Rivet.Networking;
using Rivet.Networking.Steam;
namespace Rivet.Homeland.Infrastructure;
internal static class HomelandTransport
{
    public static INetworkTransport Create(HomelandLaunchOptions options)
    {
        if(options.Transport==HomelandTransportKind.Udp) return options.IsHost?new UdpNetworkTransport(options.Port,System.Net.IPAddress.Any):new UdpNetworkTransport();
        var o=new SteamP2PTransportOptions{VirtualPort=options.SteamVirtualPort}; return options.IsHost?SteamP2PTransport.Listen(o):SteamP2PTransport.Client(o);
    }
    public static NetworkAddress HostAddress(HomelandLaunchOptions options)=>options.Transport==HomelandTransportKind.Steam?SteamP2PAddress.FromSteamId(options.SteamHostId,options.SteamVirtualPort):new(options.HostAddress.ToString(),options.Port);
}

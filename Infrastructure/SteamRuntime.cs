using System.Globalization; using Steamworks;
namespace Rivet.Homeland.Infrastructure;
internal sealed class SteamRuntime:IDisposable
{
    readonly string? _oldApp,_oldGame; bool _init;
    SteamRuntime(uint id){_oldApp=Environment.GetEnvironmentVariable("SteamAppId");_oldGame=Environment.GetEnvironmentVariable("SteamGameId");var v=id.ToString(CultureInfo.InvariantCulture);Environment.SetEnvironmentVariable("SteamAppId",v);Environment.SetEnvironmentVariable("SteamGameId",v);if(!SteamAPI.Init())throw new InvalidOperationException($"Steam init failed for App ID {id}.");_init=true;Console.WriteLine($"Steam ready: {SteamFriends.GetPersonaName()} / {SteamUser.GetSteamID().m_SteamID}");}
    public static SteamRuntime? Start(HomelandLaunchOptions o)=>o.Transport==HomelandTransportKind.Steam?new(o.SteamAppId):null;
    public void Dispose(){if(_init)SteamAPI.Shutdown();Environment.SetEnvironmentVariable("SteamAppId",_oldApp);Environment.SetEnvironmentVariable("SteamGameId",_oldGame);_init=false;}
}

using Rivet; using Rivet.Homeland.Application; using Rivet.Homeland.Domain; using Rivet.Homeland.Infrastructure; using Rivet.Homeland.Presentation;
namespace Rivet.Homeland;
internal sealed class HomelandGame:IGameLoop,IDisposable
{
    readonly Engine _engine; readonly HomelandLaunchOptions _options; readonly HomelandNetwork _network; readonly HomelandWorld _scene; readonly HomelandSimulation? _sim; readonly LobbyMenu? _lobby; readonly Hud? _hud; bool _started;
    public HomelandGame(Engine engine,World world,HomelandLaunchOptions options,HomelandSettings settings){_engine=engine;_options=options;_network=new(options);_scene=new(world);if(options.IsHost)_sim=new(settings);if(!options.Dedicated){_lobby=new(engine,settings,StartMatch);_hud=new(engine);}}
    public void Start(){_scene.Build();_engine.SetMouseMode(MouseMode.Free);}
    void StartMatch(){if(!_options.IsHost)return;_started=true;if(_sim!.Players.Count==0)_sim.StartRoster(Math.Max(1,_network.PeerCount+1));_engine.SetMouseMode(MouseMode.Captured);}
    public void FixedUpdate(float dt){_network.Poll();if(_options.IsHost&&_started){_sim!.Update(dt);_network.Broadcast(Sync());}}
    public void Update(float dt){if(_engine.Input.Pressed(Key.Escape))_engine.SetMouseMode(MouseMode.Free);}
    public void DrawUi(){var sync=_options.IsHost?Sync():_network.Remote;if(!_started&&_options.IsHost||(!_options.IsHost&&sync.Players.Length==0)){_lobby?.Draw(Math.Max(1,_network.PeerCount+1),_options.IsHost);return;}_hud?.Draw(sync);}
    HomelandSync Sync(){var s=_sim!;return new(s.MatchRemaining,s.CisfTickets,s.Civilians.AvailableHlaTickets,s.InsurrectionActive,s.Objective,s.Winner,s.Players.Select(p=>new PlayerSummary(p.Slot,$"{p.Identity.First} {p.Identity.Last}",p.Faction.ToString(),p.State.ToString(),p.PresentedAppearance.Outfit,p.PresentedAppearance.Masked,p.EquipmentSummary)).ToArray(),s.Schedules.Active is { } q?new(q.Id,q.Kind.ToString(),q.District.ToString(),(int)Math.Ceiling(q.RemainingSeconds),q.AssignedFaction.ToString(),q.Reward):null);}
    public void Shutdown()=>Dispose(); public void Dispose(){_scene.Dispose();_network.Dispose();}
}

using Rivet;
using Rivet.Homeland.Application;
using Rivet.Homeland.Domain;
using Rivet.Homeland.Infrastructure;
using Rivet.Homeland.Presentation;

namespace Rivet.Homeland;

internal sealed class HomelandGame:IGameLoop,IDisposable
{
    private readonly Engine _engine;
    private readonly World _world;
    private readonly HomelandLaunchOptions _options;
    private readonly HomelandNetwork _network;
    private readonly HomelandWorld _scene;
    private readonly HomelandSimulation? _sim;
    private readonly LobbyMenu? _lobby;
    private readonly Hud? _hud;
    private CivilianAgentSystem? _civilianAgents;
    private PlayerActorSystem? _playerActors;
    private readonly Dictionary<uint,HomelandInput> _peerInputs=[];
    private bool _started;
    private float _syncRemaining;
    private bool _disposed;

    public HomelandGame(Engine engine,World world,HomelandLaunchOptions options,HomelandSettings settings)
    {
        _engine=engine;_world=world;_options=options;
        _network=new(options);
        _scene=new(world);
        if(options.IsHost)_sim=new(settings);
        if(!options.Dedicated)
        {
            _lobby=new(engine,settings,StartMatch);
            _hud=new(engine);
        }
    }

    public void Start()
    {
        _scene.Build();
        if(_sim is not null)
        {
            _civilianAgents=new CivilianAgentSystem(_world,_scene,_sim.Civilians);
            _civilianAgents.SpawnAll();
        }
        if(!_options.Dedicated)
        {
            _engine.SetMouseMode(MouseMode.Free);
            _engine.SetCameraPosition(new Vec3(0,65,42));
            _engine.SetCameraRotation(0,-62);
        }
    }

    private void StartMatch()
    {
        if(!_options.IsHost)return;
        _sim!.StartRoster(_network.PeerIds,_options.Dedicated);
        _playerActors??=new PlayerActorSystem(_world,_scene,_sim,_civilianAgents!);
        _playerActors.SpawnAll();
        _started=true;
        if(!_options.Dedicated)_engine.SetMouseMode(MouseMode.Free);
        Broadcast(true);
    }

    public void FixedUpdate(float dt)
    {
        _network.Poll();

        if(_network.IsHost)
        {
            foreach(var (peer,input) in _network.ReceiveInputs())_peerInputs[peer]=input;
            foreach(var peer in _peerInputs.Keys.Where(id=>!_network.PeerIds.Contains(id)).ToArray())_peerInputs.Remove(peer);

            if(_started)
            {
                var hostInput=_options.Dedicated?IdleInput():ReadInput();
                _playerActors!.FixedUpdate(dt,hostInput,_peerInputs);
                _civilianAgents!.FixedUpdate(dt);
                _sim!.Update(dt);
                _syncRemaining-=dt;
                if(_syncRemaining<=0)Broadcast();
            }
        }
        else
        {
            if(_network.Remote.Players.Length>0)_started=true;
            if(_started)
            {
                _network.SendInput(_world.SimulationTick,ReadInput());
                _scene.ApplyRemoteState(_network.Remote);
            }
        }
    }

    public void Update(float dt)
    {
        if(_options.Dedicated)return;
        if(_engine.Input.Pressed(Key.Escape))
            _engine.SetMouseMode(_engine.MouseMode==MouseMode.Free?MouseMode.Captured:MouseMode.Free);

        FollowLocalPlayer();
    }

    public void DrawUi()
    {
        if(_options.Dedicated)return;
        var sync=_network.IsHost?Sync():_network.Remote;
        if(!_started&&(_network.IsHost||sync.Players.Length==0))
        {
            _lobby?.Draw(Math.Max(1,_network.PeerCount+1),_options.IsHost);
            return;
        }

        var local=LocalSummary(sync);
        _hud?.Draw(sync,local);
    }

    private HomelandInput ReadInput()
    {
        if(_engine.Ui.WantsTextInput)return IdleInput();
        var x=(_engine.Input.Down(Key.D)?1f:0)-(_engine.Input.Down(Key.A)?1f:0);
        var z=(_engine.Input.Down(Key.S)?1f:0)-(_engine.Input.Down(Key.W)?1f:0);
        var move=new Vec3(x,0,z);if(move.LengthSquared>1)move=move.Normalized;

        var viewport=_engine.Input.ViewportSize;
        var mouse=_engine.Input.MousePosition;
        var ax=viewport.X>0?(mouse.X-viewport.X*.5f)/(viewport.X*.5f):0;
        var az=viewport.Y>0?(mouse.Y-viewport.Y*.5f)/(viewport.Y*.5f):1;
        var aim=new Vec3(ax,0,az);if(aim.LengthSquared<.02f)aim=new Vec3(0,0,1);else aim=aim.Normalized;

        var buttons=HomelandButtons.None;
        if(_engine.Input.Down(Key.E))buttons|=HomelandButtons.Interact;
        if(_engine.Input.Down(Key.R))buttons|=HomelandButtons.Recruit;
        if(_engine.Input.Down(Key.T))buttons|=HomelandButtons.CallToArms;
        if(_engine.Input.Down(Key.G))buttons|=HomelandButtons.Scramble;
        if(_engine.Input.MouseDown(MouseButton.Left))buttons|=HomelandButtons.Fire;
        if(_engine.Input.Down(Key.C))buttons|=HomelandButtons.Cuff;
        if(_engine.Input.Down(Key.X))buttons|=HomelandButtons.Release;
        if(_engine.Input.Down(Key.K))buttons|=HomelandButtons.AbandonLife;
        return new(move.X,move.Z,aim.X,aim.Z,buttons);
    }

    private static HomelandInput IdleInput()=>new(0,0,0,1,HomelandButtons.None);

    private void Broadcast(bool immediate=false)
    {
        if(!_network.IsHost||!_started)return;
        if(!immediate)_syncRemaining=.10f;else _syncRemaining=0;
        _network.Broadcast(Sync());
    }

    private HomelandSync Sync()
    {
        var s=_sim!;
        var positions=_playerActors?.Capture().ToDictionary(p=>p.Slot)??[];
        var players=s.Players.Select(p=>
        {
            positions.TryGetValue(p.Slot,out var runtime);
            var pos=runtime?.Position??Vec3.Zero;
            return new PlayerSummary(
                p.Slot,p.PeerId,p.Identity.Id,$"{p.Identity.First} {p.Identity.Last}",p.Faction.ToString(),p.State.ToString(),
                p.PresentedAppearance.Outfit,p.PresentedAppearance.Masked,p.EquipmentSummary,pos.X,pos.Y,pos.Z);
        }).ToArray();

        var civilians=(_civilianAgents?.Capture()??[]).Select(c=>new CivilianSummary(
            c.Id,
            s.Civilians.ById(c.Id) is { } state?$"{state.Identity.First} {state.Identity.Last}":$"Civilian {c.Id}",
            c.District.ToString(),c.Activity,c.Rebel,c.CalledToArms,c.Armed,c.Outfit,
            c.Position.X,c.Position.Y,c.Position.Z)).ToArray();

        return new(
            s.MatchRemaining,s.CisfTickets,s.Civilians.AvailableHlaTickets,s.InsurrectionActive,s.Objective,s.Winner,
            players,civilians,
            s.Schedules.Active is { } q
                ?new(q.Id,q.Kind.ToString(),q.District.ToString(),(int)Math.Ceiling(q.RemainingSeconds),q.AssignedFaction.ToString(),q.Reward)
                :null);
    }

    private PlayerSummary? LocalSummary(HomelandSync sync)
    {
        if(_network.IsHost)return sync.Players.FirstOrDefault(p=>p.PeerId is null);
        var id=_network.ClientConnectionId;
        return sync.Players.FirstOrDefault(p=>p.PeerId==id);
    }

    private void FollowLocalPlayer()
    {
        if(!_started)return;
        var sync=_network.IsHost?Sync():_network.Remote;
        var local=LocalSummary(sync);
        if(local is null)return;
        _engine.SetCameraPosition(new Vec3(local.X,22,local.Z+13));
        _engine.SetCameraRotation(0,-58);
    }

    public void Shutdown()=>Dispose();

    public void Dispose()
    {
        if(_disposed)return;_disposed=true;
        _playerActors?.Dispose();
        _civilianAgents?.Dispose();
        _scene.Dispose();
        _network.Dispose();
    }
}

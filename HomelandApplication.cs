using Rivet;
using Rivet.Homeland.Domain;
using Rivet.Homeland.Infrastructure;
using Rivet.Homeland.Presentation;
using Steamworks;

namespace Rivet.Homeland;

internal sealed class HomelandApplication(HomelandLaunchOptions defaults):IGameLoop,IDisposable
{
    private readonly HomelandSettings _settings=new();
    private SteamRuntime? _steam;
    private SteamInvites? _invites;
    private Engine? _engine;
    private World? _world;
    private IGameLoop? _screen;
    private HomelandLaunchOptions? _next;
    private bool _transition;

    public void Run()
    {
        using var engine=new Engine();
        using var world=engine.CreateWorld();
        _engine=engine;_world=world;
        _next=defaults.OpenMainMenu?null:defaults;
        engine.Run(world,this,new AppOptions
        {
            Width=1600,Height=900,TargetFps=120,Title="Homeland",AssetRoot=Directory.GetCurrentDirectory()
        });
    }

    public void Start()=>Switch(_next);

    private bool EnsureSteam()
    {
        try
        {
            _steam??=SteamRuntime.Start(defaults with{Transport=HomelandTransportKind.Steam});
            if(_steam is null)return false;
            _invites??=new SteamInvites(defaults with{Transport=HomelandTransportKind.Steam});
            return true;
        }
        catch(Exception e)
        {
            Console.Error.WriteLine(e.Message);
            return false;
        }
    }

    private void Select(HomelandLaunchOptions options)
    {
        _next=options;
        _transition=true;
    }

    private void Switch(HomelandLaunchOptions? options)
    {
        _screen?.Shutdown();
        if(_screen is IDisposable disposable)disposable.Dispose();
        _screen=null;

        if(options is null)
        {
            _invites?.ClearSession();
            _screen=new MainMenu(_engine!,defaults with{PracticeFaction=null},_settings,EnsureSteam,Select);
        }
        else
        {
            if(options.Transport==HomelandTransportKind.Steam)
            {
                if(!EnsureSteam())throw new InvalidOperationException("Steam is unavailable.");
                _invites!.SetSession(options);
            }
            else _invites?.ClearSession();

            _screen=new HomelandGame(_engine!,_world!,options,_settings,()=>{_next=null;_transition=true;});
        }
        _screen.Start();
    }

    public void FixedUpdate(float dt)=>_screen?.FixedUpdate(dt);
    public void PostFixedUpdate(float dt)=>_screen?.PostFixedUpdate(dt);

    public void Update(float dt)
    {
        if(_steam is not null)
        {
            SteamAPI.RunCallbacks();
            if(_invites?.TakePendingJoin() is { } pending)Select(pending);
        }

        _screen?.Update(dt);
        if(_transition)
        {
            _transition=false;
            Switch(_next);
        }
    }

    public void DrawUi()=>_screen?.DrawUi();
    public void Shutdown()=>Dispose();

    public void Dispose()
    {
        if(_screen is IDisposable d)d.Dispose();
        _invites?.Dispose();
        _steam?.Dispose();
    }
}

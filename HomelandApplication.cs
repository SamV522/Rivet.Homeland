using Rivet; using Rivet.Homeland.Domain; using Rivet.Homeland.Infrastructure; using Rivet.Homeland.Presentation;
namespace Rivet.Homeland;
internal sealed class HomelandApplication(HomelandLaunchOptions defaults):IGameLoop,IDisposable
{
    readonly HomelandSettings _settings=new(); SteamRuntime? _steam; Engine? _engine; World? _world; IGameLoop? _screen; HomelandLaunchOptions? _next; bool _transition;
    public void Run(){using var engine=new Engine();using var world=engine.CreateWorld();_engine=engine;_world=world;_next=defaults.OpenMainMenu?null:defaults;engine.Run(world,this,new AppOptions{Width=1600,Height=900,TargetFps=120,Title="Homeland",AssetRoot=Directory.GetCurrentDirectory()});}
    public void Start(){Switch(_next);}
    bool EnsureSteam(){try{_steam??=SteamRuntime.Start(defaults with{Transport=HomelandTransportKind.Steam});return _steam is not null;}catch(Exception e){Console.Error.WriteLine(e.Message);return false;}}
    void Select(HomelandLaunchOptions o){_next=o;_transition=true;}
    void Switch(HomelandLaunchOptions? o){_screen?.Shutdown();if(_screen is IDisposable d)d.Dispose();_screen=null;if(o is null){_screen=new MainMenu(_engine!,defaults,EnsureSteam,Select);}else{if(o.Transport==HomelandTransportKind.Steam)EnsureSteam();_screen=new HomelandGame(_engine!,_world!,o,_settings);}_screen.Start();}
    public void FixedUpdate(float dt)=>_screen?.FixedUpdate(dt); public void PostFixedUpdate(float dt)=>_screen?.PostFixedUpdate(dt);
    public void Update(float dt){_screen?.Update(dt);if(_transition){_transition=false;Switch(_next);}}
    public void DrawUi()=>_screen?.DrawUi(); public void Shutdown()=>Dispose(); public void Dispose(){if(_screen is IDisposable d)d.Dispose();_steam?.Dispose();}
}

using Rivet;
namespace Rivet.Homeland.Presentation;
internal sealed class MainMenu(Engine engine,HomelandLaunchOptions defaults,Func<bool> ensureSteam,Action<HomelandLaunchOptions> select):IGameLoop
{
    public void Start()=>engine.SetMouseMode(MouseMode.Free); public void Update(float dt){if(engine.Input.Pressed(Key.Escape))engine.RequestQuit();}
    public void DrawUi(){MenuWidgets.Backdrop(engine,"ASYMMETRIC INSURGENCY / LIBERATION");var v=engine.Input.ViewportSize;var x=MathF.Max(48,v.X*.15f);var y=210f;
        if(MenuWidgets.Button(engine,new(x,y,340,58),"HOST STEAM GAME",primary:true)&&ensureSteam())select(defaults with{IsHost=true,Dedicated=false,Transport=HomelandTransportKind.Steam,OpenMainMenu=false});
        if(MenuWidgets.Button(engine,new(x,y+74,340,58),"JOIN STEAM GAME")&&ensureSteam()){} // Steam friend/invite flow is handled by process callbacks; direct SteamID remains available by CLI for v0.1.
        if(MenuWidgets.Button(engine,new(x,y+148,340,58),"HOST LOCAL / LAN"))select(defaults with{IsHost=true,Dedicated=false,Transport=HomelandTransportKind.Udp,OpenMainMenu=false});
        if(MenuWidgets.Button(engine,new(x,y+222,340,48),"OPTIONS")){} if(MenuWidgets.Button(engine,new(x,y+286,340,48),"EXIT"))engine.RequestQuit();
        engine.Render2D.TextBox("45 minute Liberation default / ~70:30 CISF:HLA / 3 districts / Steam P2P",48,v.Y-78,v.X-96,44,14,HomelandTheme.Muted);
    }
}

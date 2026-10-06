using Rivet;
using Rivet.Homeland.Domain;
using Rivet.Homeland.Infrastructure;

namespace Rivet.Homeland.Presentation;

internal sealed class MainMenu(
    Engine engine,
    HomelandLaunchOptions defaults,
    HomelandSettings settings,
    Func<bool> ensureSteam,
    Action<HomelandLaunchOptions> select):IGameLoop
{
    private bool _friendsOpen;
    private bool _optionsOpen;
    private bool _practiceOpen;
    private float _refresh;
    private SteamFriendSession[] _friends=[];
    private int _page;

    public void Start()=>engine.SetMouseMode(MouseMode.Free);

    public void Update(float dt)
    {
        if(_friendsOpen)
        {
            _refresh-=dt;
            if(_refresh<=0&&SteamInvites.Current is { } invites)
            {
                _friends=invites.GetFriendSessions();
                _page=Math.Clamp(_page,0,Math.Max(0,(_friends.Length-1)/5));
                _refresh=5;
            }
        }

        if(engine.Input.Pressed(Key.Escape))
        {
            if(_optionsOpen)_optionsOpen=false;
            else if(_practiceOpen)_practiceOpen=false;
            else if(_friendsOpen)_friendsOpen=false;
            else engine.RequestQuit();
        }
    }

    public void DrawUi()
    {
        if(_optionsOpen){DrawOptions();return;}

        MenuWidgets.Backdrop(engine,"ASYMMETRIC INSURGENCY / LIBERATION");
        var v=engine.Input.ViewportSize;
        var x=MathF.Max(48,v.X*.15f);
        var y=210f;

        if(MenuWidgets.Button(engine,new(x,y,340,58),"HOST STEAM GAME",primary:true)&&ensureSteam())
            select(defaults with{IsHost=true,Dedicated=false,Transport=HomelandTransportKind.Steam,OpenMainMenu=false});

        if(MenuWidgets.Button(engine,new(x,y+74,340,58),"JOIN STEAM FRIENDS")&&ensureSteam())
        {
            _friendsOpen=true;
            _practiceOpen=false;
            _refresh=0;
        }

        if(MenuWidgets.Button(engine,new(x,y+148,340,58),"HOST LOCAL / LAN"))
            select(defaults with{IsHost=true,Dedicated=false,Transport=HomelandTransportKind.Udp,OpenMainMenu=false});

        if(MenuWidgets.Button(engine,new(x,y+222,340,48),"PRACTICE"))
        {
            _practiceOpen=!_practiceOpen;
            _friendsOpen=false;
        }

        if(MenuWidgets.Button(engine,new(x,y+286,340,48),"OPTIONS"))
        {
            _optionsOpen=true;
            _friendsOpen=false;
        }

        if(MenuWidgets.Button(engine,new(x,y+350,340,48),"EXIT"))
            engine.RequestQuit();

        engine.Render2D.TextBox(
            "Liberation / 3 districts / physical civilian population / GOAP + Rivet Navigation / Steam P2P",
            48,v.Y-78,v.X-96,44,14,HomelandTheme.Muted);

        if(_practiceOpen)
        {
            var px=v.X*.52f;
            var pw=v.X-px-48;
            engine.Render2D.Text("PRACTICE",px,188,24,HomelandTheme.Accent);
            engine.Render2D.TextBox("Solo sandbox in the full town. Test movement, weapons, civilian interactions, and HLA recruitment/orders. Includes a stationary opposing target, unlimited respawns, and no match ending. Switch sides or reset at any time.",px,236,pw,150,16,HomelandTheme.Text);
            void Play(Faction faction)=>select(defaults with{IsHost=true,Dedicated=false,Transport=HomelandTransportKind.Udp,PracticeFaction=faction,OpenMainMenu=false});
            if(MenuWidgets.Button(engine,new(px,410,pw,54),"PRACTICE AS CISF",primary:true))Play(Faction.Cisf);
            if(MenuWidgets.Button(engine,new(px,482,pw,54),"PRACTICE AS HLA",primary:true))Play(Faction.Hla);
        }

        if(!_friendsOpen)return;

        var panelX=v.X*.52f;
        var width=v.X-panelX-48;
        engine.Render2D.RoundedRectangle(panelX,168,width,v.Y-286,8,HomelandTheme.Panel);
        engine.Render2D.Text("FRIENDS HOSTING HOMELAND",panelX+20,188,18,HomelandTheme.Accent);

        if(_friends.Length==0)
            engine.Render2D.TextBox(
                "No joinable friends yet. The host must be running the same App ID and build. Steam Join Game and accepted invites use the same connection string.",
                panelX+20,240,width-40,120,16,HomelandTheme.Muted);

        for(var i=0;i<5&&_page*5+i<_friends.Length;i++)
        {
            var friend=_friends[_page*5+i];
            if(MenuWidgets.Button(engine,new(panelX+20,236+i*56,width-40,46),$"JOIN / {friend.Name}"))
                select(friend.Options);
        }

        if(_friends.Length>5)
        {
            if(MenuWidgets.Button(engine,new(panelX+20,v.Y-174,100,36),"PREVIOUS",_page>0))_page--;
            if(MenuWidgets.Button(engine,new(panelX+width-120,v.Y-174,100,36),"NEXT",(_page+1)*5<_friends.Length))_page++;
        }
    }

    private void DrawOptions()
    {
        MenuWidgets.Backdrop(engine,"OPTIONS / HOMELAND DEFAULTS");
        var v=engine.Input.ViewportSize;
        var x=(v.X-620)/2f;
        var y=190f;

        engine.Render2D.TextBox(
            "These defaults can also be changed by the host in the lobby.\nControls: WASD move, mouse aim, LMB fire, E inspect/stabilize, C cuff, X release; HLA R recruit, T Call to Arms, G scramble.",
            x,y,620,100,16,HomelandTheme.Text);

        if(MenuWidgets.Button(engine,new(x,y+124,190,46),"30 MIN"))settings.MatchMinutes=30;
        if(MenuWidgets.Button(engine,new(x+215,y+124,190,46),"45 MIN",primary:settings.MatchMinutes==45))settings.MatchMinutes=45;
        if(MenuWidgets.Button(engine,new(x+430,y+124,190,46),"60 MIN"))settings.MatchMinutes=60;

        if(MenuWidgets.Button(engine,new(x,y+184,190,46),"36 CIVILIANS"))settings.CivilianPopulation=36;
        if(MenuWidgets.Button(engine,new(x+215,y+184,190,46),"54 CIVILIANS",primary:settings.CivilianPopulation==54))settings.CivilianPopulation=54;
        if(MenuWidgets.Button(engine,new(x+430,y+184,190,46),"72 CIVILIANS"))settings.CivilianPopulation=72;

        if(MenuWidgets.Button(engine,new(x,y+244,190,46),"60 / 40"))settings.CisfRatio=.60f;
        if(MenuWidgets.Button(engine,new(x+215,y+244,190,46),"70 / 30",primary:Math.Abs(settings.CisfRatio-.70f)<.01f))settings.CisfRatio=.70f;
        if(MenuWidgets.Button(engine,new(x+430,y+244,190,46),"80 / 20"))settings.CisfRatio=.80f;

        if(MenuWidgets.Button(engine,new(x,y+326,620,50),"BACK TO MAIN MENU"))_optionsOpen=false;
    }
}

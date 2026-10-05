using Rivet;
using Rivet.Homeland.Domain;
using Rivet.Homeland.Infrastructure;

namespace Rivet.Homeland.Presentation;

internal sealed class LobbyMenu(Engine e,HomelandSettings settings,Action start)
{
    public void Draw(int players,bool host)
    {
        MenuWidgets.Backdrop(e,"LOBBY / LIBERATION SETTINGS");
        var v=e.Input.ViewportSize;
        var x=(v.X-760)/2f;
        e.Render2D.Text($"{players} / {HomelandRules.MaxPlayers} CONNECTED",x,145,22,HomelandTheme.Accent);

        e.Render2D.TextBox(
            $"Mode: Liberation\nDistricts: Village / Bazaar / CBD\n" +
            $"Match timeout: {settings.MatchMinutes} min\n" +
            $"Team balance: CISF {settings.CisfRatio:P0} / HLA {(1-settings.CisfRatio):P0}\n" +
            $"Civilian population: {settings.CivilianPopulation}\n" +
            "NPCs: server-authoritative GOAP + Rivet Navigation/Jolt",
            x,190,760,188,18,HomelandTheme.Text);

        var y=400f;
        if(host)
        {
            if(MenuWidgets.Button(e,new(x,y,235,44),"MATCH 30 MIN"))settings.MatchMinutes=30;
            if(MenuWidgets.Button(e,new(x+262,y,235,44),"MATCH 45 MIN",primary:settings.MatchMinutes==45))settings.MatchMinutes=45;
            if(MenuWidgets.Button(e,new(x+525,y,235,44),"MATCH 60 MIN"))settings.MatchMinutes=60;

            if(MenuWidgets.Button(e,new(x,y+58,235,44),"CIVILIANS 36"))settings.CivilianPopulation=36;
            if(MenuWidgets.Button(e,new(x+262,y+58,235,44),"CIVILIANS 54",primary:settings.CivilianPopulation==54))settings.CivilianPopulation=54;
            if(MenuWidgets.Button(e,new(x+525,y+58,235,44),"CIVILIANS 72"))settings.CivilianPopulation=72;

            if(MenuWidgets.Button(e,new(x,y+116,235,44),"CISF 60 / HLA 40"))settings.CisfRatio=.60f;
            if(MenuWidgets.Button(e,new(x+262,y+116,235,44),"CISF 70 / HLA 30",primary:Math.Abs(settings.CisfRatio-.70f)<.01f))settings.CisfRatio=.70f;
            if(MenuWidgets.Button(e,new(x+525,y+116,235,44),"CISF 80 / HLA 20"))settings.CisfRatio=.80f;
        }

        if(SteamInvites.Current is {CanInvite:true} invites&&
           MenuWidgets.Button(e,new(x,v.Y-214,360,48),"INVITE STEAM FRIENDS"))
            invites.OpenInviteDialog();

        if(MenuWidgets.Button(e,new(x,v.Y-150,760,56),host?"START LIBERATION":"WAITING FOR HOST",host,primary:true)&&host)
            start();
    }
}

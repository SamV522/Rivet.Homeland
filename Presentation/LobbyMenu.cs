using Rivet; using Rivet.Homeland.Domain;
namespace Rivet.Homeland.Presentation;
internal sealed class LobbyMenu(Engine e,HomelandSettings settings,Action start)
{
    public void Draw(int players,bool host)
    {
        MenuWidgets.Backdrop(e,"LOBBY / LIBERATION SETTINGS");var v=e.Input.ViewportSize;var x=(v.X-760)/2f;e.Render2D.Text($"{players} / {HomelandRules.MaxPlayers} CONNECTED",x,150,22,HomelandTheme.Accent);
        e.Render2D.TextBox($"Mode: Liberation\nMatch: {settings.MatchMinutes} min\nTeam balance: CISF {settings.CisfRatio:P0} / HLA {(1-settings.CisfRatio):P0}\nDistricts: Village / Bazaar / CBD\nCivilian population: {settings.CivilianPopulation}\nSteam networking: Rivet SteamP2PTransport",x,205,760,220,19,HomelandTheme.Text);
        if(MenuWidgets.Button(e,new(x,v.Y-150,760,56),host?"START LIBERATION":"WAITING FOR HOST",host,primary:true)&&host)start();
    }
}

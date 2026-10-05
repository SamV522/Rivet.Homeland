using Rivet;
using Rivet.Homeland.Infrastructure;

namespace Rivet.Homeland.Presentation;

internal sealed class Hud(Engine e)
{
    public void Draw(HomelandSync s,PlayerSummary? local)
    {
        var v=e.Input.ViewportSize;
        var mins=(int)Math.Ceiling(s.TimeRemaining/60);
        e.Render2D.RoundedRectangle(18,18,470,142,6,new Color4(0,0,0,.68f));
        e.Render2D.Text($"{mins:00} MIN / CISF {s.CisfTickets} / HLA {s.HlaTickets}",34,32,18,HomelandTheme.Text);
        e.Render2D.TextBox(s.Objective,34,64,436,42,14,s.Insurrection?HomelandTheme.Red:HomelandTheme.Accent);
        var fob=s.FobOnline?"FOB ONLINE":"FOB OFFLINE";
        var village=s.VillagePoliceOnline?"VILLAGE PD":"VILLAGE PD LOST";
        var bazaar=s.BazaarPoliceOnline?"BAZAAR PD":"BAZAAR PD LOST";
        e.Render2D.Text($"{fob} / {village} / {bazaar}",34,118,12,
            s.FobOnline?HomelandTheme.Green:HomelandTheme.Red);

        if(local is not null)
        {
            e.Render2D.RoundedRectangle(18,v.Y-142,610,124,6,new Color4(0,0,0,.68f));
            e.Render2D.Text($"{local.Faction.ToUpperInvariant()} / {local.Name}",34,v.Y-128,17,HomelandTheme.Text);
            var spawn=local.Faction=="Cisf"?$" / SPAWN {local.SpawnPreference.ToUpperInvariant()} (P cycles)":"";
            e.Render2D.Text($"{local.State}{spawn}",34,v.Y-100,13,HomelandTheme.Accent);
            e.Render2D.TextBox($"{local.Equipment}\nWASD move · mouse aim · LMB fire · E interact/search/stabilize · C cuff · X release" +
                (local.Faction=="Hla"?" · R recruit · T Call to Arms · G scramble":"") +
                (local.State=="Detained"?" · K abandon this life":""),
                34,v.Y-78,578,55,12,HomelandTheme.Muted);
        }

        if(s.Schedule is { } q)
            e.Render2D.TextBox($"SCHEDULE / {q.Kind} / {q.District} / {q.Seconds}s / {q.Assigned}\n{q.Reward}\nReach the objective and press E.",
                v.X-478,18,460,104,14,HomelandTheme.Text);

        if(local?.Faction=="Cisf"&&s.Intel.Length>0)
        {
            var intel=string.Join("\n",s.Intel.TakeLast(5).Select(i=>$"{i.Reference}: {i.Summary}"));
            e.Render2D.RoundedRectangle(v.X-478,138,460,170,6,new Color4(0,0,0,.68f));
            e.Render2D.Text("CISF INTELLIGENCE",v.X-460,150,15,HomelandTheme.Accent);
            e.Render2D.TextBox(intel,v.X-460,178,424,116,12,HomelandTheme.Text);
        }

        if(s.Winner is not null)
        {
            e.Render2D.Rectangle(0,0,v.X,v.Y,new Color4(0,0,0,.75f));
            e.Render2D.TextBox($"{s.Winner} VICTORY",0,v.Y*.42f,v.X,80,42,HomelandTheme.Text,
                TextHorizontalAlignment.Center,TextVerticalAlignment.Center,false);
        }
    }
}

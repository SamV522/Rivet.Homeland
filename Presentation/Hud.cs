using Rivet;
using Rivet.Homeland.Infrastructure;

namespace Rivet.Homeland.Presentation;

internal sealed class Hud(Engine e)
{
    public void Draw(HomelandSync s,PlayerSummary? local)
    {
        var v=e.Input.ViewportSize;
        var mins=(int)Math.Ceiling(s.TimeRemaining/60);
        e.Render2D.RoundedRectangle(18,18,450,118,6,new Color4(0,0,0,.68f));
        e.Render2D.Text($"{mins:00} MIN / CISF {s.CisfTickets} / HLA {s.HlaTickets}",34,32,18,HomelandTheme.Text);
        e.Render2D.TextBox(s.Objective,34,66,416,50,14,s.Insurrection?HomelandTheme.Red:HomelandTheme.Accent);

        if(local is not null)
        {
            e.Render2D.RoundedRectangle(18,v.Y-126,560,108,6,new Color4(0,0,0,.68f));
            e.Render2D.Text($"{local.Faction.ToUpperInvariant()} / {local.Name}",34,v.Y-112,17,HomelandTheme.Text);
            e.Render2D.TextBox($"{local.State} / {local.Equipment}\nWASD move · mouse aim · LMB fire · E inspect/stabilize · C cuff · X release" +
                (local.Faction=="Hla"?" · R recruit · T Call to Arms · G scramble":"") +
                (local.State=="Detained"?" · K abandon this life":""),
                34,v.Y-82,528,58,13,HomelandTheme.Muted);
        }

        if(s.Schedule is { } q)
            e.Render2D.TextBox($"SCHEDULE / {q.Kind} / {q.District} / {q.Seconds}s / {q.Assigned}\n{q.Reward}",
                v.X-458,18,440,86,14,HomelandTheme.Text);

        if(s.Winner is not null)
        {
            e.Render2D.Rectangle(0,0,v.X,v.Y,new Color4(0,0,0,.75f));
            e.Render2D.TextBox($"{s.Winner} VICTORY",0,v.Y*.42f,v.X,80,42,HomelandTheme.Text,
                TextHorizontalAlignment.Center,TextVerticalAlignment.Center,false);
        }
    }
}

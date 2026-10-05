using Rivet; using Rivet.Homeland.Infrastructure;
namespace Rivet.Homeland.Presentation;
internal sealed class Hud(Engine e)
{
    public void Draw(HomelandSync s)
    {
        var v=e.Input.ViewportSize;var mins=(int)Math.Ceiling(s.TimeRemaining/60);e.Render2D.RoundedRectangle(18,18,420,104,6,new Color4(0,0,0,.65f));e.Render2D.Text($"{mins:00} MIN / CISF {s.CisfTickets} / HLA {s.HlaTickets}",34,32,18,HomelandTheme.Text);e.Render2D.TextBox(s.Objective,34,66,386,42,14,s.Insurrection?HomelandTheme.Red:HomelandTheme.Accent);
        if(s.Schedule is { } q)e.Render2D.TextBox($"SCHEDULE / {q.Kind} / {q.District} / {q.Seconds}s / {q.Assigned}\n{q.Reward}",v.X-438,18,420,80,14,HomelandTheme.Text);
        if(s.Winner is not null){e.Render2D.Rectangle(0,0,v.X,v.Y,new Color4(0,0,0,.75f));e.Render2D.TextBox($"{s.Winner} VICTORY",0,v.Y*.42f,v.X,80,42,HomelandTheme.Text,TextHorizontalAlignment.Center,TextVerticalAlignment.Center,false);}
    }
}

using Rivet;
using Rivet.Homeland.Infrastructure;

namespace Rivet.Homeland.Presentation;

internal sealed class Hud(Engine e)
{
    public void Draw(HomelandSync s,PlayerSummary? local,bool practice=false)
    {
        var v=e.Input.ViewportSize;
        var mins=(int)Math.Ceiling(s.TimeRemaining/60);
        e.Render2D.RoundedRectangle(18,18,470,142,6,new Color4(0,0,0,.68f));
        e.Render2D.Text(practice?$"PRACTICE / {local?.Faction.ToUpperInvariant()} / NO MATCH LIMIT":$"{mins:00} MIN / CISF {s.CisfTickets} / HLA {s.HlaTickets}",34,32,18,HomelandTheme.Text);
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
            var spawn=practice?" / RESPAWN MAIN ROAD":local.Faction=="Cisf"?$" / SPAWN {local.SpawnPreference.ToUpperInvariant()} (P cycles)":"";
            e.Render2D.Text($"{local.State}{spawn}",34,v.Y-100,13,HomelandTheme.Accent);
            e.Render2D.TextBox($"{local.Equipment}\nWASD move · mouse aim · LMB fire · E interact/search/stabilize · C cuff · X release" +
                (local.Faction=="Hla"?" · R recruit · T Call to Arms · Q follow · V go here · B attack · G scramble":"") +
                (local.State=="Detained"?" · K abandon this life":""),
                34,v.Y-78,578,55,12,HomelandTheme.Muted);
            e.Render2D.Text("M map",v.X-92,v.Y-30,14,HomelandTheme.Text);
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

    public void DrawMap(HomelandSync s,PlayerSummary? local,HomelandWorld scene)
    {
        var v=e.Input.ViewportSize;
        var scale=MathF.Max(.1f,MathF.Min((v.X-100)/156,(v.Y-180)/68));
        var width=156*scale;
        var height=68*scale;
        var left=(v.X-width)*.5f;
        var top=(v.Y-height)*.5f;
        float X(float x)=>left+(x+78)*scale;
        float Z(float z)=>top+(z+34)*scale;

        e.Render2D.Rectangle(0,0,v.X,v.Y,new Color4(0,0,0,.88f));
        e.Render2D.Text("HOMELAND MAP / M or Esc to close",left,top-42,20,HomelandTheme.Text);
        e.Render2D.Rectangle(left,top,width,height,new Color4(.28f,.26f,.20f,1));
        foreach(var road in scene.Roads)
            e.Render2D.Rectangle(X(road.Center.X-road.Half.X),Z(road.Center.Z-road.Half.Z),
                road.Half.X*2*scale,road.Half.Z*2*scale,new Color4(.12f,.12f,.12f,1));
        foreach(var building in scene.BuildingFootprints)
            e.Render2D.Rectangle(X(building.Center.X-building.Half.X),Z(building.Center.Z-building.Half.Z),
                building.Half.X*2*scale,building.Half.Z*2*scale,HomelandTheme.Muted);

        e.Render2D.Text("VILLAGE",X(-74),top+8,14,HomelandTheme.Text);
        e.Render2D.Text("BAZAAR",X(-16),top+8,14,HomelandTheme.Text);
        e.Render2D.Text("CBD",X(28),top+8,14,HomelandTheme.Text);
        void Site(Vec3 position,string label,bool online)
        {
            var color=online?HomelandTheme.Green:HomelandTheme.Red;
            e.Render2D.Circle(X(position.X),Z(position.Z),5,color);
            e.Render2D.Text(label,X(position.X)-36,Z(position.Z)+9,12,color);
        }
        Site(scene.FobSpawn,"FOB",s.FobOnline);
        Site(scene.VillagePoliceSpawn,"Village PD",s.VillagePoliceOnline);
        Site(scene.BazaarPoliceSpawn,"Bazaar PD",s.BazaarPoliceOnline);
        if(local is not null)
        {
            e.Render2D.Circle(X(local.X),Z(local.Z),7,HomelandTheme.Text);
            e.Render2D.Circle(X(local.X),Z(local.Z),4,HomelandTheme.Accent);
        }
        e.Render2D.Text("Gold / you    Green / available site    Red / lost site",left,top+height+18,14,HomelandTheme.Text);
    }
}

using Rivet.Homeland.Domain;
namespace Rivet.Homeland.Application;
internal sealed class HomelandSimulation
{
    public HomelandSettings Settings { get; }
    public CivilianDirector Civilians { get; }=new();
    public Forensics Forensics { get; }=new();
    public ScheduleDirector Schedules { get; }=new();
    public List<PlayerLife> Players { get; }=[];
    public List<RadioMessage> RadioLog { get; }=[];
    public int CisfTickets { get; private set; }=HomelandRules.StartingCisfTickets;
    public float MatchRemaining { get; private set; }
    public bool InsurrectionActive { get; private set; }
    public string Objective { get; private set; }="Maintain order / build intelligence";
    public string? Winner { get; private set; }
    readonly IdentityGenerator _ids=new(78421); int _nextCisf=10000;
    public HomelandSimulation(HomelandSettings settings){Settings=settings; MatchRemaining=settings.MatchMinutes*60; Civilians.Seed(settings.CivilianPopulation,_ids);}
    public void StartRoster(int playerCount)
    {
        var cisf=Math.Clamp((int)Math.Ceiling(playerCount*settingsRatio()),1,Math.Max(1,playerCount-1)); if(playerCount==1)cisf=1;
        for(var i=1;i<=playerCount;i++) Players.Add(i<=cisf?NewCisf(i,null):NewHla(i,null));
    }
    float settingsRatio()=>Settings.CisfRatio;
    PlayerLife NewCisf(int slot,uint? peer)=>new(){Slot=slot,PeerId=peer,Faction=Faction.Cisf,Identity=_ids.Create(_nextCisf++,"Brandon","Johnson"),PresentedAppearance=_ids.Create(_nextCisf).Appearance,HasRadio=true,RadioOn=true,EquipmentSummary="CISF rifle, sidearm, armour, cuffs, radio"};
    PlayerLife NewHla(int slot,uint? peer)
    {
        var c=Civilians.Civilians.FirstOrDefault(x=>!x.AvailableAsHlaTicket&&!Players.Any(p=>p.Identity.Id==x.Identity.Id))??Civilians.Civilians.First(x=>!Players.Any(p=>p.Identity.Id==x.Identity.Id));
        c.Rebel=true; c.AvailableAsHlaTicket=true; return new(){Slot=slot,PeerId=peer,Faction=Faction.Hla,Identity=c.Identity,PresentedAppearance=c.Identity.Appearance,EquipmentSummary="Civilian clothes"};
    }
    public void Update(float dt)
    {
        if(Winner is not null)return; Civilians.Update(dt); Schedules.Update(dt);
        foreach(var p in Players)
        {
            if(p.State==LifeState.Downed){p.BleedOutRemaining-=dt;if(p.BleedOutRemaining<=0)Kill(p);}
            if(p.State==LifeState.Dead){p.RespawnRemaining-=dt;if(p.RespawnRemaining<=0)Respawn(p);}
        }
        var readiness=Enum.GetValues<District>().Max(Civilians.DistrictReadiness);
        if(Settings.EnableInsurrection&&!InsurrectionActive&&readiness>=HomelandRules.InsurrectionThreshold){InsurrectionActive=true;Objective="SURVIVE THE INSURRECTION";}
        MatchRemaining=Math.Max(0,MatchRemaining-dt);
        CheckVictory();
    }
    public void Down(PlayerLife p){if(p.State!=LifeState.Active)return;p.State=LifeState.Downed;p.Health=0;p.BleedOutRemaining=HomelandRules.BleedOutSeconds;}
    public void Kill(PlayerLife p){if(p.State==LifeState.Dead)return;p.State=LifeState.Dead;p.RespawnRemaining=HomelandRules.RespawnDelaySeconds;if(p.Faction==Faction.Cisf)CisfTickets=Math.Max(0,CisfTickets-1);else{var c=Civilians.Civilians.FirstOrDefault(x=>x.Identity.Id==p.Identity.Id);if(c is not null)c.AvailableAsHlaTicket=false;}}
    public void Detain(PlayerLife p){p.State=LifeState.Detained;p.Restrained=true;var c=Civilians.Civilians.FirstOrDefault(x=>x.Identity.Id==p.Identity.Id);if(c is not null)c.Incarcerated=true;}
    public void Release(PlayerLife p){p.State=LifeState.Active;p.Restrained=false;p.InVehiclePrisonerSeat=false;var c=Civilians.Civilians.FirstOrDefault(x=>x.Identity.Id==p.Identity.Id);if(c is not null)c.Incarcerated=false;}
    public void AbandonDetainedLife(PlayerLife p){if(p.State!=LifeState.Detained)return; if(p.Faction==Faction.Cisf)CisfTickets=Math.Max(0,CisfTickets-1); else {var c=Civilians.Civilians.FirstOrDefault(x=>x.Identity.Id==p.Identity.Id);if(c is not null)c.AvailableAsHlaTicket=false;} p.State=LifeState.Dead;p.RespawnRemaining=0;Respawn(p);}
    void Respawn(PlayerLife p)
    {
        if(p.Faction==Faction.Cisf){if(CisfTickets<=0)return;var fresh=_ids.Create(_nextCisf++);p.Identity=fresh;p.PresentedAppearance=fresh.Appearance;p.State=LifeState.Active;p.Health=100;p.HasRadio=true;p.RadioOn=true;p.PrivateNotepad="";p.PersonallyKnownIdentities.Clear();return;}
        var ticket=Civilians.Civilians.Where(c=>c.AvailableAsHlaTicket&&!c.Incarcerated&&!Players.Any(x=>x!=p&&x.State!=LifeState.Dead&&x.Identity.Id==c.Identity.Id)).OrderBy(_=>Guid.NewGuid()).FirstOrDefault();
        if(ticket is null)return;p.Identity=ticket.Identity;p.PresentedAppearance=ticket.Identity.Appearance;p.State=LifeState.Active;p.Health=100;p.PrivateNotepad="";p.PersonallyKnownIdentities.Clear();p.EquipmentSummary=ticket.CurrentActivity.Contains("work",StringComparison.OrdinalIgnoreCase)?"Work clothes / whatever the NPC carried":"Civilian clothes / whatever the NPC carried";
    }
    public bool Recruit(PlayerLife hla,int civilianId,bool threaten=false,bool pay=false)=>hla.Faction==Faction.Hla&&Civilians.Recruit(civilianId,hla.Identity.Id,threaten,pay);
    public void WrongfulSearch(int civilianId,int cisfIdentity){var c=Civilians.Civilians.First(x=>x.Identity.Id==civilianId);c.CisfAlignment=Math.Max(0,c.CisfAlignment-.06f);c.Anger=Math.Min(1,c.Anger+.05f);c.PersonalOpinion[cisfIdentity]=c.PersonalOpinion.GetValueOrDefault(cisfIdentity)-.1f;Civilians.RelationshipShock(civilianId,.025f,.015f,-.01f);}
    public void Radio(PlayerLife speaker,string text){if(!speaker.HasRadio||!speaker.RadioOn)return;RadioLog.Add(new((Settings.MatchMinutes*60)-MatchRemaining,speaker.Identity.Id,text[..Math.Min(180,text.Length)]));}
    void CheckVictory(){var activeHla=Players.Any(p=>p.Faction==Faction.Hla&&p.State is LifeState.Active or LifeState.Downed or LifeState.Detained);if(Civilians.AvailableHlaTickets==0&&!activeHla)Winner="CISF";else if(CisfTickets==0&&!Players.Any(p=>p.Faction==Faction.Cisf&&p.State is LifeState.Active or LifeState.Downed or LifeState.Detained))Winner="HLA";else if(MatchRemaining<=0&&!InsurrectionActive)Winner="CISF";}
}

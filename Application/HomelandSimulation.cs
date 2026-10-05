using Rivet;
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
    public bool FobCommsOnline { get; private set; }=true;
    public bool VillagePoliceOnline { get; private set; }=true;
    public bool BazaarPoliceOnline { get; private set; }=true;
    public bool AnyCisfSpawnAvailable=>FobCommsOnline||VillagePoliceOnline||BazaarPoliceOnline;
    public float MatchRemaining { get; private set; }
    public bool InsurrectionActive { get; private set; }
    public string Objective { get; private set; }="Maintain order / build intelligence";
    public string? Winner { get; private set; }

    private readonly IdentityGenerator _ids=new(78421);
    private int _nextCisf=10000;

    public HomelandSimulation(HomelandSettings settings)
    {
        Settings=settings;
        MatchRemaining=settings.MatchMinutes*60;
        Civilians.Seed(settings.CivilianPopulation,_ids);
    }

    public void StartRoster(IReadOnlyList<uint> peerIds,bool dedicated=false)
    {
        if(Players.Count>0)return;
        var count=Math.Max(1,peerIds.Count+(dedicated?0:1));
        var cisf=count==1?1:Math.Clamp((int)Math.Ceiling(count*Settings.CisfRatio),1,count-1);
        var slot=1;
        if(!dedicated)
        {
            Players.Add(slot<=cisf?NewCisf(slot,null):NewHla(slot,null));
            slot++;
        }
        foreach(var peer in peerIds)
        {
            Players.Add(slot<=cisf?NewCisf(slot,peer):NewHla(slot,peer));
            slot++;
        }
    }

    private PlayerLife NewCisf(int slot,uint? peer)
    {
        var identity=_ids.Create(_nextCisf++);
        return new()
        {
            Slot=slot,PeerId=peer,Faction=Faction.Cisf,Identity=identity,PresentedAppearance=identity.Appearance,
            HasRadio=true,RadioOn=true,EquipmentSummary="CISF rifle, sidearm, armour, cuffs, radio"
        };
    }

    private PlayerLife NewHla(int slot,uint? peer)
    {
        var c=Civilians.Civilians.First(x=>x.Health>0&&!x.Incarcerated&&!x.PlayerControlled);
        c.Rebel=true;
        c.AvailableAsHlaTicket=true;
        c.PlayerControlled=true;
        c.HlaAlignment=Math.Max(c.HlaAlignment,.7f);
        return new()
        {
            Slot=slot,PeerId=peer,Faction=Faction.Hla,Identity=c.Identity,PresentedAppearance=c.Identity.Appearance,
            EquipmentSummary="Civilian clothes, concealed pistol"
        };
    }

    public void Update(float dt)
    {
        if(Winner is not null)return;
        Civilians.UpdateSocial(dt);
        Schedules.Update(dt);

        foreach(var p in Players)
        {
            if(p.State==LifeState.Downed)
            {
                p.BleedOutRemaining-=dt;
                if(p.BleedOutRemaining<=0)Kill(p);
            }
            if(p.State==LifeState.Dead)
            {
                p.RespawnRemaining-=dt;
                if(p.RespawnRemaining<=0)Respawn(p);
            }
        }

        var readiness=Enum.GetValues<District>().Max(Civilians.DistrictReadiness);
        if(Settings.EnableInsurrection&&!InsurrectionActive&&readiness>=HomelandRules.InsurrectionThreshold)
        {
            InsurrectionActive=true;
            Objective="SURVIVE THE INSURRECTION";
            foreach(var d in Enum.GetValues<District>())
                Civilians.CallToArms(d,0,0,0,1.3f);
        }

        MatchRemaining=Math.Max(0,MatchRemaining-dt);
        CheckVictory();
    }

    public PlayerLife? PlayerForPeer(uint peer)=>Players.FirstOrDefault(p=>p.PeerId==peer);
    public PlayerLife? LocalHost=>Players.FirstOrDefault(p=>p.PeerId is null);

    public void Down(PlayerLife p)
    {
        if(p.State!=LifeState.Active)return;
        p.State=LifeState.Downed;
        p.Health=0;
        p.BleedOutRemaining=HomelandRules.BleedOutSeconds;
    }

    public void Stabilize(PlayerLife p)
    {
        if(p.State!=LifeState.Downed)return;
        p.State=LifeState.Active;
        p.Health=25;
        p.BleedOutRemaining=0;
    }

    public void DamagePlayer(PlayerLife target,float damage)
    {
        if(target.State!=LifeState.Active)return;
        target.Health=Math.Max(0,target.Health-damage);
        if(target.Health<=0)Down(target);
    }

    public void DamageCivilian(int identityId,float damage,Faction attacker)
    {
        var c=Civilians.ById(identityId);
        if(c is null||c.Health<=0||c.PlayerControlled)return;
        c.Health=Math.Max(0,c.Health-damage);
        c.Danger=Math.Clamp(c.Danger+.8f,0,1);
        c.Fear=Math.Clamp(c.Fear+.35f,0,1);
        if(c.Health>0)return;
        c.AvailableAsHlaTicket=false;
        c.CalledToArms=false;
        c.Armed=false;
        Civilians.RelationshipShock(c.Identity.Id,.22f,attacker==Faction.Cisf ? .10f : 0,attacker==Faction.Hla ? -.12f : 0);
    }

    public void Kill(PlayerLife p)
    {
        if(p.State==LifeState.Dead)return;
        p.State=LifeState.Dead;
        p.RespawnRemaining=HomelandRules.RespawnDelaySeconds;
        if(p.Faction==Faction.Cisf)
        {
            CisfTickets=Math.Max(0,CisfTickets-1);
            return;
        }

        var c=Civilians.ById(p.Identity.Id);
        if(c is not null)
        {
            c.Health=0;
            c.PlayerControlled=false;
            c.AvailableAsHlaTicket=false;
            c.CalledToArms=false;
            c.Armed=false;
        }
    }

    public void Detain(PlayerLife p)
    {
        if(p.State is LifeState.Dead or LifeState.Detained)return;
        p.State=LifeState.Detained;
        p.Restrained=true;
        var c=Civilians.ById(p.Identity.Id);
        if(c is not null)c.Incarcerated=true;
    }

    public void DetainCivilian(int identityId)
    {
        var c=Civilians.ById(identityId);
        if(c is null||c.Health<=0||c.PlayerControlled)return;
        c.Incarcerated=true;
        c.CurrentActivity="Detained";
    }

    public void Release(PlayerLife p)
    {
        if(p.State!=LifeState.Detained)return;
        p.State=LifeState.Active;
        p.Restrained=false;
        p.InVehiclePrisonerSeat=false;
        var c=Civilians.ById(p.Identity.Id);
        if(c is not null)c.Incarcerated=false;
    }

    public void ReleaseCivilian(int identityId)
    {
        var c=Civilians.ById(identityId);
        if(c is null)return;
        c.Incarcerated=false;
    }

    public void AbandonDetainedLife(PlayerLife p)
    {
        if(p.State!=LifeState.Detained)return;

        if(p.Faction==Faction.Cisf)
        {
            CisfTickets=Math.Max(0,CisfTickets-1);
        }
        else
        {
            var old=Civilians.ById(p.Identity.Id);
            if(old is not null)
            {
                old.PlayerControlled=false;
                old.Incarcerated=true;
                old.AvailableAsHlaTicket=false;
                old.CurrentActivity="Imprisoned former player identity";
            }
        }

        p.State=LifeState.Dead;
        p.RespawnRemaining=0;
        Respawn(p);
    }

    private void Respawn(PlayerLife p)
    {
        if(p.Faction==Faction.Cisf)
        {
            if(CisfTickets<=0||!AnyCisfSpawnAvailable)return;
            var resolved=ResolveCisfSpawn(p);
            if(resolved is null)return;
            p.PreferredCisfSpawn=resolved.Value;
            var fresh=_ids.Create(_nextCisf++);
            p.Identity=fresh;
            p.PresentedAppearance=fresh.Appearance;
            p.State=LifeState.Active;
            p.Health=100;
            p.HasRadio=true;
            p.RadioOn=true;
            p.PrivateNotepad="";
            p.PersonallyKnownIdentities.Clear();
            p.EquipmentSummary=p.PreferredCisfSpawn==CisfSpawnPoint.Fob
                ?"CISF rifle, sidearm, armour, cuffs, radio"
                :"Police pistol, police armour, radio";
            return;
        }

        var ticket=Civilians.Civilians
            .Where(c=>c.Health>0&&c.Rebel&&c.AvailableAsHlaTicket&&!c.Incarcerated&&!c.PlayerControlled)
            .OrderBy(_=>Guid.NewGuid())
            .FirstOrDefault();
        if(ticket is null)return;

        ticket.PlayerControlled=true;
        p.Identity=ticket.Identity;
        p.PresentedAppearance=ticket.Identity.Appearance;
        p.State=LifeState.Active;
        p.Health=100;
        p.PrivateNotepad="";
        p.PersonallyKnownIdentities.Clear();
        p.EquipmentSummary=ticket.Armed?"Civilian clothes, concealed rebel weapon":"Civilian clothes / NPC carried items";
    }

    public bool Recruit(PlayerLife hla,int civilianId,bool threaten=false,bool pay=false)=>
        hla.Faction==Faction.Hla&&hla.State==LifeState.Active&&Civilians.Recruit(civilianId,hla.Identity.Id,threaten,pay);

    public CisfSpawnPoint? ResolveCisfSpawn(PlayerLife player)
    {
        if(player.Faction!=Faction.Cisf)return null;
        if(player.PreferredCisfSpawn==CisfSpawnPoint.Fob&&FobCommsOnline)return CisfSpawnPoint.Fob;
        if(player.PreferredCisfSpawn==CisfSpawnPoint.VillagePolice&&VillagePoliceOnline)return CisfSpawnPoint.VillagePolice;
        if(player.PreferredCisfSpawn==CisfSpawnPoint.BazaarPolice&&BazaarPoliceOnline)return CisfSpawnPoint.BazaarPolice;
        if(FobCommsOnline)return CisfSpawnPoint.Fob;
        if(VillagePoliceOnline)return CisfSpawnPoint.VillagePolice;
        if(BazaarPoliceOnline)return CisfSpawnPoint.BazaarPolice;
        return null;
    }

    public void CycleCisfSpawn(PlayerLife player)
    {
        if(player.Faction!=Faction.Cisf)return;
        var choices=new[]{CisfSpawnPoint.Fob,CisfSpawnPoint.VillagePolice,CisfSpawnPoint.BazaarPolice}
            .Where(s=>s switch
            {
                CisfSpawnPoint.Fob=>FobCommsOnline,
                CisfSpawnPoint.VillagePolice=>VillagePoliceOnline,
                CisfSpawnPoint.BazaarPolice=>BazaarPoliceOnline,
                _=>false
            }).ToArray();
        if(choices.Length==0)return;
        var index=Array.IndexOf(choices,player.PreferredCisfSpawn);
        player.PreferredCisfSpawn=choices[(index+1+choices.Length)%choices.Length];
    }

    public void SabotageFobComms()
    {
        if(!FobCommsOnline)return;
        FobCommsOnline=false;
        Objective=InsurrectionActive?"SURVIVE THE INSURRECTION":"FOB communications destroyed / operate from police stations";
    }

    public void SetPoliceStation(District district,bool cisfControlled)
    {
        if(district==District.Village)VillagePoliceOnline=cisfControlled;
        else if(district==District.Bazaar)BazaarPoliceOnline=cisfControlled;
    }

    public bool ResolveSchedule(Faction winner)
    {
        var schedule=Schedules.Active;
        if(schedule is null||schedule.Complete||schedule.Failed||winner==Faction.Civilian)return false;
        Schedules.Resolve(winner);
        if(winner==Faction.Cisf)
        {
            if(schedule.Kind==ScheduleKind.ReinforcementTruck)CisfTickets+=3;
            foreach(var c in Civilians.Civilians.Where(c=>c.District==schedule.District))
            {
                c.CisfAlignment=Math.Clamp(c.CisfAlignment+.035f,0,1);
                c.Anger=Math.Clamp(c.Anger-.02f,0,1);
            }
        }
        else
        {
            foreach(var c in Civilians.Civilians.Where(c=>c.District==schedule.District))
            {
                c.HlaAlignment=Math.Clamp(c.HlaAlignment+.045f,0,1);
                c.Anger=Math.Clamp(c.Anger+.03f,0,1);
            }
        }
        return true;
    }

    public void CallToArms(PlayerLife hla,Vec3 position)
    {
        if(hla.Faction!=Faction.Hla||hla.State!=LifeState.Active)return;
        var district=position.X<-20?District.Village:position.X>20?District.Cbd:District.Bazaar;
        Civilians.CallToArms(district,hla.Identity.Id,position.X,position.Z);
    }

    public void OrderFollow(PlayerLife hla,Vec3 position)
    {
        if(hla.Faction!=Faction.Hla||hla.State!=LifeState.Active)return;
        var district=position.X<-20?District.Village:position.X>20?District.Cbd:District.Bazaar;
        Civilians.OrderFollow(district,hla.Identity.Id);
    }

    public void OrderGoHere(PlayerLife hla,Vec3 position,Vec3 aim)
    {
        if(hla.Faction!=Faction.Hla||hla.State!=LifeState.Active)return;
        var district=position.X<-20?District.Village:position.X>20?District.Cbd:District.Bazaar;
        var target=position+aim.Normalized*10;
        Civilians.OrderGoHere(district,new Vec3Like(target.X,target.Z));
    }

    public void OrderAttack(PlayerLife hla,Vec3 position)
    {
        if(hla.Faction!=Faction.Hla||hla.State!=LifeState.Active)return;
        var district=position.X<-20?District.Village:position.X>20?District.Cbd:District.Bazaar;
        Civilians.OrderAttack(district);
    }

    public void Scramble(PlayerLife hla,Vec3 position)
    {
        if(hla.Faction!=Faction.Hla)return;
        var district=position.X<-20?District.Village:position.X>20?District.Cbd:District.Bazaar;
        Civilians.Scramble(district);
    }

    public EvidenceRecord SearchCivilian(PlayerLife cisf,int civilianId)
    {
        var c=Civilians.ById(civilianId)??throw new InvalidOperationException("Civilian not found.");
        var result=Forensics.InspectIdentity(c.Identity,cisf.Identity.Id);
        cisf.PersonallyKnownIdentities.Add(c.Identity.Id);
        WrongfulSearch(civilianId,cisf.Identity.Id);
        return result;
    }

    public void WrongfulSearch(int civilianId,int cisfIdentity)
    {
        var c=Civilians.ById(civilianId); if(c is null)return;
        c.CisfAlignment=Math.Max(0,c.CisfAlignment-.06f);
        c.Anger=Math.Min(1,c.Anger+.05f);
        c.PersonalOpinion[cisfIdentity]=c.PersonalOpinion.GetValueOrDefault(cisfIdentity)-.1f;
        Civilians.RelationshipShock(civilianId,.025f,.015f,-.01f);
    }

    public void Radio(PlayerLife speaker,string text)
    {
        if(!speaker.HasRadio||!speaker.RadioOn)return;
        RadioLog.Add(new((Settings.MatchMinutes*60)-MatchRemaining,speaker.Identity.Id,text[..Math.Min(180,text.Length)]));
    }

    private void CheckVictory()
    {
        var activeHla=Players.Any(p=>p.Faction==Faction.Hla&&p.State is LifeState.Active or LifeState.Downed or LifeState.Detained);
        if(Civilians.AvailableHlaTickets==0&&!activeHla)Winner="CISF";
        else if(CisfTickets==0&&!Players.Any(p=>p.Faction==Faction.Cisf&&p.State is LifeState.Active or LifeState.Downed or LifeState.Detained))Winner="HLA";
        else if(MatchRemaining<=0&&!InsurrectionActive)Winner="CISF";
    }
}

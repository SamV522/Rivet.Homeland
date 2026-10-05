using Rivet.Homeland.Domain;
namespace Rivet.Homeland.Application;
internal sealed class ScheduleDirector
{
    private readonly Random _rng=new(9211); private int _next=1; private float _cooldown=20;
    public ScheduleState? Active { get; private set; }
    public int CisfWins { get; private set; } public int HlaWins { get; private set; }
    public void Update(float dt)
    {
        if(Active is { Complete:false,Failed:false } a){ var next=a.RemainingSeconds-dt; Active=a with{RemainingSeconds=next}; if(next<=0) Resolve(a.AssignedFaction==Faction.Cisf?Faction.Hla:Faction.Cisf); return; }
        _cooldown-=dt; if(_cooldown<=0 && CisfWins+HlaWins<HomelandRules.ScheduleCount) StartRandom();
    }
    void StartRandom(){var kinds=Enum.GetValues<ScheduleKind>(); var kind=kinds[_rng.Next(kinds.Length)]; var faction=_rng.NextDouble()<.6?Faction.Cisf:Faction.Hla; Active=new(_next++,kind,(District)_rng.Next(3),_rng.Next(210,360),faction,Reward:RewardFor(kind,faction)); _cooldown=9999;}
    public void Resolve(Faction winner){ if(Active is null)return; if(winner==Faction.Cisf)CisfWins++; else if(winner==Faction.Hla)HlaWins++; Active=Active with{Complete=true,RemainingSeconds=0}; _cooldown=35; }
    static string RewardFor(ScheduleKind k,Faction f)=>k switch{ScheduleKind.ReinforcementTruck when f==Faction.Cisf=>"+3 CISF reinforcement tickets",ScheduleKind.MedicalConvoy=>"Medical stock",ScheduleKind.SupplyConvoy=>"Weapons/ammunition stock",ScheduleKind.RadioBriefing=>"Intelligence opportunity",_=>"District influence and resources"};
}

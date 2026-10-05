using Rivet;
using Rivet.Homeland.Domain;
using Rivet.Homeland.Infrastructure;

namespace Rivet.Homeland.Application;

internal sealed class CivilianAgentSystem : IDisposable
{
    private readonly World _world;
    private readonly HomelandWorld _scene;
    private readonly CivilianDirector _director;
    private readonly Forensics _forensics;
    private readonly Dictionary<int,Runtime> _agents=[];
    private readonly IReadOnlyList<GoapAction> _actions;
    private float _clock;
    private bool _disposed;

    public CivilianAgentSystem(World world,HomelandWorld scene,HomelandSimulation simulation)
    {
        _world=world;_scene=scene;_director=simulation.Civilians;_forensics=simulation.Forensics;
        _actions=
        [
            new("Flee to safety",CivilianFact.InDanger,CivilianFact.AtSafePlace,CivilianFact.AtSafePlace,
                CivilianFact.AtHome|CivilianFact.AtWork|CivilianFact.AtBazaar|CivilianFact.AtReportPoint|CivilianFact.AtWeaponCache|CivilianFact.AtRally,.2f),
            new("Report what I saw",CivilianFact.HasReportableIntel,CivilianFact.InDanger|CivilianFact.IntelReported,CivilianFact.AtReportPoint|CivilianFact.IntelReported,
                CivilianFact.AtHome|CivilianFact.AtWork|CivilianFact.AtBazaar|CivilianFact.AtSafePlace|CivilianFact.AtWeaponCache|CivilianFact.AtRally,.65f),
            new("Retrieve hidden weapon",CivilianFact.Rebel|CivilianFact.CalledToArms,CivilianFact.InDanger|CivilianFact.Armed,CivilianFact.AtWeaponCache|CivilianFact.Armed,
                CivilianFact.AtHome|CivilianFact.AtWork|CivilianFact.AtBazaar|CivilianFact.AtSafePlace|CivilianFact.AtReportPoint|CivilianFact.AtRally,.6f),
            new("Rally with HLA",CivilianFact.Rebel|CivilianFact.CalledToArms|CivilianFact.Armed,CivilianFact.InDanger|CivilianFact.AtRally,CivilianFact.AtRally,
                CivilianFact.AtHome|CivilianFact.AtWork|CivilianFact.AtBazaar|CivilianFact.AtSafePlace|CivilianFact.AtReportPoint|CivilianFact.AtWeaponCache,.8f),
            new("Scramble home",CivilianFact.Rebel|CivilianFact.ScrambleRequested,CivilianFact.InDanger|CivilianFact.AtHome,CivilianFact.AtHome,
                CivilianFact.AtWork|CivilianFact.AtBazaar|CivilianFact.AtSafePlace|CivilianFact.AtReportPoint|CivilianFact.AtWeaponCache|CivilianFact.AtRally,.25f),
            new("Go home",CivilianFact.None,CivilianFact.InDanger|CivilianFact.AtHome,CivilianFact.AtHome,
                CivilianFact.AtWork|CivilianFact.AtBazaar|CivilianFact.AtSafePlace|CivilianFact.AtReportPoint|CivilianFact.AtWeaponCache|CivilianFact.AtRally,1),
            new("Go to work",CivilianFact.None,CivilianFact.InDanger|CivilianFact.AtWork,CivilianFact.AtWork,
                CivilianFact.AtHome|CivilianFact.AtBazaar|CivilianFact.AtSafePlace|CivilianFact.AtReportPoint|CivilianFact.AtWeaponCache|CivilianFact.AtRally,1),
            new("Visit bazaar",CivilianFact.None,CivilianFact.InDanger|CivilianFact.AtBazaar,CivilianFact.AtBazaar,
                CivilianFact.AtHome|CivilianFact.AtWork|CivilianFact.AtSafePlace|CivilianFact.AtReportPoint|CivilianFact.AtWeaponCache|CivilianFact.AtRally,1)
        ];
    }

    public void SpawnAll()
    {
        foreach(var c in _director.Civilians)
        {
            var start=InitialPosition(c);
            var entity=_world.Spawn($"Civilian:{c.Identity.Id}:{c.Identity.First}:{c.Identity.Last}")
                .SetTransform(new Transform(start,Vec3.Zero,One()))
                .SetBounds(new Vec3(.28f,.875f,.28f),new Vec3(0,.875f,0))
                .SetCharacterController(new CharacterControllerDesc(Radius:_scene.HumanNavigationProfile.Radius,Height:_scene.HumanNavigationProfile.Height,MaxStrength:80))
                .SetTint(new Vec3(.72f,.65f,.52f));

            var model=HomelandAssets.Civilian(c.Identity.Appearance.Outfit);
            if(model is not null)entity.SetModel(model);else entity.AddTag("DebugVisible");

            var agent=entity.AddComponent<NavigationAgent>(a=>
            {
                a.Navigation=_scene.Navigation;
                a.Profile=_scene.HumanNavigationProfile;
                a.Filter=_scene.CivilianFilter();
                a.Speed=2.25f;
                a.StoppingDistance=.38f;
            });
            _agents[c.Identity.Id]=new(c,entity,agent);
        }
    }

    public void FixedUpdate(float dt,IReadOnlyDictionary<int,Vec3>? playerPositions=null)
    {
        _clock+=dt;
        foreach(var runtime in _agents.Values)
        {
            var c=runtime.State;
            if(c.Health<=0)
            {
                runtime.Agent.Stop();
                runtime.Entity.SetVisible(false);
                continue;
            }

            if(c.PlayerControlled)
            {
                runtime.Agent.Stop();
                runtime.Entity.SetVisible(false);
                continue;
            }

            runtime.Entity.SetVisible(true);
            c.District=_scene.DistrictAt(runtime.Entity.Transform.Position);
            if(c.FollowIdentityId is { } leader&&playerPositions is not null&&playerPositions.TryGetValue(leader,out var leaderPosition))
            {
                c.RallyX=leaderPosition.X;
                c.RallyZ=leaderPosition.Z;
            }

            if(c.Incarcerated)
            {
                runtime.Agent.Stop();
                runtime.ActiveAction=null;
                runtime.Plan=[];
                continue;
            }

            if(c.AttackOrdered&&c.CalledToArms&&c.Armed)
            {
                runtime.ActiveAction=null;
                runtime.Plan=[];
                continue;
            }

            runtime.ReplanRemaining-=dt;
            if(runtime.ActiveAction is not null&&Arrived(runtime))
            {
                Complete(runtime,runtime.ActiveAction);
                runtime.ActiveAction=null;
                runtime.Plan=[];
                runtime.ReplanRemaining=0;
            }

            if(runtime.ReplanRemaining>0&&runtime.ActiveAction is not null)continue;
            Replan(runtime);
        }
    }

    private void Replan(Runtime r)
    {
        r.ReplanRemaining=.45f+(r.State.Identity.Id%7)*.03f;
        var facts=Facts(r);
        var goal=Goal(r.State);
        var plan=GoapPlanner.Plan(facts,goal,_actions);
        r.Plan=plan;
        if(plan.Count==0)
        {
            r.ActiveAction=null;
            r.Agent.Stop();
            r.State.CurrentActivity=DescribeIdle(r.State,facts);
            return;
        }

        var next=plan[0];
        if(r.ActiveAction?.Name==next.Name&&r.Agent.Destination is not null)return;
        r.ActiveAction=next;
        var destination=Destination(next.Name,r.State);
        r.State.CurrentActivity=next.Name;
        if(!r.Agent.SetDestination(destination))
        {
            var reachable=_scene.Navigation.RandomReachablePoint(r.Entity.Transform.Position,6,(uint)r.State.Identity.Id,_scene.CivilianFilter());
            if(reachable is { } p)r.Agent.SetDestination(p);
        }
    }

    private CivilianFact Facts(Runtime r)
    {
        var c=r.State;var p=r.Entity.Transform.Position;var f=CivilianFact.None;
        if(Near(p,_scene.HomePosition(c)))f|=CivilianFact.AtHome;
        if(Near(p,_scene.WorkPosition(c)))f|=CivilianFact.AtWork;
        if(Near(p,_scene.BazaarPosition(c)))f|=CivilianFact.AtBazaar;
        if(Near(p,_scene.SafePosition(c)))f|=CivilianFact.AtSafePlace;
        if(Near(p,_scene.ReportPosition(c)))f|=CivilianFact.AtReportPoint;
        if(Near(p,_scene.WeaponCachePosition(c)))f|=CivilianFact.AtWeaponCache;
        if(Near(p,_scene.RallyPosition(c)))f|=CivilianFact.AtRally;
        if(c.Danger>.22f)f|=CivilianFact.InDanger;
        if(c.Memories.Any(m=>!m.Reported))f|=CivilianFact.HasReportableIntel;
        else f|=CivilianFact.IntelReported;
        if(c.Rebel)f|=CivilianFact.Rebel;
        if(c.CalledToArms)f|=CivilianFact.CalledToArms;
        if(c.Armed)f|=CivilianFact.Armed;
        if(c.ScrambleRequested)f|=CivilianFact.ScrambleRequested;
        return f;
    }

    private GoapGoal Goal(CivilianState c)
    {
        if(c.Danger>.22f)return new(CivilianFact.AtSafePlace,CivilianFact.None);

        var unreported=c.Memories.Any(m=>!m.Reported);
        var willInform=c.CisfAlignment+.15f*c.Fear>c.HlaAlignment;
        if(unreported&&willInform)return new(CivilianFact.IntelReported,CivilianFact.None);

        if(c.Rebel&&c.CalledToArms&&!c.ScrambleRequested)
            return new(CivilianFact.Armed|CivilianFact.AtRally,CivilianFact.None);

        if(c.Rebel&&c.ScrambleRequested)
            return new(CivilianFact.AtHome,CivilianFact.None);

        var phase=(int)((_clock/32f+c.Identity.Id*.37f)%4);
        return phase switch
        {
            0=>new(CivilianFact.AtHome,CivilianFact.None),
            1 when c.Occupation!=CivilianOccupation.Unemployed=>new(CivilianFact.AtWork,CivilianFact.None),
            2=>new(CivilianFact.AtBazaar,CivilianFact.None),
            _=>new(CivilianFact.AtHome,CivilianFact.None)
        };
    }

    private Vec3 Destination(string action,CivilianState c)=>action switch
    {
        "Flee to safety"=>_scene.SafePosition(c),
        "Report what I saw"=>_scene.ReportPosition(c),
        "Retrieve hidden weapon"=>_scene.WeaponCachePosition(c),
        "Rally with HLA"=>_scene.RallyPosition(c),
        "Scramble home"=>_scene.HomePosition(c),
        "Go to work"=>_scene.WorkPosition(c),
        "Visit bazaar"=>_scene.BazaarPosition(c),
        _=>_scene.HomePosition(c)
    };

    private void Complete(Runtime r,GoapAction action)
    {
        var c=r.State;
        switch(action.Name)
        {
            case "Report what I saw":
                foreach(var memory in c.Memories.Where(m=>!m.Reported).ToArray())
                {
                    _forensics.Log(_forensics.WitnessStatement(c.Identity.Id,memory));
                    memory.Reported=true;
                }
                c.CurrentActivity="Reported what they witnessed to CISF";
                break;
            case "Retrieve hidden weapon":
                c.Armed=true;
                c.CurrentActivity="Armed Rebel";
                break;
            case "Rally with HLA":
                c.CurrentActivity="Armed Rebel at rally";
                break;
            case "Scramble home":
                c.CalledToArms=false;
                c.ScrambleRequested=false;
                c.CurrentActivity="Back in civilian routine";
                break;
            default:
                c.CurrentActivity=action.Name switch
                {
                    "Go home"=>"At home",
                    "Go to work"=>$"Working as {c.Occupation}",
                    "Visit bazaar"=>"At the bazaar",
                    "Flee to safety"=>"Hiding from danger",
                    _=>action.Name
                };
                break;
        }
    }

    public void UpdateRebelCombat(float dt,IReadOnlyList<PlayerCombatTarget> targets,HomelandSimulation simulation)
    {
        foreach(var runtime in _agents.Values)
        {
            var c=runtime.State;
            runtime.CombatCooldown=Math.Max(0,runtime.CombatCooldown-dt);
            runtime.CombatRepath=Math.Max(0,runtime.CombatRepath-dt);
            if(c.Health<=0||c.PlayerControlled||c.Incarcerated||!c.Rebel||!c.CalledToArms||!c.Armed||!c.AttackOrdered)continue;

            var target=targets
                .Where(t=>t.Life.Faction==Faction.Cisf&&t.Life.State==LifeState.Active)
                .OrderBy(t=>Horizontal(t.Position-runtime.Entity.Transform.Position))
                .FirstOrDefault();
            if(target is null)continue;

            var delta=target.Position-runtime.Entity.Transform.Position;
            var distance=Horizontal(delta);
            if(distance>18)continue;

            if(distance>7.5f)
            {
                if(runtime.CombatRepath<=0)
                {
                    runtime.Agent.SetDestination(_scene.Project(target.Position));
                    runtime.CombatRepath=.55f+(c.Identity.Id%5)*.08f;
                }
                c.CurrentActivity="Advancing on CISF";
                continue;
            }

            runtime.Agent.Stop();
            c.CurrentActivity="Engaging CISF";
            if(runtime.CombatCooldown>0||!ClearShot(runtime.Entity,target.Entity))continue;

            // Recruited civilians are intentionally shaky and individually weak.
            var damage=8f+(c.Identity.Id%5);
            simulation.DamagePlayer(target.Life,damage);
            runtime.CombatCooldown=.85f+(c.Identity.Id%7)*.11f;
        }
    }

    private bool ClearShot(Entity shooter,Entity target)
    {
        var from=shooter.Transform.Position+new Vec3(0,1f,0);
        var to=target.Transform.Position+new Vec3(0,1f,0);
        var delta=to-from;
        if(delta.Length<.05f)return true;
        var direction=delta.Normalized;
        var hit=_world.Raycast(from+direction*.45f,direction,Math.Max(0,delta.Length-.45f));
        return hit is null||hit.Value.Entity==target;
    }

    public Vec3 Position(int identityId)
    {
        if(!_agents.TryGetValue(identityId,out var r))return Vec3.Zero;
        return r.State.PlayerControlled&&r.ControlledPosition is { } controlled
            ? controlled
            : r.Entity.Transform.Position;
    }

    public Entity? EntityFor(int identityId)=>
        _agents.TryGetValue(identityId,out var r)&&!r.State.PlayerControlled?r.Entity:null;

    public void SetPlayerControlled(int identityId,bool controlled)
    {
        if(!_agents.TryGetValue(identityId,out var r))return;
        if(controlled)
        {
            r.ControlledPosition=r.Entity.Transform.Position;
            r.State.PlayerControlled=true;
            r.Agent.Stop();
            r.Entity.SetTransform(r.Entity.Transform with{Position=new Vec3(r.Entity.Transform.Position.X,-20,r.Entity.Transform.Position.Z)});
            r.Entity.SetVisible(false);
            return;
        }

        r.State.PlayerControlled=false;
        if(r.ControlledPosition is { } returnPosition)
            r.Entity.SetTransform(r.Entity.Transform with{Position=_scene.Project(returnPosition)});
        r.ControlledPosition=null;
        r.Entity.SetVisible(true);
        r.ReplanRemaining=0;
    }

    public void SetControlledPosition(int identityId,Vec3 position)
    {
        if(!_agents.TryGetValue(identityId,out var r)||!r.State.PlayerControlled)return;
        r.ControlledPosition=position;
    }

    public void KillIdentity(int identityId)
    {
        if(!_agents.TryGetValue(identityId,out var r))return;
        r.State.Health=0;
        r.State.PlayerControlled=false;
        r.Agent.Stop();
        r.Entity.SetVisible(false);
    }

    public CivilianRuntimeSummary[] Capture()=>_agents.Values
        .Where(r=>r.State.Health>0&&!r.State.PlayerControlled)
        .Select(r=>new CivilianRuntimeSummary(
            r.State.Identity.Id,
            r.Entity.Transform.Position,
            r.State.District,
            r.State.CurrentActivity,
            r.State.Rebel,
            r.State.CalledToArms,
            r.State.Armed,
            r.State.Identity.Appearance.Outfit))
        .ToArray();

    private Vec3 InitialPosition(CivilianState c)
    {
        var basePoint=(c.Identity.Id%3) switch
        {
            0=>_scene.HomePosition(c),
            1=>_scene.WorkPosition(c),
            _=>_scene.BazaarPosition(c)
        };
        return _scene.Navigation.RandomReachablePoint(basePoint,2,(uint)c.Identity.Id,_scene.CivilianFilter())??basePoint;
    }

    private static bool Near(Vec3 a,Vec3 b)=>Horizontal(a-b)<.85f;
    private static float Horizontal(Vec3 v)=>MathF.Sqrt(v.X*v.X+v.Z*v.Z);
    private static string DescribeIdle(CivilianState c,CivilianFact facts)=>
        c.Incarcerated?"Detained":facts.HasFlag(CivilianFact.AtHome)?"At home":facts.HasFlag(CivilianFact.AtWork)?$"Working as {c.Occupation}":facts.HasFlag(CivilianFact.AtBazaar)?"At the bazaar":"Waiting";

    private static Vec3 One()=>new(1,1,1);

    public void Dispose()
    {
        if(_disposed)return;_disposed=true;
        foreach(var r in _agents.Values){r.Agent.ResetPath();if(r.Entity.IsAlive)r.Entity.Destroy();}
        _agents.Clear();
    }

    private sealed class Runtime(CivilianState state,Entity entity,NavigationAgent agent)
    {
        public CivilianState State { get; }=state;
        public Entity Entity { get; }=entity;
        public NavigationAgent Agent { get; }=agent;
        public float ReplanRemaining { get; set; }
        public GoapAction? ActiveAction { get; set; }
        public IReadOnlyList<GoapAction> Plan { get; set; }=[];
        public float CombatCooldown { get; set; }
        public float CombatRepath { get; set; }
        public Vec3? ControlledPosition { get; set; }
    }

    private static bool Arrived(Runtime r)=>r.Agent.Destination is not null&&r.Agent.RemainingDistance<=r.Agent.StoppingDistance+.2f;
}

internal sealed record CivilianRuntimeSummary(int Id,Vec3 Position,District District,string Activity,bool Rebel,bool CalledToArms,bool Armed,string Outfit);

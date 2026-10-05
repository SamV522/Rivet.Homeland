using Rivet;
using Rivet.Homeland.Domain;
using Rivet.Homeland.Infrastructure;

namespace Rivet.Homeland.Application;

internal sealed class PlayerActorSystem : IDisposable
{
    private readonly World _world;
    private readonly HomelandWorld _scene;
    private readonly HomelandSimulation _simulation;
    private readonly CivilianAgentSystem _civilians;
    private readonly Dictionary<int,Runtime> _actors=[];
    private bool _disposed;

    public PlayerActorSystem(World world,HomelandWorld scene,HomelandSimulation simulation,CivilianAgentSystem civilians)
    {
        _world=world;_scene=scene;_simulation=simulation;_civilians=civilians;
    }

    public void SpawnAll()
    {
        foreach(var player in _simulation.Players)
            Spawn(player);
    }

    private void Spawn(PlayerLife player)
    {
        var start=SpawnPosition(player);
        var entity=_world.Spawn($"Player:{player.Slot}:{player.Faction}:{player.Identity.Id}")
            .SetTransform(new Transform(start,Vec3.Zero,One()))
            .SetBounds(new Vec3(.30f,.875f,.30f),new Vec3(0,.875f,0))
            .SetCharacterController(new CharacterControllerDesc(Radius:.30f,Height:1.75f,MaxStrength:110))
            .SetTint(player.Faction==Faction.Cisf?new Vec3(.28f,.40f,.34f):new Vec3(.66f,.57f,.44f));

        var model=player.Faction==Faction.Cisf?HomelandAssets.Cisf():HomelandAssets.Civilian(player.PresentedAppearance.Outfit);
        if(model is not null)entity.SetModel(model);else entity.AddTag("DebugVisible");

        _actors[player.Slot]=new(player,entity,player.Identity.Id);
        if(player.Faction==Faction.Hla)_civilians.SetPlayerControlled(player.Identity.Id,true);
    }

    public void FixedUpdate(float dt,HomelandInput hostInput,IReadOnlyDictionary<uint,HomelandInput> peerInputs)
    {
        foreach(var runtime in _actors.Values)
        {
            var player=runtime.Player;
            HandleIdentityTransition(runtime);

            var input=player.PeerId is { } peer
                ? peerInputs.GetValueOrDefault(peer,new HomelandInput(0,0,0,1,HomelandButtons.None))
                : hostInput;

            runtime.FireCooldown=Math.Max(0,runtime.FireCooldown-dt);
            var controller=runtime.Entity.GetComponent<CharacterController>();
            if(controller is null)continue;

            if(player.State!=LifeState.Active)
            {
                controller.Move(Vec3.Zero);
                runtime.Previous=input.Buttons;
                continue;
            }

            var movement=new Vec3(input.MoveX,0,input.MoveZ);
            if(movement.LengthSquared>1)movement=movement.Normalized;
            controller.Move(movement*HomelandRules.WalkSpeed);
            if(player.Faction==Faction.Hla)_civilians.SetControlledPosition(player.Identity.Id,runtime.Entity.Transform.Position);

            var aim=new Vec3(input.AimX,0,input.AimZ);
            if(aim.LengthSquared>.05f)runtime.Aim=aim.Normalized;
            if(runtime.Aim.LengthSquared<.05f)runtime.Aim=new Vec3(0,0,1);

            var pressed=input.Buttons&~runtime.Previous;
            runtime.Previous=input.Buttons;

            if((pressed&HomelandButtons.Interact)!=0)Interact(runtime);
            if((pressed&HomelandButtons.Recruit)!=0)Recruit(runtime);
            if((pressed&HomelandButtons.CallToArms)!=0)_simulation.CallToArms(player,runtime.Entity.Transform.Position);
            if((pressed&HomelandButtons.Scramble)!=0)_simulation.Scramble(player,runtime.Entity.Transform.Position);
            if((pressed&HomelandButtons.Cuff)!=0)Cuff(runtime);
            if((pressed&HomelandButtons.Release)!=0)Release(runtime);
            if((pressed&HomelandButtons.AbandonLife)!=0)_simulation.AbandonDetainedLife(player);
            if((pressed&HomelandButtons.CycleSpawn)!=0)_simulation.CycleCisfSpawn(player);
            if((input.Buttons&HomelandButtons.Fire)!=0&&runtime.FireCooldown<=0)Fire(runtime);
        }
    }

    private void HandleIdentityTransition(Runtime runtime)
    {
        var player=runtime.Player;
        if(runtime.IdentityId==player.Identity.Id)return;

        var old=runtime.IdentityId;
        runtime.IdentityId=player.Identity.Id;
        runtime.Entity.Name=$"Player:{player.Slot}:{player.Faction}:{player.Identity.Id}";
        runtime.Entity.SetTransform(runtime.Entity.Transform with{Position=SpawnPosition(player)});
        runtime.Entity.SetTint(player.Faction==Faction.Cisf?new Vec3(.28f,.40f,.34f):new Vec3(.66f,.57f,.44f));
        runtime.Aim=new Vec3(0,0,1);

        if(player.Faction==Faction.Hla)
        {
            _civilians.SetPlayerControlled(old,false);
            _civilians.SetPlayerControlled(player.Identity.Id,true);
        }
    }

    private Vec3 SpawnPosition(PlayerLife player)
    {
        if(player.Faction==Faction.Cisf)
        {
            var spawn=_simulation.ResolveCisfSpawn(player)??player.PreferredCisfSpawn;
            return _scene.CisfSpawn(spawn);
        }
        var position=_civilians.Position(player.Identity.Id);
        return position.LengthSquared>.01f?position:_scene.BazaarPosition(_simulation.Civilians.ById(player.Identity.Id)!);
    }

    private void Interact(Runtime actor)
    {
        var player=actor.Player;
        var position=actor.Entity.Transform.Position;

        if(_simulation.Schedules.Active is {Complete:false,Failed:false} schedule
           &&Flat(_scene.SchedulePosition(schedule)-position).Length<3.5f)
        {
            _simulation.ResolveSchedule(player.Faction);
            return;
        }

        if(player.Faction==Faction.Hla&&_simulation.FobCommsOnline
           &&Flat(_scene.FobCommunications-position).Length<3.5f)
        {
            _simulation.SabotageFobComms();
            return;
        }

        if(Flat(_scene.VillagePoliceSpawn-position).Length<4)
        {
            _simulation.SetPoliceStation(District.Village,player.Faction==Faction.Cisf);
            return;
        }

        if(Flat(_scene.BazaarPoliceSpawn-position).Length<4)
        {
            _simulation.SetPoliceStation(District.Bazaar,player.Faction==Faction.Cisf);
            return;
        }

        var nearbyDowned=NearestPlayer(actor,2.2f,p=>p.State==LifeState.Downed);
        if(nearbyDowned is not null)
        {
            _simulation.Stabilize(nearbyDowned.Player);
            return;
        }

        var civilian=NearestCivilian(actor,HomelandRules.InteractionRange);
        if(civilian is null)return;
        if(player.Faction==Faction.Cisf)
        {
            var evidence=_simulation.SearchCivilian(player,civilian.State.Identity.Id);
            _simulation.Forensics.Log(evidence);
            civilian.State.CurrentActivity="Being searched by CISF";
        }
    }

    private void Recruit(Runtime actor)
    {
        if(actor.Player.Faction!=Faction.Hla)return;
        var civilian=NearestCivilian(actor,HomelandRules.InteractionRange);
        if(civilian is null)return;
        var success=_simulation.Recruit(actor.Player,civilian.State.Identity.Id);
        civilian.State.CurrentActivity=success?"Quietly recruited into HLA":"Refused HLA recruitment";
    }

    private void Cuff(Runtime actor)
    {
        var target=NearestPlayer(actor,1.8f,p=>(p.State is LifeState.Active or LifeState.Downed)&&p.Faction!=actor.Player.Faction);
        if(target is not null)
        {
            _simulation.Detain(target.Player);
            return;
        }

        if(actor.Player.Faction!=Faction.Cisf)return;
        var civilian=NearestCivilian(actor,1.8f);
        if(civilian is not null)_simulation.DetainCivilian(civilian.State.Identity.Id);
    }

    private void Release(Runtime actor)
    {
        var target=NearestPlayer(actor,2.0f,p=>p.State==LifeState.Detained);
        if(target is not null)
        {
            _simulation.Release(target.Player);
            return;
        }

        var civilian=NearestCivilian(actor,2.0f,c=>c.Incarcerated);
        if(civilian is not null)_simulation.ReleaseCivilian(civilian.State.Identity.Id);
    }

    private void Fire(Runtime shooter)
    {
        shooter.FireCooldown=shooter.Player.Faction==Faction.Cisf ? .14f : .28f;
        var origin=shooter.Entity.Transform.Position;
        var aim=shooter.Aim;

        Runtime? bestPlayer=null;
        CivilianCandidate? bestCivilian=null;
        var bestDistance=float.MaxValue;

        foreach(var target in _actors.Values)
        {
            if(target==shooter||target.Player.State==LifeState.Dead)continue;
            var delta=Flat(target.Entity.Transform.Position-origin);
            var distance=delta.Length;if(distance<.1f||distance>35)continue;
            if(Dot(delta.Normalized,aim)<.965f)continue;
            if(!ClearShot(origin,target.Entity.Transform.Position,target.Entity))continue;
            if(distance<bestDistance){bestDistance=distance;bestPlayer=target;bestCivilian=null;}
        }

        foreach(var c in _simulation.Civilians.Civilians.Where(c=>c.Health>0&&!c.PlayerControlled&&!c.Incarcerated))
        {
            var p=_civilians.Position(c.Identity.Id);
            var delta=Flat(p-origin);
            var distance=delta.Length;if(distance<.1f||distance>35)continue;
            if(Dot(delta.Normalized,aim)<.965f)continue;
            if(!ClearShot(origin,p,null))continue;
            if(distance<bestDistance){bestDistance=distance;bestCivilian=new(c,p);bestPlayer=null;}
        }

        if(bestPlayer is not null)_simulation.DamagePlayer(bestPlayer.Player,shooter.Player.Faction==Faction.Cisf ? 42 : 38);
        else if(bestCivilian is not null)
        {
            _simulation.DamageCivilian(bestCivilian.State.Identity.Id,shooter.Player.Faction==Faction.Cisf ? 42 : 38,shooter.Player.Faction);
            if(bestCivilian.State.Health<=0)_civilians.KillIdentity(bestCivilian.State.Identity.Id);
        }

        var district=_scene.DistrictAt(origin);
        _simulation.Civilians.RaiseDanger(district,.72f);
        foreach(var witness in _simulation.Civilians.Civilians.Where(c=>c.Health>0&&!c.PlayerControlled&&!c.Incarcerated))
        {
            var wp=_civilians.Position(witness.Identity.Id);
            if(Flat(wp-origin).Length>16)continue;
            _simulation.Civilians.AddWitness(witness.Identity.Id,shooter.Player.Identity.Id,
                $"Saw {shooter.Player.Identity.First} {shooter.Player.Identity.Last} fire a weapon",true,1);
        }
    }

    private bool ClearShot(Vec3 origin,Vec3 target,Entity? targetEntity)
    {
        var from=origin+new Vec3(0,1.0f,0);
        var to=target+new Vec3(0,1.0f,0);
        var delta=to-from;
        var distance=delta.Length;
        if(distance<.01f)return true;
        var hit=_world.Raycast(from,delta.Normalized,distance);
        return hit is null||targetEntity is not null&&hit.Value.Entity==targetEntity;
    }

    private Runtime? NearestPlayer(Runtime source,float range,Func<PlayerLife,bool> predicate)
    {
        Runtime? best=null;var distance=range;
        foreach(var r in _actors.Values)
        {
            if(r==source||!predicate(r.Player))continue;
            var d=Flat(r.Entity.Transform.Position-source.Entity.Transform.Position).Length;
            if(d<distance){distance=d;best=r;}
        }
        return best;
    }

    private CivilianCandidate? NearestCivilian(Runtime source,float range,Func<CivilianState,bool>? predicate=null)
    {
        CivilianCandidate? best=null;var distance=range;
        foreach(var c in _simulation.Civilians.Civilians)
        {
            if(c.Health<=0||c.PlayerControlled||(predicate is not null&&!predicate(c)))continue;
            var p=_civilians.Position(c.Identity.Id);
            var d=Flat(p-source.Entity.Transform.Position).Length;
            if(d<distance){distance=d;best=new(c,p);}
        }
        return best;
    }

    public Vec3 Position(int slot)=>_actors.TryGetValue(slot,out var r)?r.Entity.Transform.Position:Vec3.Zero;
    public Vec3 Aim(int slot)=>_actors.TryGetValue(slot,out var r)?r.Aim:new Vec3(0,0,1);

    public PlayerRuntimeSummary[] Capture()=>_actors.Values.Select(r=>
    {
        var p=r.Entity.Transform.Position;
        return new PlayerRuntimeSummary(r.Player.Slot,r.Player.PeerId,r.Player.Identity.Id,p,r.Aim);
    }).ToArray();

    public void Dispose()
    {
        if(_disposed)return;_disposed=true;
        foreach(var r in _actors.Values)if(r.Entity.IsAlive)r.Entity.Destroy();
        _actors.Clear();
    }

    private static Vec3 Flat(Vec3 v)=>new(v.X,0,v.Z);
    private static float Dot(Vec3 a,Vec3 b)=>a.X*b.X+a.Z*b.Z;
    private static Vec3 One()=>new(1,1,1);

    private sealed class Runtime(PlayerLife player,Entity entity,int identityId)
    {
        public PlayerLife Player { get; }=player;
        public Entity Entity { get; }=entity;
        public int IdentityId { get; set; }=identityId;
        public HomelandButtons Previous { get; set; }
        public Vec3 Aim { get; set; }=new(0,0,1);
        public float FireCooldown { get; set; }
    }

    private sealed record CivilianCandidate(CivilianState State,Vec3 Position);
}

internal sealed record PlayerRuntimeSummary(int Slot,uint? PeerId,int IdentityId,Vec3 Position,Vec3 Aim);

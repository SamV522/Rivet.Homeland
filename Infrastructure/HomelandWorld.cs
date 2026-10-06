using Rivet;
using Rivet.Homeland.Domain;

namespace Rivet.Homeland.Infrastructure;

internal sealed class HomelandWorld : IDisposable
{
    private readonly World _world;
    private readonly List<Entity> _entities = [];
    private readonly Dictionary<int, Entity> _remoteCivilians = [];
    private readonly Dictionary<int, Entity> _remotePlayers = [];
    private readonly Dictionary<int, Entity> _remoteCorpses = [];
    private readonly List<BuildingSpec> _buildings = [];
    private readonly List<(Vec3 Center, Vec3 Half)> _roads = [];
    public IReadOnlyList<(Vec3 Center, Vec3 Half)> Roads => _roads;
    public IEnumerable<(Vec3 Center, Vec3 Half)> BuildingFootprints =>
        _buildings.Select(b => (b.Center, b.Half));
    private Entity? _scheduleMarker;
    private int _scheduleMarkerId;
    private bool _built;

    public NavigationProfile HumanNavigationProfile { get; } = new()
    {
        Name = "Human",
        Radius = .28f,
        Height = 1.75f,
        MaxSlopeDegrees = 48,
        MaxClimb = .35f,
        CellSize = .2f,
        CellHeight = .1f,
        TileSize = 64
    };

    public NavigationSpace Navigation { get; private set; } = null!;
    public Vec3 FobSpawn => new(58, .05f, -17);
    public Vec3 FobCommunications => new(70, .05f, -27);
    public Vec3 VillagePoliceSpawn => new(-58, .05f, 18);
    public Vec3 BazaarPoliceSpawn => new(14, .05f, 19);

    public HomelandWorld(World world) => _world = world;

    public void Build()
    {
        if (_built) return;
        _built = true;

        _entities.Add(_world.Spawn("Homeland ambient").SetAmbientLight(new Vec3(.92f, .84f, .72f), .55f));
        var sun = _world.Spawn("Homeland sun")
            .SetTransform(new Transform(Vec3.Zero, new Vec3(-52, -32, 0), One()))
            .SetDirectionalLight(new Vec3(1, .91f, .74f), 1.05f);
        sun.GetComponent<DirectionalLight>()!.CastShadows = true;
        _entities.Add(sun);

        AddGround("World ground", new Vec3(0, -.25f, 0), new Vec3(78, .25f, 34), new Vec3(.52f, .46f, .35f));
        AddRoad(new Vec3(0, .015f, 0), new Vec3(78, .015f, 3));
        AddRoad(new Vec3(0, .016f, 18), new Vec3(78, .015f, 2.2f));
        AddRoad(new Vec3(-20, .017f, 9), new Vec3(2.3f, .015f, 25));
        AddRoad(new Vec3(20, .018f, -8), new Vec3(2.3f, .015f, 26));

        BuildDistricts();
        Navigation = new NavigationSpace(NavigationGeometry(), HumanNavigationProfile, NavigationModifiers());
    }

    private void BuildDistricts()
    {
        for (var i = 0; i < 12; i++)
        {
            var name=$"Village house {i}";
            AddBuilding(name,new Vec3(-69+(i%4)*11,0,new[] {-24f,-10f,10f}[i/4]),
                HomelandAssets.House(name),new Vec3(.67f,.58f,.44f));
        }

        for (var i = 0; i < 18; i++)
        {
            var name=$"Bazaar shop {i}";
            AddBuilding(name,new Vec3(-15+(i%6)*6,0,new[] {-20f,-9f,10f}[i/6]),
                HomelandAssets.BazaarBuilding(name),new Vec3(.73f,.59f,.39f));
        }

        for (var i = 0; i < 10; i++)
        {
            var name=$"CBD block {i}";
            AddBuilding(name,new Vec3(27+(i%5)*9,0,-18+(i/5)*28),
                HomelandAssets.CbdBuilding(name),new Vec3(.54f,.55f,.53f));
        }

        AddBuilding("CISF FOB",new Vec3(62,0,-28),HomelandAssets.FobBuilding(),new Vec3(.28f,.31f,.30f));
        AddBuilding("Police Station Bazaar",new Vec3(14,0,26),HomelandAssets.PoliceStation("Bazaar"),new Vec3(.33f,.38f,.42f));
        AddBuilding("Police Station Village",new Vec3(-59,0,26),HomelandAssets.PoliceStation("Village"),new Vec3(.36f,.40f,.43f));

        var tower=AddSolid("FOB communications tower",new Vec3(70,6,-27),new Vec3(.65f,6,.65f),new Vec3(.42f,.45f,.46f));
        tower.SetInteraction("Sabotage communications tower","homeland.sabotage");

        SpawnVehicle("CISF reinforcement truck",new Vec3(54,0,-18),HomelandAssets.Truck());
        SpawnVehicle("Civilian car",new Vec3(-4,0,8),HomelandAssets.Car());
        SpawnVehicle("Civilian car 2",new Vec3(25,0,7),HomelandAssets.Car());
        SpawnVehicle("Police vehicle",new Vec3(18,0,24),HomelandAssets.PoliceVehicle());
    }

    private void AddBuilding(string name,Vec3 groundPosition,ModelPlacement placement,Vec3 tint)
    {
        var half=placement.ColliderHalfExtents;
        var center=new Vec3(groundPosition.X,half.Y,groundPosition.Z);
        // Keep the complete model/collider footprint clear of every street.
        foreach(var road in _roads)
            if(MathF.Abs(center.X-road.Center.X)<half.X+road.Half.X+.4f &&
               MathF.Abs(center.Z-road.Center.Z)<half.Z+road.Half.Z+.4f)
                throw new InvalidOperationException($"{name} overlaps a road or its setback.");
        _buildings.Add(new BuildingSpec(name,center,half,tint,"building"));

        var root=_world.Spawn(name)
            .SetTransform(new Transform(center,Vec3.Zero,One()))
            .SetBounds(half,Vec3.Zero)
            .SetTint(tint)
            .SetBody(new BodyDesc(BodyMotion.Static,half,1,false));
        _entities.Add(root);

        if(placement.Path is not null)
        {
            var visual=root.CreateChild(name+" visual")
                .SetLocalTransform(new Transform(new Vec3(0,-half.Y,0),Vec3.Zero,placement.VisualScale))
                .SetModel(placement.Path)
                .SetTint(tint);
            _entities.Add(visual);
        }
        else
        {
            root.AddTag("DebugVisible");
        }

        root.SetInteraction(name,"homeland.location");
    }

    private Entity AddGround(string name,Vec3 p,Vec3 half,Vec3 tint)
    {
        var e=_world.Spawn(name).SetTransform(new Transform(p,Vec3.Zero,One()))
            .SetBounds(half,Vec3.Zero).SetTint(tint).AddTag("DebugVisible")
            .SetBody(new BodyDesc(BodyMotion.Static,half,1,false));
        _entities.Add(e);
        return e;
    }

    private void AddRoad(Vec3 p,Vec3 half)
    {
        _roads.Add((p,half));
        var e=_world.Spawn("Road base").SetTransform(new Transform(p,Vec3.Zero,One()))
            .SetBounds(half,Vec3.Zero).SetTint(new Vec3(.18f,.18f,.17f)).AddTag("DebugVisible");
        _entities.Add(e);

        var model=HomelandAssets.RoadStraight();
        if(model is null)return;

        var alongX=half.X>=half.Z;
        var halfLength=alongX?half.X:half.Z;
        const float spacing=4f;
        var count=Math.Max(1,(int)MathF.Ceiling(halfLength*2/spacing));
        var tileLength=halfLength*2/count;
        var roadWidth=(alongX?half.Z:half.X)*2;
        for(var i=0;i<count;i++)
        {
            var offset=-halfLength+tileLength*.5f+i*tileLength;
            var position=p+(alongX?new Vec3(offset,.02f,0):new Vec3(0,.02f,offset));
            var visual=_world.Spawn("Kenney road tile")
                // Kenney's straight road runs along local X.
                .SetTransform(new Transform(position,new Vec3(0,alongX?0:90,0),new Vec3(tileLength,1f,roadWidth)))
                .SetModel(model);
            _entities.Add(visual);
        }
    }

    private Entity AddSolid(string name,Vec3 p,Vec3 half,Vec3 tint)
    {
        var e=_world.Spawn(name).SetTransform(new Transform(p,Vec3.Zero,One()))
            .SetBounds(half,Vec3.Zero).SetTint(tint)
            .SetBody(new BodyDesc(BodyMotion.Static,half,1,false))
            .AddTag("DebugVisible");
        _entities.Add(e);
        return e;
    }

    private void SpawnVehicle(string name,Vec3 groundPosition,ModelPlacement placement)
    {
        var half=placement.ColliderHalfExtents;
        var center=new Vec3(groundPosition.X,half.Y,groundPosition.Z);
        var root=_world.Spawn(name)
            .SetTransform(new Transform(center,Vec3.Zero,One()))
            .SetBounds(half,Vec3.Zero)
            .SetTint(new Vec3(.24f,.28f,.27f))
            .SetBody(new BodyDesc(BodyMotion.Kinematic,half,1200,false));
        _entities.Add(root);

        if(placement.Path is not null)
        {
            var visual=root.CreateChild(name+" visual")
                .SetLocalTransform(new Transform(new Vec3(0,-half.Y,0),Vec3.Zero,placement.VisualScale))
                .SetModel(placement.Path);
            _entities.Add(visual);
        }
        else root.AddTag("DebugVisible");
    }

    private IReadOnlyList<NavigationTriangle> NavigationGeometry()
    {
        const float minX = -78, maxX = 78, minZ = -34, maxZ = 34;
        return
        [
            new(new Vec3(minX, 0, minZ), new Vec3(minX, 0, maxZ), new Vec3(maxX, 0, maxZ)),
            new(new Vec3(minX, 0, minZ), new Vec3(maxX, 0, maxZ), new Vec3(maxX, 0, minZ))
        ];
    }

    private IReadOnlyList<NavigationModifier> NavigationModifiers()
    {
        var result = _roads.Select(road => new NavigationModifier
        {
            Minimum = road.Center-new Vec3(road.Half.X,1,road.Half.Z),
            Maximum = road.Center+new Vec3(road.Half.X,1,road.Half.Z),
            Area = 2
        }).ToList();
        // Exclusions come last so no lower-cost road modifier can reopen a building footprint.
        foreach (var b in _buildings)
            result.Add(new NavigationModifier
            {
                Minimum = b.Center - new Vec3(b.Half.X + .25f, b.Half.Y + 1, b.Half.Z + .25f),
                Maximum = b.Center + new Vec3(b.Half.X + .25f, b.Half.Y + 1, b.Half.Z + .25f),
                Excluded = true
            });
        return result;
    }

    public NavigationFilter CivilianFilter()
    {
        var filter = new NavigationFilter();
        filter.Costs[2] = 1.05f;
        return filter;
    }

    public Vec3 Project(Vec3 point) => Navigation.SamplePosition(point, CivilianFilter()) ?? point;

    public District DistrictAt(Vec3 p) => p.X < -20 ? District.Village : p.X > 20 ? District.Cbd : District.Bazaar;

    public Vec3 HomePosition(CivilianState c)
    {
        var id = c.Identity.Id;
        return c.District switch
        {
            District.Village => Project(new Vec3(-69 + (id % 4) * 11, 0, -18 + ((id / 4) % 3) * 17)),
            District.Bazaar => Project(new Vec3(-15 + (id % 6) * 6, 0, -12 + ((id / 6) % 3) * 16)),
            _ => Project(new Vec3(27 + (id % 5) * 9, 0, -10 + ((id / 5) % 2) * 30))
        };
    }

    public Vec3 WorkPosition(CivilianState c)
    {
        var n = c.Identity.Id;
        var raw = c.Occupation switch
        {
            CivilianOccupation.Farmer => new Vec3(-72 + n % 7 * 4, 0, 12 + n % 4 * 3),
            CivilianOccupation.Shopkeeper or CivilianOccupation.Trader => new Vec3(-16 + n % 6 * 6, 0, -7 + n % 3 * 16),
            CivilianOccupation.Clerk => new Vec3(24 + n % 5 * 9, 0, -7 + n % 2 * 30),
            CivilianOccupation.Mechanic => new Vec3(-7, 0, 10 + n % 3 * 4),
            CivilianOccupation.Driver => new Vec3(8 + n % 5 * 6, 0, 5),
            CivilianOccupation.Labourer => new Vec3(31 + n % 4 * 9, 0, 5),
            _ => BazaarPosition(c)
        };
        return Project(raw);
    }

    public Vec3 BazaarPosition(CivilianState c) => Project(new Vec3(-17 + (c.Identity.Id % 7) * 5.2f, 0, 7 + (c.Identity.Id % 3) * 3.5f));

    public Vec3 SafePosition(CivilianState c)
    {
        var sign = (c.Identity.Id & 1) == 0 ? 1f : -1f;
        return c.District switch
        {
            District.Village => Project(new Vec3(-74 + (c.Identity.Id % 5) * 5, 0, sign * 28)),
            District.Bazaar => Project(new Vec3(sign * 16, 0, 28 - (c.Identity.Id % 4) * 5)),
            _ => Project(new Vec3(70 - (c.Identity.Id % 5) * 6, 0, sign * 27))
        };
    }

    public Vec3 ReportPosition(CivilianState c) => c.District == District.Village ? VillagePoliceSpawn : c.District == District.Bazaar ? BazaarPoliceSpawn : FobSpawn;

    public Vec3 WeaponCachePosition(CivilianState c) => c.District switch
    {
        District.Village => Project(new Vec3(-48, 0, -29)),
        District.Bazaar => Project(new Vec3(-9, 0, 29)),
        _ => Project(new Vec3(31, 0, 27))
    };

    public Vec3 RallyPosition(CivilianState c) => c.RallyX != 0 || c.RallyZ != 0
        ? Project(new Vec3(c.RallyX, 0, c.RallyZ))
        : c.District switch
        {
            District.Village => Project(new Vec3(-45, 0, 7)),
            District.Bazaar => Project(new Vec3(0, 0, 7)),
            _ => Project(new Vec3(43, 0, 7))
        };

    public Vec3 SchedulePosition(ScheduleState schedule)=>SchedulePosition(schedule.Kind,schedule.District);

    public Vec3 SchedulePosition(ScheduleKind kind,District district)
    {
        var basePoint=district switch
        {
            District.Village=>new Vec3(-43,0,7),
            District.Bazaar=>new Vec3(0,0,7),
            _=>new Vec3(43,0,7)
        };
        var offset=kind switch
        {
            ScheduleKind.MedicalConvoy=>new Vec3(0,0,8),
            ScheduleKind.SupplyConvoy=>new Vec3(4,0,-6),
            ScheduleKind.Checkpoint=>new Vec3(-4,0,0),
            ScheduleKind.Patrol=>new Vec3(6,0,3),
            ScheduleKind.RadioBriefing=>new Vec3(-6,0,6),
            ScheduleKind.ReinforcementTruck=>new Vec3(8,0,-8),
            ScheduleKind.HvtTransfer=>new Vec3(-8,0,-7),
            _=>Vec3.Zero
        };
        return Project(basePoint+offset);
    }

    public void UpdateScheduleMarker(ScheduleSummary? schedule)
    {
        if(schedule is null||!Enum.TryParse<ScheduleKind>(schedule.Kind,out var kind)||!Enum.TryParse<District>(schedule.District,out var district))
        {
            if(_scheduleMarker is { } old && old.IsAlive) old.SetVisible(false);
            _scheduleMarkerId=0;
            return;
        }

        var p=SchedulePosition(kind,district);
        Entity marker;
        if(_scheduleMarker is not { } existing || !existing.IsAlive)
        {
            marker=_world.Spawn("Schedule objective marker")
                .SetBounds(new Vec3(.45f,.08f,.45f),Vec3.Zero)
                .SetTint(new Vec3(.95f,.62f,.18f))
                .AddTag("DebugVisible");
            _scheduleMarker=marker;
            _entities.Add(marker);
        }
        else marker=existing;

        marker
            .SetTransform(new Transform(p+new Vec3(0,.10f,0),Vec3.Zero,One()))
            .SetVisible(true);
        _scheduleMarkerId=schedule.Id;
    }

    public Vec3 CisfSpawn(CisfSpawnPoint spawn) => spawn switch
    {
        CisfSpawnPoint.VillagePolice => VillagePoliceSpawn,
        CisfSpawnPoint.BazaarPolice => BazaarPoliceSpawn,
        _ => FobSpawn
    };

    public void ApplyRemoteState(HomelandSync state)
    {
        ApplyRemoteCivilians(state.Civilians);
        ApplyRemotePlayers(state.Players);
        ApplyRemoteCorpses(state.Corpses);
    }

    private void ApplyRemoteCivilians(IReadOnlyList<CivilianSummary> states)
    {
        var live = states.Select(s => s.Id).ToHashSet();
        foreach (var id in _remoteCivilians.Keys.Where(id => !live.Contains(id)).ToArray())
        {
            if (_remoteCivilians[id].IsAlive) _remoteCivilians[id].Destroy();
            _remoteCivilians.Remove(id);
        }

        foreach (var s in states)
        {
            if (!_remoteCivilians.TryGetValue(s.Id, out var e))
            {
                e = SpawnRemoteHuman($"Civilian {s.Id}", HomelandAssets.Civilian(s.Outfit), new Vec3(.54f, .46f, .36f));
                _remoteCivilians.Add(s.Id, e);
            }
            e.SetTransform(new Transform(new Vec3(s.X, s.Y, s.Z), Vec3.Zero, One()));
            e.SetTint(s.CalledToArms ? new Vec3(.60f, .42f, .25f) : new Vec3(.72f, .65f, .52f));
        }
    }

    private void ApplyRemotePlayers(IReadOnlyList<PlayerSummary> states)
    {
        var live = states.Select(s => s.Slot).ToHashSet();
        foreach (var id in _remotePlayers.Keys.Where(id => !live.Contains(id)).ToArray())
        {
            if (_remotePlayers[id].IsAlive) _remotePlayers[id].Destroy();
            _remotePlayers.Remove(id);
        }
        foreach (var s in states)
        {
            if (!_remotePlayers.TryGetValue(s.Slot, out var e))
            {
                var model = s.Faction == Faction.Cisf.ToString() ? HomelandAssets.Cisf() : HomelandAssets.Civilian(s.Outfit);
                e = SpawnRemoteHuman($"Player {s.Slot}", model, s.Faction == Faction.Cisf.ToString() ? new Vec3(.30f, .42f, .35f) : new Vec3(.64f, .56f, .43f));
                _remotePlayers.Add(s.Slot, e);
            }
            e.SetTransform(new Transform(new Vec3(s.X, s.Y, s.Z), Vec3.Zero, One()));
            e.SetVisible(s.State != LifeState.Dead.ToString());
        }
    }

    private void ApplyRemoteCorpses(IReadOnlyList<CorpseSummary> states)
    {
        var live=states.Select(s=>s.IdentityId).ToHashSet();
        foreach(var id in _remoteCorpses.Keys.Where(id=>!live.Contains(id)).ToArray())
        {
            if(_remoteCorpses[id].IsAlive)_remoteCorpses[id].Destroy();
            _remoteCorpses.Remove(id);
        }

        foreach(var s in states)
        {
            if(!_remoteCorpses.TryGetValue(s.IdentityId,out var corpse))
            {
                var model=s.Faction==Faction.Cisf.ToString()?HomelandAssets.Cisf():HomelandAssets.Civilian(s.Outfit);
                corpse=SpawnRemoteHuman($"Corpse {s.Name}",model,new Vec3(.40f,.34f,.29f));
                corpse.SetInteraction($"Inspect {s.Name}","homeland.corpse");
                _remoteCorpses.Add(s.IdentityId,corpse);
            }
            corpse.SetTransform(new Transform(new Vec3(s.X,s.Y,s.Z),Vec3.Zero,One()));
        }
    }

    private Entity SpawnRemoteHuman(string name,string? model,Vec3 tint)
    {
        var root=_world.Spawn(name).SetTint(tint);
        if(model is not null)
        {
            var visual=root.CreateChild(name+" visual")
                .SetLocalTransform(new Transform(Vec3.Zero,Vec3.Zero,HomelandAssets.HumanVisualScale(model)))
                .SetModel(model);
            _entities.Add(visual);
        }
        else
        {
            root.SetBounds(new Vec3(.30f,.875f,.30f),new Vec3(0,.875f,0)).AddTag("DebugVisible");
        }
        _entities.Add(root);
        return root;
    }

    private static Vec3 One() => new(1, 1, 1);

    public void Dispose()
    {
        foreach (var e in _entities) if (e.IsAlive) e.Destroy();
        _remoteCivilians.Clear();
        _remotePlayers.Clear();
        _remoteCorpses.Clear();
        Navigation?.Dispose();
        _entities.Clear();
    }

    private sealed record BuildingSpec(string Name, Vec3 Center, Vec3 Half, Vec3 Tint, string AssetToken);
}

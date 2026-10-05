using Rivet; using Rivet.Homeland.Domain;
namespace Rivet.Homeland.Infrastructure;
internal sealed class HomelandWorld:IDisposable
{
    readonly World _world; readonly List<Entity> _entities=[]; public HomelandWorld(World world)=>_world=world;
    static Vec3 One()=>new(1,1,1);
    public void Build()
    {
        if(_entities.Count>0)return;
        _entities.Add(_world.Spawn("Homeland ambient").SetAmbientLight(new Vec3(.92f,.84f,.72f),.55f));
        var sun=_world.Spawn("Homeland sun").SetTransform(new Transform(Vec3.Zero,new Vec3(-52,-32,0),One())).SetDirectionalLight(new Vec3(1,.91f,.74f),1.05f);sun.GetComponent<DirectionalLight>()!.CastShadows=true;_entities.Add(sun);
        Ground(new Vec3(-42,-.25f,0),new Vec3(36,.25f,34),new Vec3(.54f,.47f,.34f),"Village ground");
        Ground(new Vec3(0,-.25f,0),new Vec3(20,.25f,34),new Vec3(.58f,.50f,.37f),"Bazaar ground");
        Ground(new Vec3(42,-.25f,0),new Vec3(36,.25f,34),new Vec3(.46f,.44f,.39f),"CBD ground");
        Road(new Vec3(0,.01f,0),new Vec3(76,.02f,3)); Road(new Vec3(0,.01f,18),new Vec3(76,.02f,2.2f));
        for(var i=0;i<12;i++) Building($"Village house {i}",new Vec3(-64+(i%4)*11,1.5f,-23+(i/4)*17),new Vec3(4,1.5f,4),HomelandAssets.House(),new Vec3(.67f,.58f,.44f));
        for(var i=0;i<18;i++) Building($"Bazaar shop {i}",new Vec3(-15+(i%6)*6,1.3f,-18+(i/6)*16),new Vec3(2.6f,1.3f,4.5f),HomelandAssets.Building(),new Vec3(.73f,.59f,.39f));
        for(var i=0;i<10;i++) Building($"CBD block {i}",new Vec3(25+(i%5)*9,3.5f,-18+(i/5)*30),new Vec3(3.7f,3.5f,6),HomelandAssets.Building(),new Vec3(.54f,.55f,.53f));
        Building("CISF FOB",new Vec3(61,2,-28),new Vec3(10,2,7),HomelandAssets.Building(),new Vec3(.28f,.31f,.30f));
        Building("Police Station Bazaar",new Vec3(14,1.6f,26),new Vec3(5,1.6f,5),HomelandAssets.Building(),new Vec3(.33f,.38f,.42f));
        Building("Police Station Village",new Vec3(-59,1.6f,25),new Vec3(5,1.6f,5),HomelandAssets.Building(),new Vec3(.36f,.40f,.43f));
        var tower=Building("FOB communications tower",new Vec3(68,7,-28),new Vec3(.6f,7,.6f),null,new Vec3(.42f,.45f,.46f));tower.SetInteraction("Sabotage communications tower","homeland.sabotage");
        SpawnVehicle("CISF reinforcement truck",new Vec3(55,.7f,-18),HomelandAssets.Truck()); SpawnVehicle("Civilian car",new Vec3(-3,.5f,8),HomelandAssets.Car());
    }
    Entity Ground(Vec3 p,Vec3 h,Vec3 c,string n)=>Building(n,p,h,null,c);
    void Road(Vec3 p,Vec3 h){var e=Building("Road",p,h,HomelandAssets.Road(),new Vec3(.19f,.19f,.18f));}
    Entity Building(string name,Vec3 p,Vec3 half,string? model,Vec3 tint)
    {
        var e=_world.Spawn(name).SetTransform(new Transform(p,Vec3.Zero,One())).SetTint(tint); if(model is not null)e.SetModel(model);else e.SetBounds(half,Vec3.Zero).AddTag("DebugVisible"); _entities.Add(e);return e;
    }
    void SpawnVehicle(string name,Vec3 p,string? model){var e=Building(name,p,new Vec3(1.1f,.7f,2.2f),model,new Vec3(.24f,.28f,.27f));e.SetBody(new BodyDesc(BodyMotion.Kinematic,new Vec3(1.1f,.7f,2.2f),1200,false));}
    public void Dispose(){foreach(var e in _entities)if(e.IsAlive)e.Destroy();_entities.Clear();}
}

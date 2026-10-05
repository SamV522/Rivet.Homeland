using Rivet.Homeland.Domain;
namespace Rivet.Homeland.Application;
internal sealed class CivilianDirector
{
    private readonly Random _rng=new(33117);
    public List<CivilianState> Civilians { get; }=[];
    public int AvailableHlaTickets=>Civilians.Count(c=>c.AvailableAsHlaTicket&&!c.Incarcerated);
    public void Seed(int count,IdentityGenerator ids)
    {
        for(var i=0;i<count;i++)
        {
            var c=new CivilianState{Identity=ids.Create(100+i),District=(District)(i%3),Occupation=(CivilianOccupation)_rng.Next(Enum.GetValues<CivilianOccupation>().Length),HlaAlignment=(float)_rng.NextDouble()*.25f,CisfAlignment=(float)_rng.NextDouble()*.25f,Fear=.15f,Anger=.05f,CarryingId=_rng.NextDouble()>.18};
            Civilians.Add(c);
        }
        for(var i=0;i<Civilians.Count;i++) foreach(var j in Enumerable.Range(1,_rng.Next(2,5))) Civilians[i].Relationships.Add(Civilians[(i+j)%Civilians.Count].Identity.Id);
    }
    public void Update(float dt)
    {
        foreach(var c in Civilians)
        {
            if(c.Incarcerated){ c.CurrentActivity="Detained"; c.Anger=Math.Clamp(c.Anger+HomelandRules.WrongfulDetentionAngerPerMinute/60f*dt/100f,0,1); continue; }
            var phase=(int)(DateTime.UtcNow.Second/10f + c.Identity.Id)%5;
            c.CurrentActivity=phase switch{0=>"At home",1=>"Travelling to work",2=>c.Occupation==CivilianOccupation.Unemployed?"Visiting the bazaar":$"Working as {c.Occupation}",3=>"Buying food",_=>"Going home"};
        }
    }
    public bool Recruit(int civilianId,int recruiterId,bool threaten=false,bool pay=false)
    {
        var c=Civilians.First(x=>x.Identity.Id==civilianId); var opinion=c.PersonalOpinion.GetValueOrDefault(recruiterId);
        var chance=.18f+c.HlaAlignment*.45f+c.Anger*.28f+Math.Max(0,opinion)*.12f+(pay?.12f:0)-(threaten?c.Fear*.12f:0);
        if(_rng.NextDouble()>chance){ c.PersonalOpinion[recruiterId]=opinion-(threaten?.2f:.05f); return false; }
        c.Rebel=true; c.AvailableAsHlaTicket=true; c.HlaAlignment=Math.Max(c.HlaAlignment,.65f); return true;
    }
    public void RelationshipShock(int identityId,float angerDelta,float hlaDelta=0,float cisfDelta=0)
    {
        var source=Civilians.FirstOrDefault(c=>c.Identity.Id==identityId); if(source is null)return;
        foreach(var c in Civilians.Where(c=>source.Relationships.Contains(c.Identity.Id))) { c.Anger=Math.Clamp(c.Anger+angerDelta,0,1); c.HlaAlignment=Math.Clamp(c.HlaAlignment+hlaDelta,0,1); c.CisfAlignment=Math.Clamp(c.CisfAlignment+cisfDelta,0,1); }
    }
    public float DistrictReadiness(District d)
    {
        var local=Civilians.Where(c=>c.District==d).ToArray(); if(local.Length==0)return 0;
        return local.Average(c=>Math.Clamp(c.HlaAlignment*.45f+c.Anger*.35f+(c.Rebel?.2f:0)-c.Fear*.12f,0,1));
    }
}

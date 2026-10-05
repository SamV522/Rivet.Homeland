using Rivet.Homeland.Domain;

namespace Rivet.Homeland.Application;

internal sealed class CivilianDirector
{
    private readonly Random _rng=new(33117);
    public List<CivilianState> Civilians { get; }=[];
    public int AvailableHlaTickets=>Civilians.Count(c=>c.AvailableAsHlaTicket&&!c.Incarcerated&&!c.PlayerControlled);

    public void Seed(int count,IdentityGenerator ids)
    {
        for(var i=0;i<count;i++)
        {
            var c=new CivilianState
            {
                Identity=ids.Create(100+i),
                District=(District)(i%3),
                Occupation=(CivilianOccupation)_rng.Next(Enum.GetValues<CivilianOccupation>().Length),
                HlaAlignment=(float)_rng.NextDouble()*.25f,
                CisfAlignment=(float)_rng.NextDouble()*.25f,
                Fear=.15f,
                Anger=.05f,
                CarryingId=_rng.NextDouble()>.18
            };
            Civilians.Add(c);
        }

        for(var i=0;i<Civilians.Count;i++)
        {
            var countLinks=_rng.Next(2,5);
            for(var j=1;j<=countLinks;j++)
            {
                var other=Civilians[(i+j)%Civilians.Count].Identity.Id;
                Civilians[i].Relationships.Add(other);
                Civilians.First(x=>x.Identity.Id==other).Relationships.Add(Civilians[i].Identity.Id);
            }
        }
    }

    public void UpdateSocial(float dt)
    {
        foreach(var c in Civilians)
        {
            c.Danger=Math.Max(0,c.Danger-dt*.06f);
            c.Fear=Math.Clamp(c.Fear-dt*.002f,0,1);
            if(!c.Incarcerated) continue;
            c.CurrentActivity="Detained";
            c.Anger=Math.Clamp(c.Anger+HomelandRules.WrongfulDetentionAngerPerMinute/60f*dt/100f,0,1);
        }
    }

    public CivilianState? ById(int identityId)=>Civilians.FirstOrDefault(c=>c.Identity.Id==identityId);

    public bool Recruit(int civilianId,int recruiterId,bool threaten=false,bool pay=false)
    {
        var c=Civilians.First(x=>x.Identity.Id==civilianId);
        if(c.Incarcerated||c.PlayerControlled||c.Rebel)return c.Rebel;
        var opinion=c.PersonalOpinion.GetValueOrDefault(recruiterId);
        var chance=.18f+c.HlaAlignment*.45f+c.Anger*.28f+Math.Max(0,opinion)*.12f+(pay?.12f:0)-(threaten?c.Fear*.12f:0);
        if(_rng.NextDouble()>chance)
        {
            c.PersonalOpinion[recruiterId]=opinion-(threaten?.2f:.05f);
            c.Memories.Add(new(){SubjectIdentityId=recruiterId,Fact="Tried to recruit me into HLA",FirstHand=true,Confidence=1});
            return false;
        }
        c.Rebel=true;
        c.AvailableAsHlaTicket=true;
        c.HlaAlignment=Math.Max(c.HlaAlignment,.65f);
        c.PersonalOpinion[recruiterId]=Math.Max(.25f,opinion+.2f);
        return true;
    }

    public void CallToArms(District district,int callerIdentityId,float rallyX,float rallyZ,float radiusInfluence=1)
    {
        foreach(var c in Civilians.Where(c=>c.District==district&&c.Rebel&&!c.Incarcerated&&!c.PlayerControlled))
        {
            var readiness=Math.Clamp(c.HlaAlignment*.55f+c.Anger*.35f-c.Fear*.15f,0,1);
            if(readiness<.42f/radiusInfluence)continue;
            c.CalledToArms=true;
            c.ScrambleRequested=false;
            c.RallyX=rallyX;
            c.RallyZ=rallyZ;
            c.CurrentActivity="Answering Call to Arms";
            c.Memories.Add(new(){SubjectIdentityId=callerIdentityId,Fact="Called local HLA supporters to arms",FirstHand=true,Confidence=1});
        }
    }

    public void Scramble(District district)
    {
        foreach(var c in Civilians.Where(c=>c.District==district&&c.Rebel&&c.CalledToArms&&!c.PlayerControlled))
        {
            c.ScrambleRequested=true;
            c.CurrentActivity="Scrambling back into civilian life";
        }
    }

    public void RaiseDanger(District district,float amount)
    {
        foreach(var c in Civilians.Where(c=>c.District==district&&!c.Incarcerated))
        {
            c.Danger=Math.Clamp(c.Danger+amount,0,1);
            c.Fear=Math.Clamp(c.Fear+amount*.35f,0,1);
        }
    }

    public void AddWitness(int witnessIdentity,int subjectIdentity,string fact,bool firstHand=true,float confidence=1)
    {
        var c=ById(witnessIdentity); if(c is null)return;
        c.Memories.Add(new(){SubjectIdentityId=subjectIdentity,Fact=fact,FirstHand=firstHand,Confidence=Math.Clamp(confidence,0,1)});
    }

    public void MarkWitnessesReported(CivilianState civilian)
    {
        foreach(var memory in civilian.Memories.Where(m=>!m.Reported))memory.Reported=true;
    }

    public void RelationshipShock(int identityId,float angerDelta,float hlaDelta=0,float cisfDelta=0)
    {
        var source=ById(identityId); if(source is null)return;
        foreach(var c in Civilians.Where(c=>source.Relationships.Contains(c.Identity.Id)))
        {
            c.Anger=Math.Clamp(c.Anger+angerDelta,0,1);
            c.HlaAlignment=Math.Clamp(c.HlaAlignment+hlaDelta,0,1);
            c.CisfAlignment=Math.Clamp(c.CisfAlignment+cisfDelta,0,1);
        }
    }

    public float DistrictReadiness(District d)
    {
        var local=Civilians.Where(c=>c.District==d).ToArray(); if(local.Length==0)return 0;
        return local.Average(c=>Math.Clamp(c.HlaAlignment*.45f+c.Anger*.35f+(c.Rebel?.2f:0)-c.Fear*.12f,0,1));
    }
}

namespace Rivet.Homeland.Application;

[Flags]
internal enum CivilianFact : ulong
{
    None=0,
    AtHome=1UL<<0,
    AtWork=1UL<<1,
    AtBazaar=1UL<<2,
    AtSafePlace=1UL<<3,
    AtReportPoint=1UL<<4,
    AtWeaponCache=1UL<<5,
    AtRally=1UL<<6,
    InDanger=1UL<<7,
    HasReportableIntel=1UL<<8,
    IntelReported=1UL<<9,
    Rebel=1UL<<10,
    CalledToArms=1UL<<11,
    Armed=1UL<<12,
    ScrambleRequested=1UL<<13,
}

internal sealed record GoapAction(
    string Name,
    CivilianFact RequireAll,
    CivilianFact RequireNone,
    CivilianFact Add,
    CivilianFact Remove,
    float Cost=1);

internal readonly record struct GoapGoal(CivilianFact RequireAll,CivilianFact RequireNone);

internal static class GoapPlanner
{
    public static IReadOnlyList<GoapAction> Plan(
        CivilianFact start,
        GoapGoal goal,
        IReadOnlyList<GoapAction> actions)
    {
        if(Satisfies(start,goal))return [];

        var frontier=new PriorityQueue<Node,float>();
        frontier.Enqueue(new(start,[],0),0);
        var best=new Dictionary<CivilianFact,float>{{start,0}};

        while(frontier.TryDequeue(out var node,out _))
        {
            if(Satisfies(node.State,goal))return node.Plan;
            foreach(var action in actions)
            {
                if((node.State&action.RequireAll)!=action.RequireAll)continue;
                if((node.State&action.RequireNone)!=0)continue;
                var next=(node.State|action.Add)&~action.Remove;
                if(next==node.State)continue;
                var cost=node.Cost+Math.Max(.01f,action.Cost);
                if(best.TryGetValue(next,out var existing)&&existing<=cost)continue;
                best[next]=cost;
                var plan=new List<GoapAction>(node.Plan){action};
                frontier.Enqueue(new(next,plan,cost),cost);
            }
        }
        return [];
    }

    private static bool Satisfies(CivilianFact state,GoapGoal goal)=>
        (state&goal.RequireAll)==goal.RequireAll&&(state&goal.RequireNone)==0;

    private sealed record Node(CivilianFact State,List<GoapAction> Plan,float Cost);
}

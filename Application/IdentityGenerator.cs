using Rivet.Homeland.Domain;
namespace Rivet.Homeland.Application;
internal sealed class IdentityGenerator(int seed)
{
    private readonly Random _rng=new(seed);
    private static readonly string[] First=["Ahmed","Yusuf","Karim","Samir","Hadi","Omar","Nabil","Farid","Rashid","Layla","Mariam","Nadia","Sara","Amina","Dalia","Hana"];
    private static readonly string[] Last=["Abdullah-Warith","Haddad","Rahman","Nasser","Khalil","Saleh","Aziz","Mansour","Darzi","Farah","Hamdan","Yacoub"];
    private static readonly string[] Hair=["Black","Dark Brown","Brown","Grey"];
    private static readonly string[] Eyes=["Brown","Dark Brown","Hazel","Green"];
    private static readonly string[] Skin=["C3","C4","D3","D4","E3","E4"];
    private static readonly string[] Outfit=["work-shirt","long-coat","market-clothes","casual-shirt","utility-jacket","traditional-clothes"];
    public Identity Create(int id,string? forcedFirst=null,string? forcedLast=null)=>new(id,forcedFirst??Pick(First),forcedLast??Pick(Last),_rng.Next(18,72),new(Pick(Hair),Pick(Eyes),Pick(Skin),Pick(Outfit)),Sig("FP",id),Sig("BLD",id),$"home-{id:000}",$"job-{id:000}");
    string Pick(string[] a)=>a[_rng.Next(a.Length)];
    string Sig(string p,int id)=>$"{p}-{((id*7919+seed*104729)&0xFFFFFF):X6}";
}

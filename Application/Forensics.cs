using Rivet.Homeland.Domain;
namespace Rivet.Homeland.Application;
internal sealed class Forensics
{
    private int _next=1000;
    public List<EvidenceRecord> Database { get; }=[];
    public EvidenceRecord InspectIdentity(Identity identity,int author)
    {
        var r=new EvidenceRecord(_next++,EvidenceKind.Dossier,$"DOS-{_next:0000}",$"{identity.First} {identity.Last}, {identity.Age}, hair {identity.Appearance.Hair}, eyes {identity.Appearance.Eyes}, skin {identity.Appearance.SkinCode}",true,identity.Id,AuthorIdentityId:author);
        return r;
    }
    public EvidenceRecord SampleFingerprint(string signature,int? identity=null)=>new(_next++,EvidenceKind.Fingerprint,$"FP-SAMPLE-{_next:0000}","Recovered fingerprint",true,identity,signature);
    public EvidenceRecord SampleBlood(string signature,int? identity=null)=>new(_next++,EvidenceKind.Blood,$"BLD-SAMPLE-{_next:0000}","Recovered blood sample",true,identity,signature);
    public EvidenceRecord Projectile(string gunSignature)=>new(_next++,EvidenceKind.Ballistic,$"BAL-{_next:0000}","Recovered projectile",true,Signature:gunSignature);
    public void Log(EvidenceRecord record)=>Database.Add(record);
    public bool TryManualIdentityMatch(DossierFields entered,Identity target)
    {
        var score=0; var tests=0;
        void Check(bool supplied,bool ok){ if(!supplied)return; tests++; if(ok)score++; }
        Check(!string.IsNullOrWhiteSpace(entered.First),string.Equals(entered.First,target.First,StringComparison.OrdinalIgnoreCase));
        Check(!string.IsNullOrWhiteSpace(entered.Last),string.Equals(entered.Last,target.Last,StringComparison.OrdinalIgnoreCase));
        Check(entered.Age.HasValue,entered.Age==target.Age);
        Check(entered.Hair is not null,string.Equals(entered.Hair,target.Appearance.Hair,StringComparison.OrdinalIgnoreCase));
        Check(entered.Eyes is not null,string.Equals(entered.Eyes,target.Appearance.Eyes,StringComparison.OrdinalIgnoreCase));
        Check(entered.SkinCode is not null,string.Equals(entered.SkinCode,target.Appearance.SkinCode,StringComparison.OrdinalIgnoreCase));
        return tests>=4 && score==tests;
    }
}

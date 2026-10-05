namespace Rivet.Homeland.Domain;

internal enum Faction { Civilian, Hla, Cisf }
internal enum District { Village, Bazaar, Cbd }
internal enum LifeState { Active, Downed, Detained, Dead }
internal enum EvidenceKind { Dossier, Fingerprint, Blood, Ballistic, CctvPhoto, WitnessStatement, PlayerNote }
internal enum ScheduleKind { MedicalConvoy, SupplyConvoy, Checkpoint, Patrol, RadioBriefing, ReinforcementTruck, HvtTransfer }
internal enum CivilianOccupation { Unemployed, Farmer, Shopkeeper, Driver, Mechanic, Clerk, Trader, Thief, Labourer }

internal sealed record Appearance(string Hair, string Eyes, string SkinCode, string Outfit, bool Masked=false);
internal sealed record Identity(int Id,string First,string Last,int Age,Appearance Appearance,string FingerprintSignature,string BloodSignature,string? HomeId,string? JobId);
internal sealed record EvidenceRecord(int Id,EvidenceKind Kind,string Reference,string Summary,bool Verified,int? IdentityId=null,string? Signature=null,int? AuthorIdentityId=null);
internal sealed record DossierFields(string First,string Last,int? Age,string? Hair,string? Eyes,string? SkinCode);
internal sealed record ScheduleState(int Id,ScheduleKind Kind,District District,float RemainingSeconds,Faction AssignedFaction,bool Complete=false,bool Failed=false,string Reward="");
internal sealed record RadioMessage(float Time,int SpeakerIdentityId,string Text);
internal sealed record WitnessMemory(int SubjectIdentityId,string Fact,bool FirstHand,float Confidence);

internal sealed class CivilianState
{
    public required Identity Identity { get; init; }
    public District District { get; set; }
    public CivilianOccupation Occupation { get; set; }
    public float HlaAlignment { get; set; }
    public float CisfAlignment { get; set; }
    public float Fear { get; set; }
    public float Anger { get; set; }
    public bool Rebel { get; set; }
    public bool AvailableAsHlaTicket { get; set; }
    public bool CarryingId { get; set; } = true;
    public bool Incarcerated { get; set; }
    public string CurrentActivity { get; set; } = "Going about their day";
    public Dictionary<int,float> PersonalOpinion { get; } = [];
    public List<WitnessMemory> Memories { get; } = [];
    public HashSet<int> Relationships { get; } = [];
}

internal sealed class PlayerLife
{
    public int Slot { get; init; }
    public uint? PeerId { get; init; }
    public Faction Faction { get; set; }
    public required Identity Identity { get; set; }
    public Appearance PresentedAppearance { get; set; } = null!;
    public LifeState State { get; set; }
    public float Health { get; set; } = 100;
    public float BleedOutRemaining { get; set; }
    public float RespawnRemaining { get; set; }
    public bool HasRadio { get; set; }
    public bool RadioOn { get; set; }
    public bool HasIdDocument { get; set; } = true;
    public bool Restrained { get; set; }
    public bool InVehiclePrisonerSeat { get; set; }
    public string EquipmentSummary { get; set; } = "Civilian clothes";
    public string PrivateNotepad { get; set; } = "";
    public HashSet<int> PersonallyKnownIdentities { get; } = [];
}

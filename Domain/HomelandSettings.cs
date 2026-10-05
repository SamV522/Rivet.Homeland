namespace Rivet.Homeland.Domain;
internal sealed class HomelandSettings
{
    public int MatchMinutes { get; set; } = HomelandRules.DefaultMatchMinutes;
    public int CivilianPopulation { get; set; } = HomelandRules.InitialCivilianPopulation;
    public float CisfRatio { get; set; } = HomelandRules.CisfShare;
    public bool AllowCivilianPlayers { get; set; } = true;
    public bool EnableBallistics { get; set; } = true;
    public bool EnableCctvRecognition { get; set; } = true;
    public bool EnableInsurrection { get; set; } = true;
}

namespace MessageModerationLambda;

public enum ModerationStatus
{
    Clean,
    Flagged
}

public class ModerationResult
{
    public required ModerationStatus Status { get; init; }
    public required string[] MatchedTerms { get; init; }
    public required string OriginalText { get; init; }
}

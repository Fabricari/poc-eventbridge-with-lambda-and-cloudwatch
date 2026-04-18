namespace MessageModerationLambda;

public class MessageModerationService
{
    // Mild, family-friendly terms that this demo treats as flagged content.
    private static readonly HashSet<string> FlaggedTerms = new(StringComparer.OrdinalIgnoreCase)
    {
        "gee", "golly", "gosh", "drat", "rats", "shoot", "shucks", "darn", "dang", "heck",
        "frick", "fudge", "crud", "poop", "butt", "crap", "jerk", "idiot"
    };

    // Evaluates the incoming text and returns status, matched terms, and original content.
    public ModerationResult Evaluate(string text)
    {
        var words = text.Split([' ', '\t', '\n', '\r', ',', '.', '!', '?', ';', ':'],
            StringSplitOptions.RemoveEmptyEntries);

        var matched = words
            .Where(word => FlaggedTerms.Contains(word))
            .Select(word => word.ToLowerInvariant())
            .Distinct()
            .Order()
            .ToArray();

        return new ModerationResult
        {
            Status = matched.Length > 0 ? ModerationStatus.Flagged : ModerationStatus.Clean,
            MatchedTerms = matched,
            OriginalText = text
        };
    }
}

// Represents one moderation pass result for a submitted message.
public class ModerationResult
{
    public required ModerationStatus Status { get; init; }
    public required string[] MatchedTerms { get; init; }
    public required string OriginalText { get; init; }
}

// Defines the high-level moderation state for evaluated text.
public enum ModerationStatus
{
    Clean,
    Flagged
}
